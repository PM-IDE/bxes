use std::rc::Rc;

use binary_rw::{BinaryReader, FileStream, SeekStream};
use num_traits::FromPrimitive;

use crate::{models::*, type_ids};

use super::errors::*;

pub fn try_read_event_log_metadata(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<Option<Vec<(Rc<Box<String>>, BxesValue)>>, BxesReadError> {
    let metadata_count = try_read_u32(reader)?;
    if metadata_count == 0 {
        Ok(None)
    } else {
        let mut metadata = vec![];

        for _ in 0..metadata_count {
            metadata.push(try_read_kv_pair(reader, values, kv_pairs)?);
        }

        Ok(Some(metadata))
    }
}

pub fn try_read_traces_variants(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<Vec<BxesTraceVariant>, BxesReadError> {
    let mut variants = vec![];
    let variant_count = try_read_u32(reader)?;

    for _ in 0..variant_count {
        variants.push(try_read_trace_variant(reader, values, kv_pairs)?);
    }

    Ok(variants)
}

fn try_read_trace_variant(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<BxesTraceVariant, BxesReadError> {
    let traces_count = try_read_u32(reader)?;
    let events_count = try_read_u32(reader)?;
    let mut events = vec![];

    for _ in 0..events_count {
        events.push(try_read_event(reader, values, kv_pairs)?);
    }

    Ok(BxesTraceVariant {
        traces_count,
        events,
    })
}

fn try_read_event(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<BxesEvent, BxesReadError> {
    let name_index = try_read_u32(reader)? as usize;
    let name = values.get(name_index);

    if name.is_none() {
        return Err(BxesReadError::FailedToIndexValue(name_index));
    }

    let name_string = match name.unwrap() {
        BxesValue::String(string) => string,
        _ => return Err(BxesReadError::NameOfEventIsNotAString),
    };

    let timestamp = try_read_i64(reader)?;
    let lifecycle = match try_read_bxes_value(reader)? {
        BxesValue::StandardLifecycle(lifecycle) => Lifecycle::Standard(lifecycle),
        BxesValue::BrafLifecycle(lifecycle) => Lifecycle::Braf(lifecycle),
        _ => return Err(BxesReadError::LifecycleOfEventOutOfRange),
    };

    Ok(BxesEvent {
        name: name_string.clone(),
        timestamp,
        lifecycle,
        attributes: try_read_event_attributes(reader, values, kv_pairs)?,
    })
}

fn try_read_event_attributes(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<Option<Vec<(Rc<Box<String>>, BxesValue)>>, BxesReadError> {
    let attributes_count = try_read_u32(reader)?;
    if attributes_count == 0 {
        Ok(None)
    } else {
        let mut attributes = vec![];
        for _ in 0..attributes_count {
            let pair = try_read_kv_pair(reader, values, kv_pairs)?;
            attributes.push(pair);
        }

        Ok(Some(attributes))
    }
}

fn try_read_kv_pair(
    reader: &mut BinaryReader,
    values: &Vec<BxesValue>,
    kv_pairs: &Vec<(u32, u32)>,
) -> Result<(Rc<Box<String>>, BxesValue), BxesReadError> {
    let kv_index = try_read_u32(reader)? as usize;
    let kv_pair = match kv_pairs.get(kv_index) {
        None => return Err(BxesReadError::FailedToIndexKeyValue(kv_index)),
        Some(pair) => pair,
    };

    let key_index = kv_pair.0 as usize;
    let key = match values.get(key_index) {
        None => return Err(BxesReadError::FailedToIndexValue(key_index)),
        Some(value) => value,
    };

    let key_string = match key {
        BxesValue::String(string) => string.clone(),
        _ => return Err(BxesReadError::EventAttributeKeyIsNotAString),
    };

    let value_index = kv_pair.1 as usize;
    let value = match values.get(value_index) {
        None => return Err(BxesReadError::FailedToIndexValue(value_index)),
        Some(value) => value,
    };

    Ok((key_string, value.clone()))
}

pub fn try_read_key_values(reader: &mut BinaryReader) -> Result<Vec<(u32, u32)>, BxesReadError> {
    let mut key_values = vec![];

    let key_values_count = try_read_u32(reader)?;
    for _ in 0..key_values_count {
        key_values.push((try_read_u32(reader)?, try_read_u32(reader)?));
    }

    Ok(key_values)
}

pub fn try_read_values(reader: &mut BinaryReader) -> Result<Vec<BxesValue>, BxesReadError> {
    let mut values = vec![];

    let values_count = try_read_u32(reader)?;
    for _ in 0..values_count {
        values.push(try_read_bxes_value(reader)?);
    }

    Ok(values)
}

fn try_read_bxes_value(reader: &mut BinaryReader) -> Result<BxesValue, BxesReadError> {
    let type_id = try_read_u8(reader)?;

    match type_id {
        type_ids::I_32 => Ok(BxesValue::Int32(try_read_i32(reader)?)),
        type_ids::I_64 => Ok(BxesValue::Int64(try_read_i64(reader)?)),
        type_ids::U_32 => Ok(BxesValue::Uint32(try_read_u32(reader)?)),
        type_ids::U_64 => Ok(BxesValue::Uint64(try_read_u64(reader)?)),
        type_ids::F_32 => Ok(BxesValue::Float32(try_read_f32(reader)?)),
        type_ids::F_64 => Ok(BxesValue::Float64(try_read_f64(reader)?)),
        type_ids::BOOL => Ok(BxesValue::Bool(try_read_bool(reader)?)),
        type_ids::STRING => Ok(BxesValue::String(Rc::new(Box::new(try_read_string(
            reader,
        )?)))),
        type_ids::TIMESTAMP => Ok(BxesValue::Timestamp(try_read_i64(reader)?)),
        type_ids::BRAF_LIFECYCLE => Ok(BxesValue::BrafLifecycle(try_read_braf_lifecycle(reader)?)),
        type_ids::STANDARD_LIFECYCLE => Ok(BxesValue::StandardLifecycle(
            try_read_standard_lifecycle(reader)?,
        )),
        _ => Err(BxesReadError::FailedToParseTypeId(type_id)),
    }
}

pub fn try_read_i32(reader: &mut BinaryReader) -> Result<i32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_i32())
}

pub fn try_read_i64(reader: &mut BinaryReader) -> Result<i64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_i64())
}

pub fn try_read_u32(reader: &mut BinaryReader) -> Result<u32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u32())
}

