use binary_rw::{BinaryWriter, FileStream, SeekStream};
use num_traits::ToPrimitive;
use std::{cell::RefCell, rc::Rc};

use crate::{
    models::{BrafLifecycle, BxesEvent, BxesEventLog, BxesValue, Lifecycle, StandardLifecycle},
    type_ids,
};

use super::{errors::BxesWriteError, write_context::BxesWriteContext};

pub fn try_write_variants(
    log: &BxesEventLog,
    context: Rc<RefCell<BxesWriteContext>>,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context.clone(), || {
        for variant in &log.variants {
            try_write_u32_no_type_id(context.borrow_mut().writer, variant.traces_count)?;
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
            try_write_u32_no_type_id(context.borrow_mut().writer, index as u32)?;
        } else {
            return Err(BxesWriteError::FailedToFindValueIndex(event.name.clone()));
        };
    }

    try_write_i64_no_type_id(context.borrow_mut().writer, event.timestamp)?;
    try_write_lifecycle(context.borrow_mut().writer, &event.lifecycle)?;
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
                    try_write_u32_no_type_id(context.borrow_mut().writer, index as u32)?;
                } else {
                    return Err(BxesWriteError::FailedToFindKeyValueIndex((
                        key.clone(),
                        value.clone(),
                    )));
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
    context: Rc<RefCell<BxesWriteContext<'b>>>,
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

                        try_write_u32_no_type_id(context.borrow_mut().writer, key_index as u32)?;
                        try_write_u32_no_type_id(context.borrow_mut().writer, value_index as u32)?;

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
        for (key, value) in metadata {
            action(ValueOrKeyValue::Value(key))?;
            action(ValueOrKeyValue::Value(value))?;

            action(ValueOrKeyValue::KeyValue((key, value)))?;
        }
    }

    for variant in &log.variants {
        for event in &variant.events {
            action(ValueOrKeyValue::Value(&event.name))?;
            if let Some(attributes) = event.attributes.as_ref() {
                for (key, value) in attributes {
                    action(ValueOrKeyValue::Value(key))?;
                    action(ValueOrKeyValue::Value(value))?;

                    action(ValueOrKeyValue::KeyValue((key, value)))?;
                }
            }
        }
    }

    Ok(())
}

pub fn try_write_version(writer: &mut BinaryWriter, version: u32) -> Result<(), BxesWriteError> {
    try_write_u32_no_type_id(writer, version)
}

pub fn try_write_values<'a: 'b, 'b>(
    log: &'a BxesEventLog,
    context: Rc<RefCell<BxesWriteContext<'b>>>,
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
    let pos = try_tell_pos(context.borrow_mut().writer)?;

    try_write_u32_no_type_id(context.borrow_mut().writer, 0)?;

    let count = writer_action()?;

    let current_pos = try_tell_pos(context.borrow_mut().writer)?;
    try_seek(context.borrow_mut().writer, pos)?;
    try_write_u32_no_type_id(context.borrow_mut().writer, count)?;
    try_seek(context.borrow_mut().writer, current_pos)
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
    context: &mut BxesWriteContext<'b>,
) -> Result<bool, BxesWriteError> {
    if context.values_indices.borrow().contains_key(value) {
        return Ok(false);
    }

    let len = context.values_indices.borrow().len();
    context.values_indices.borrow_mut().insert(value, len);

    match value {
        BxesValue::Int32(value) => try_write_i32(context.writer, *value),
        BxesValue::Int64(value) => try_write_i64(context.writer, *value),
        BxesValue::Uint32(value) => try_write_u32(context.writer, *value),
        BxesValue::Uint64(value) => try_write_u64(context.writer, *value),
        BxesValue::Float32(value) => try_write_f32(context.writer, *value),
        BxesValue::Float64(value) => try_write_f64(context.writer, *value),
        BxesValue::String(value) => try_write_string(context.writer, value.as_str()),
        BxesValue::Bool(value) => try_write_bool(context.writer, *value),
        BxesValue::Timestamp(value) => try_write_timestamp(context.writer, *value),
        BxesValue::BrafLifecycle(value) => try_write_braf_lifecycle(context.writer, value),
        BxesValue::StandardLifecycle(value) => try_write_standard_lifecycle(context.writer, value),
    }?;

    Ok(true)
}

pub fn try_write_i32(writer: &mut BinaryWriter, value: i32) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
        writer.write_i32(value)
    })
}

pub fn try_write_i64_no_type_id(
    writer: &mut BinaryWriter,
    value: i64,
) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| writer.write_i64(value))
}

pub fn try_write_i64(writer: &mut BinaryWriter, value: i64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_64)?;
        writer.write_i64(value)
    })
}

pub fn try_write_u32_no_type_id(
    writer: &mut BinaryWriter,
    value: u32,
) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| writer.write_u32(value))
}

pub fn try_write_u32(writer: &mut BinaryWriter, value: u32) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::U_32)?;
        writer.write_u32(value)
    })
}

pub fn try_write_u64(writer: &mut BinaryWriter, value: u64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::U_64)?;
        writer.write_u64(value)
    })
}

pub fn try_write_f32(writer: &mut BinaryWriter, value: f32) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::F_32)?;
        writer.write_f32(value)
    })
}

pub fn try_write_f64(writer: &mut BinaryWriter, value: f64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::F_64)?;
        writer.write_f64(value)
    })
}

pub fn try_write_bool(writer: &mut BinaryWriter, value: bool) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::BOOL)?;
        writer.write_u8(if value { 1 } else { 0 })
    })
}

pub fn try_write_string(writer: &mut BinaryWriter, value: &str) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::STRING)?;
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
    try_write_enum_value(writer, type_ids::BRAF_LIFECYCLE, value)
}

fn try_write_enum_value<T: ToPrimitive>(
    writer: &mut BinaryWriter,
    type_id: u8,
    value: &T,
) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_id)?;
        writer.write_u8(T::to_u8(value).unwrap())
    })
}

pub fn try_write_standard_lifecycle(
    writer: &mut BinaryWriter,
    value: &StandardLifecycle,
) -> Result<(), BxesWriteError> {
    try_write_enum_value(writer, type_ids::STANDARD_LIFECYCLE, value)
}

pub fn try_write_timestamp(writer: &mut BinaryWriter, value: i64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::TIMESTAMP)?;
        writer.write_i64(value)
    })
}

fn try_write_primitive_value(
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
