use std::collections::HashMap;

use binary_rw::{BinaryError, BinaryWriter, Endian, FileStream, SeekStream};
use num_traits::ToPrimitive;

use crate::{
    models::{BrafLifecycle, BxesEventLog, BxesValue, StandardLifecycle},
    type_ids,
};

pub enum BxesWriteError {
    FailedToOpenFileForWriting(String),
    WriteError(BinaryError),
    FailedToGetWriterPosition(String),
    FailedToSeek(String),
}

pub struct BxesWriteContext<'a> {
    pub values_indices: HashMap<BxesValue, usize>,
    pub kv_indices: HashMap<(BxesValue, BxesValue), usize>,
    pub writer: &'a mut BinaryWriter<'a>,
}

pub fn write_bxes(path: &str, log: &BxesEventLog) -> Result<(), BxesWriteError> {
    let mut stream = try_open_write(path)?;
    let mut writer = BinaryWriter::new(&mut stream, Endian::Little);

    let mut context = &mut BxesWriteContext {
        values_indices: HashMap::new(),
        kv_indices: HashMap::new(),
        writer: &mut writer,
    };

    try_write_values(log, context)
}

pub fn try_write_version(writer: &mut BinaryWriter, version: u32) -> Result<(), BxesWriteError> {
    try_write_u32(writer, version)
}

pub fn try_write_values(
    log: &BxesEventLog,
    context: &mut BxesWriteContext,
) -> Result<(), BxesWriteError> {
    write_collection_and_count(context, |context| {
        let mut count = 0;
        if let Some(metadata) = log.metadata.as_ref() {
            for (key, value) in metadata {
                if try_write_value(&BxesValue::String(key.clone()), context)? {
                    count += 1
                }

                if try_write_value(&value, context)? {
                    count += 1
                }
            }
        }

        for variant in &log.variants {
            for event in &variant.events {
                if let Some(attributes) = event.attributes.as_ref() {
                    for (key, value) in attributes {
                        if try_write_value(&BxesValue::String(key.clone()), context)? {
                            count += 1
                        }

                        if try_write_value(&value, context)? {
                            count += 1
                        }
                    }
                }
            }
        }

        Ok(count)
    })
}

fn write_collection_and_count(
    context: &mut BxesWriteContext,
    mut writer_action: impl FnMut(&mut BxesWriteContext) -> Result<u32, BxesWriteError>,
) -> Result<(), BxesWriteError> {
    let pos = try_tell_pos(context.writer)?;

    try_write_u32(context.writer, 0)?;

    let count = writer_action(context)?;

    let current_pos = try_tell_pos(context.writer)?;
    try_seek(context.writer, pos)?;
    try_write_u32(context.writer, count)?;
    try_seek(context.writer, current_pos)
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

pub fn try_write_value(
    value: &BxesValue,
    context: &mut BxesWriteContext,
) -> Result<bool, BxesWriteError> {
    if context.values_indices.contains_key(value) {
        return Ok(false);
    }

    context
        .values_indices
        .insert(value.clone(), context.values_indices.len());

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

pub fn try_write_i64(writer: &mut BinaryWriter, value: i64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_64)?;
        writer.write_i64(value)
    })
}

pub fn try_write_u32(writer: &mut BinaryWriter, value: u32) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_64)?;
        writer.write_u32(value)
    })
}

pub fn try_write_u64(writer: &mut BinaryWriter, value: u64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
        writer.write_u64(value)
    })
}

pub fn try_write_u8(writer: &mut BinaryWriter, value: u8) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
        writer.write_u8(value)
    })
}

pub fn try_write_f32(writer: &mut BinaryWriter, value: f32) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
        writer.write_f32(value)
    })
}

pub fn try_write_f64(writer: &mut BinaryWriter, value: f64) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
        writer.write_f64(value)
    })
}

pub fn try_write_bool(writer: &mut BinaryWriter, value: bool) -> Result<(), BxesWriteError> {
    try_write_primitive_value(|| {
        writer.write_u8(type_ids::I_32)?;
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
        writer.write_u8(type_ids::BRAF_LIFECYCLE)?;
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

fn try_open_write(path: &str) -> Result<FileStream, BxesWriteError> {
    match FileStream::write(path) {
        Ok(stream) => Ok(stream),
        Err(err) => Err(BxesWriteError::FailedToOpenFileForWriting(err.to_string())),
    }
}
