use binary_rw::{BinaryError, BinaryWriter, FileStream, SeekStream};
use num_traits::ToPrimitive;
use std::{
    cell::RefCell,
    fs::{self, File},
    io::Write,
    path::Path,
    rc::Rc,
};
use zip::{write::FileOptions, ZipWriter};

use crate::{
    models::{
        BrafLifecycle, BxesArtifact, BxesDrivers, BxesEvent, BxesEventLog, BxesValue, Lifecycle,
        SoftwareEventType, StandardLifecycle,
    },
    type_ids::{self, TypeIds},
};

use super::{errors::BxesWriteError, write_context::BxesWriteContext};

pub fn try_write_variants(
    log: &BxesEventLog,
    context: Rc<RefCell<BxesWriteContext>>,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context.clone(), || {
        for variant in &log.variants {
            try_write_u32_no_type_id(
                context.borrow_mut().writer.as_mut().unwrap(),
                variant.traces_count,
            )?;

            try_write_attributes(context.clone(), Some(&variant.metadata))?;

            write_collection_and_count(context.clone(), || {
                for event in &variant.events {
                    try_write_event(event, context.clone())?;
                }

                Ok(variant.events.len() as u32)
            })?;
        }

        Ok(log.variants.len() as u32)
    })
}

pub fn try_write_event(
    event: &BxesEvent,
    context: Rc<RefCell<BxesWriteContext>>,
) -> Result<(), BxesWriteError> {
    {
        if context
            .borrow()
            .values_indices
            .borrow()
            .contains_key(&event.name)
        {
            let index = *context
                .borrow()
                .values_indices
                .borrow()
                .get(&event.name)
                .unwrap();

            try_write_u32_no_type_id(context.borrow_mut().writer.as_mut().unwrap(), index as u32)?;
        } else {
            return Err(BxesWriteError::FailedToFindValueIndex(event.name.clone()));
        };
    }

    try_write_i64_no_type_id(
        context.borrow_mut().writer.as_mut().unwrap(),
        event.timestamp,
    )?;

    try_write_lifecycle(
        context.borrow_mut().writer.as_mut().unwrap(),
        &event.lifecycle,
    )?;

    try_write_attributes(context, event.attributes.as_ref())
}

pub fn try_write_log_metadata(
    log: &BxesEventLog,
    context: Rc<RefCell<BxesWriteContext>>,
) -> Result<(), BxesWriteError> {
    try_write_attributes(context, log.metadata.as_ref())
}

pub fn try_write_attributes(
    context: Rc<RefCell<BxesWriteContext>>,
    attributes: Option<&Vec<(BxesValue, BxesValue)>>,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context.clone(), || {
        if let Some(attributes) = attributes {
            for (key, value) in attributes {
                let pair = (key, value);
                if context.borrow().kv_indices.borrow().contains_key(&pair) {
                    let index = *context.borrow().kv_indices.borrow().get(&pair).unwrap();
                    try_write_u32_no_type_id(
                        context.borrow_mut().writer.as_mut().unwrap(),
                        index as u32,
                    )?;
                } else {
                    return Err(
                        BxesWriteError::FailedToFindKeyValueIndex((key.clone(), value.clone()))
                    );
                }
            }

            Ok(attributes.len() as u32)
        } else {
            Ok(0)
        }
    })
}

pub fn try_write_key_values<'a: 'b, 'b>(
    log: &'a BxesEventLog,
    context: Rc<RefCell<BxesWriteContext<'a, 'b>>>,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context.clone(), || {
        execute_with_kv_pairs(log, |value| {
            match value {
                ValueOrKeyValue::Value(_) => {}
                ValueOrKeyValue::KeyValue((key, value)) => {
                    if !context
                        .borrow()
                        .kv_indices
                        .borrow()
                        .contains_key(&(key, value))
                    {
                        let count = context.borrow().kv_indices.borrow().len();
                        let key_index = *context.borrow().values_indices.borrow().get(key).unwrap();
                        let value_index =
                            *context.borrow().values_indices.borrow().get(value).unwrap();

                        try_write_u32_no_type_id(
                            context.borrow_mut().writer.as_mut().unwrap(),
                            key_index as u32,
                        )?;

                        try_write_u32_no_type_id(
                            context.borrow_mut().writer.as_mut().unwrap(),
                            value_index as u32,
                        )?;

                        context
                            .borrow_mut()
                            .kv_indices
                            .borrow_mut()
                            .insert((key, value), count);
                    }
                }
            }

            Ok(())
        })?;

        Ok(context.borrow().kv_indices.borrow().len() as u32)
    })
}

pub enum ValueOrKeyValue<'a> {
    Value(&'a BxesValue),
    KeyValue((&'a BxesValue, &'a BxesValue)),
}