pub fn try_read_u64(reader: &mut BinaryReader) -> Result<u64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u64())
}

pub fn try_read_f32(reader: &mut BinaryReader) -> Result<f32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_f32())
}

pub fn try_read_f64(reader: &mut BinaryReader) -> Result<f64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_f64())
}

pub fn try_read_bool(reader: &mut BinaryReader) -> Result<bool, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_bool())
}

pub fn try_read_u8(reader: &mut BinaryReader) -> Result<u8, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u8())
}

fn try_read_string(reader: &mut BinaryReader) -> Result<String, BxesReadError> {
    let string_length = try_read_u64(reader)?;
    let bytes = try_read_bytes(reader, string_length as usize)?;

    match String::from_utf8(bytes) {
        Ok(string) => Ok(string),
        Err(err) => Err(BxesReadError::FailedToCreateUtf8String(err)),
    }
}

fn try_read_bytes(reader: &mut BinaryReader, length: usize) -> Result<Vec<u8>, BxesReadError> {
    let offset = try_tell_pos(reader)?;
    match reader.read_bytes(length) {
        Ok(bytes) => Ok(bytes),
        Err(err) => Err(BxesReadError::FailedToReadValue(
            FailedToReadValueError::new(offset, err.to_string()),
        )),
    }
}

fn try_read_braf_lifecycle(reader: &mut BinaryReader) -> Result<BrafLifecycle, BxesReadError> {
    try_read_enum::<BrafLifecycle>(reader)
}

fn try_read_enum<T: FromPrimitive>(reader: &mut BinaryReader) -> Result<T, BxesReadError> {
    let offset = try_tell_pos(reader)?;
    match reader.read_u8() {
        Ok(byte) => Ok(T::from_u8(byte).unwrap()),
        Err(err) => Err(BxesReadError::FailedToReadValue(
            FailedToReadValueError::new(offset, err.to_string()),
        )),
    }
}

fn try_read_standard_lifecycle(
    reader: &mut BinaryReader,
) -> Result<StandardLifecycle, BxesReadError> {
    try_read_enum::<StandardLifecycle>(reader)
}

pub fn try_open_file_stream(path: &str) -> Result<FileStream, BxesReadError> {
    match FileStream::open(path) {
        Ok(fs) => Ok(fs),
        Err(err) => Err(BxesReadError::FailedToOpenFile(err.to_string())),
    }
}

fn try_read_primitive_value<T>(
    reader_pos: usize,
    mut read_func: impl FnMut() -> binary_rw::Result<T>,
) -> Result<T, BxesReadError> {
    match read_func() {
        Ok(value) => Ok(value),
        Err(err) => Err(BxesReadError::FailedToReadValue(
            FailedToReadValueError::new(reader_pos, err.to_string()),
        )),
    }
}

fn try_tell_pos(reader: &mut BinaryReader) -> Result<usize, BxesReadError> {
    match reader.tell() {
        Ok(pos) => Ok(pos),
        Err(err) => Err(BxesReadError::FailedToReadPos(err.to_string())),
    }
}