fn execute_with_kv_pairs<'a>(
    log: &'a BxesEventLog,
    mut action: impl FnMut(ValueOrKeyValue<'a>) -> Result<(), BxesWriteError>,
) -> Result<(), BxesWriteError> {
    if let Some(metadata) = log.metadata.as_ref() {
        execute_with_attributes_kv_pairs(metadata, &mut action)?;
    }

    for variant in &log.variants {
        execute_with_attributes_kv_pairs(&variant.metadata, &mut action)?;

        for event in &variant.events {
            action(ValueOrKeyValue::Value(&event.name))?;
            if let Some(attributes) = event.attributes.as_ref() {
                execute_with_attributes_kv_pairs(attributes, &mut action)?;
            }
        }
    }

    Ok(())
}

fn execute_with_attributes_kv_pairs<'a>(
    attributes: &'a Vec<(BxesValue, BxesValue)>,
    action: &mut impl FnMut(ValueOrKeyValue<'a>) -> Result<(), BxesWriteError>,
) -> Result<(), BxesWriteError> {
    for (key, value) in attributes {
        action(ValueOrKeyValue::Value(key))?;
        action(ValueOrKeyValue::Value(value))?;

        action(ValueOrKeyValue::KeyValue((key, value)))?;
    }

    Ok(())
}

pub fn try_write_version(writer: &mut BinaryWriter, version: u32) -> Result<(), BxesWriteError> {
    try_write_u32_no_type_id(writer, version)
}

pub fn try_write_values<'a: 'b, 'b>(
    log: &'a BxesEventLog,
    context: Rc<RefCell<BxesWriteContext<'a, 'b>>>,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context.clone(), || {
        execute_with_kv_pairs(log, |value| {
            match value {
                ValueOrKeyValue::Value(value) => {
                    try_write_value(value, &mut context.borrow_mut())?;
                }
                ValueOrKeyValue::KeyValue(_) => {}
            }

            Ok(())
        })?;

        Ok(context.borrow().values_indices.borrow().len() as u32)
    })
}

fn write_collection_and_count(
    context: Rc<RefCell<BxesWriteContext>>,
    mut writer_action: impl FnMut() -> Result<u32, BxesWriteError>,
) -> Result<(), BxesWriteError> {
    let pos = try_tell_pos(context.borrow_mut().writer.as_mut().unwrap())?;

    try_write_u32_no_type_id(context.borrow_mut().writer.as_mut().unwrap(), 0)?;

    let count = writer_action()?;

    let current_pos = try_tell_pos(context.borrow_mut().writer.as_mut().unwrap())?;
    try_seek(context.borrow_mut().writer.as_mut().unwrap(), pos)?;
    try_write_u32_no_type_id(context.borrow_mut().writer.as_mut().unwrap(), count)?;
    try_seek(context.borrow_mut().writer.as_mut().unwrap(), current_pos)
}

fn try_seek(writer: &mut BinaryWriter, pos: usize) -> Result<(), BxesWriteError> {
    match writer.seek(pos) {
        Ok(_) => Ok(()),
        Err(err) => Err(BxesWriteError::FailedToSeek(err.to_string())),
    }
}

fn try_tell_pos(writer: &mut BinaryWriter) -> Result<usize, BxesWriteError> {
    match writer.tell() {
        Ok(pos) => Ok(pos),
        Err(err) => Err(BxesWriteError::FailedToGetWriterPosition(err.to_string())),
    }
}

pub fn try_write_value<'a: 'b, 'b>(
    value: &'a BxesValue,
    context: &mut BxesWriteContext<'a, 'b>,
) -> Result<bool, BxesWriteError> {
    if context.values_indices.borrow().contains_key(value) {
        return Ok(false);
    }

    match value {
        BxesValue::Int32(value) => try_write_i32(context.writer.as_mut().unwrap(), *value),
        BxesValue::Int64(value) => try_write_i64(context.writer.as_mut().unwrap(), *value),
        BxesValue::Uint32(value) => try_write_u32(context.writer.as_mut().unwrap(), *value),
        BxesValue::Uint64(value) => try_write_u64(context.writer.as_mut().unwrap(), *value),
        BxesValue::Float32(value) => try_write_f32(context.writer.as_mut().unwrap(), *value),
        BxesValue::Float64(value) => try_write_f64(context.writer.as_mut().unwrap(), *value),
        BxesValue::String(value) => {
            try_write_string(context.writer.as_mut().unwrap(), value.as_str())
        }
        BxesValue::Bool(value) => try_write_bool(context.writer.as_mut().unwrap(), *value),
        BxesValue::Timestamp(value) => {
            try_write_timestamp(context.writer.as_mut().unwrap(), *value)
        }
        BxesValue::BrafLifecycle(value) => {
            try_write_braf_lifecycle(context.writer.as_mut().unwrap(), value)
        }
        BxesValue::StandardLifecycle(value) => {
            try_write_standard_lifecycle(context.writer.as_mut().unwrap(), value)
        }
        BxesValue::Artifact(artifacts) => try_write_artifact(context, artifacts),
        BxesValue::Drivers(drivers) => try_write_drivers(context, drivers),
        BxesValue::Guid(guid) => try_write_guid(context.writer.as_mut().unwrap(), guid),
        BxesValue::SoftwareEventType(value) => {
            try_write_software_event_type(context.writer.as_mut().unwrap(), value)
        }
    }?;

    let len = context.values_indices.borrow().len();
    context.values_indices.borrow_mut().insert(value, len);

    Ok(true)
}

pub fn try_write_software_event_type(
    writer: &mut BinaryWriter,
    value: &SoftwareEventType,
) -> Result<(), BxesWriteError> {
    try_write_enum_value(writer, &TypeIds::SoftwareEventType, value)
}

pub fn try_write_guid(writer: &mut BinaryWriter, guid: &uuid::Uuid) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::Guid))?;
        writer.write_bytes(guid.to_bytes_le())
    })
}

fn get_type_id_byte(type_id: TypeIds) -> u8 {
    TypeIds::to_u8(&type_id).unwrap()
}

pub fn try_write_artifact<'a: 'b, 'b>(
    context: &mut BxesWriteContext<'a, 'b>,
    artifact: &'a BxesArtifact,
) -> Result<(), BxesWriteError> {
    for artifact in &artifact.items {
        get_or_write_value_index(&artifact.instance, context)?;
        get_or_write_value_index(&artifact.transition, context)?;
    }

    try_write_u8_no_type_id(
        context.writer.as_mut().unwrap(),
        get_type_id_byte(TypeIds::Artifact),
    )?;

    try_write_u32_no_type_id(
        context.writer.as_mut().unwrap(),
        artifact.items.len() as u32,
    )?;

    for artifact in &artifact.items {
        let index = get_index(&artifact.instance, context)?;
        try_write_u32_no_type_id(context.writer.as_mut().unwrap(), index as u32)?;

        let index = get_index(&artifact.transition, context)?;
        try_write_u32_no_type_id(context.writer.as_mut().unwrap(), index as u32)?;
    }

    Ok(())
}

fn get_index(value: &BxesValue, context: &mut BxesWriteContext) -> Result<u32, BxesWriteError> {
    if let Some(index) = context.values_indices.borrow().get(&value) {
        return Ok(*index as u32);
    }

    Err(BxesWriteError::FailedToFindValueIndex(value.clone()))
}

fn get_or_write_value_index<'a: 'b, 'b>(
    value: &'a BxesValue,
    context: &mut BxesWriteContext<'a, 'b>,
) -> Result<u32, BxesWriteError> {
    try_write_value(value, context)?;
    let index = *context.values_indices.borrow().get(value).unwrap() as u32;

    return Ok(index);
}

pub fn try_write_drivers<'a: 'b, 'b>(
    context: &mut BxesWriteContext<'a, 'b>,
    drivers: &'a BxesDrivers,
) -> Result<(), BxesWriteError> {
    for driver in &drivers.drivers {
        get_or_write_value_index(&driver.name, context)?;
        get_or_write_value_index(&driver.driver_type, context)?;
    }

    try_write_u8_no_type_id(
        context.writer.as_mut().unwrap(),
        get_type_id_byte(TypeIds::Drivers),
    )?;

    try_write_u32_no_type_id(
        context.writer.as_mut().unwrap(),
        drivers.drivers.len() as u32,
    )?;

    for driver in &drivers.drivers {
        try_write_f64_no_type_id(context.writer.as_mut().unwrap(), driver.amount())?;

        let index = get_index(&driver.name, context)?;
        try_write_u32_no_type_id(context.writer.as_mut().unwrap(), index)?;

        let index = get_index(&driver.driver_type, context)?;
        try_write_u32_no_type_id(context.writer.as_mut().unwrap(), index)?;
    }

    Ok(())
}

pub fn try_write_i32(writer: &mut BinaryWriter, value: i32) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::I32))?;
        writer.write_i32(value)
    })
}

pub fn try_write_i64_no_type_id(
    writer: &mut BinaryWriter,
    value: i64,
) -> Result<(), BxesWriteError> {
    try_write(|| writer.write_i64(value))
}

pub fn try_write_i64(writer: &mut BinaryWriter, value: i64) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::I64))?;
        writer.write_i64(value)
    })
}

pub fn try_write_u32_no_type_id(
    writer: &mut BinaryWriter,
    value: u32,
) -> Result<(), BxesWriteError> {
    try_write(|| writer.write_u32(value))
}

pub fn try_write_u32(writer: &mut BinaryWriter, value: u32) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::U32))?;
        writer.write_u32(value)
    })
}

pub fn try_write_u64(writer: &mut BinaryWriter, value: u64) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::U64))?;
        writer.write_u64(value)
    })
}

pub fn try_write_f32(writer: &mut BinaryWriter, value: f32) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::F32))?;
        writer.write_f32(value)
    })
}

pub fn try_write_u8_no_type_id(writer: &mut BinaryWriter, value: u8) -> Result<(), BxesWriteError> {
    try_write(|| writer.write_u8(value))
}

pub fn try_write_f64(writer: &mut BinaryWriter, value: f64) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::F64))?;
        writer.write_f64(value)
    })
}

pub fn try_write_f64_no_type_id(
    writer: &mut BinaryWriter,
    value: f64,
) -> Result<(), BxesWriteError> {
    try_write(|| writer.write_f64(value))
}

pub fn try_write_bool(writer: &mut BinaryWriter, value: bool) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::Bool))?;
        writer.write_u8(if value { 1 } else { 0 })
    })
}

pub fn try_write_string(writer: &mut BinaryWriter, value: &str) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::String))?;
        writer.write_u64(value.len() as u64)?;
        writer.write_bytes(value.as_bytes())
    })
}

pub fn try_write_lifecycle(
    writer: &mut BinaryWriter,
    lifecycle: &Lifecycle,
) -> Result<(), BxesWriteError> {
    match lifecycle {
        Lifecycle::Braf(braf_lifecycle) => try_write_braf_lifecycle(writer, braf_lifecycle),
        Lifecycle::Standard(standard_lifecycle) => {
            try_write_standard_lifecycle(writer, standard_lifecycle)
        }
    }
}

pub fn try_write_braf_lifecycle(
    writer: &mut BinaryWriter,
    value: &BrafLifecycle,
) -> Result<(), BxesWriteError> {
    try_write_enum_value(writer, &TypeIds::BrafLifecycle, value)
}

fn try_write_enum_value<T: ToPrimitive>(
    writer: &mut BinaryWriter,
    type_id: &TypeIds,
    value: &T,
) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(TypeIds::to_u8(type_id).unwrap())?;
        writer.write_u8(T::to_u8(value).unwrap())
    })
}

pub fn try_write_standard_lifecycle(
    writer: &mut BinaryWriter,
    value: &StandardLifecycle,
) -> Result<(), BxesWriteError> {
    try_write_enum_value(writer, &TypeIds::StandardLifecycle, value)
}

pub fn try_write_timestamp(writer: &mut BinaryWriter, value: i64) -> Result<(), BxesWriteError> {
    try_write(|| {
        writer.write_u8(get_type_id_byte(TypeIds::Timestamp))?;
        writer.write_i64(value)
    })
}

fn try_write(
    mut write_func: impl FnMut() -> binary_rw::Result<usize>,
) -> Result<(), BxesWriteError> {
    match write_func() {
        Ok(_) => Ok(()),
        Err(error) => Err(BxesWriteError::WriteError(error)),
    }
}

pub fn try_open_write(path: &str) -> Result<FileStream, BxesWriteError> {
    match FileStream::create(path) {
        Ok(stream) => Ok(stream),
        Err(err) => Err(BxesWriteError::FailedToOpenFileForWriting(err.to_string())),
    }
}

pub fn compress_to_archive(log_path: &str, save_path: &str) -> Result<(), BxesWriteError> {
    let file = File::create(save_path).or_else(|_| Err(BxesWriteError::FailedToCreateArchive))?;
    let mut zip_writer = ZipWriter::new(file);

    let archive_log_name = Path::new(save_path).file_name().unwrap().to_str().unwrap();
    let options = FileOptions::default()
        .compression_method(zip::CompressionMethod::Deflated)
        .compression_level(Some(8));

    zip_writer
        .start_file(archive_log_name, options)
        .or_else(|_| Err(BxesWriteError::FailedToCreateArchive))?;

    let bytes = fs::read(log_path).unwrap();
    zip_writer
        .write_all(&bytes)
        .or_else(|_| Err(BxesWriteError::FailedToCreateArchive))?;

    zip_writer
        .flush()
        .or_else(|_| Err(BxesWriteError::FailedToCreateArchive))?;

    zip_writer
        .finish()
        .or_else(|_| Err(BxesWriteError::FailedToCreateArchive))?;

    Ok(())
}
