use std::collections::hash_map::VacantEntry;
use std::env::var;
use std::string::FromUtf8Error;
use binary_rw::{BinaryReader, Endian, FileStream, SeekStream};
use num_derive::FromPrimitive;
use crate::num::FromPrimitive;
use crate::type_ids;

pub enum BxesValue {
    Int32(i32),
    Int64(i64),
    Uint32(u32),
    Uint64(u64),
    Float32(f32),
    Float64(f64),
    String(String),
    Bool(bool),
    Timestamp(i64),
    BrafLifecycle(BrafLifecycle),
    StandardLifecycle(StandardLifecycle)
}

#[derive(FromPrimitive)]
pub enum BrafLifecycle {
    Unspecified = 0,
    Closed = 1,
    ClosedCancelled = 2,
    ClosedCancelledAborted = 3,
    ClosedCancelledError = 4,
    ClosedCancelledExited = 5,
    ClosedCancelledObsolete = 6,
    ClosedCancelledTerminated = 7,
    Completed = 8,
    CompletedFailed = 9,
    CompletedSuccess = 10,
    Open = 11,
    OpenNotRunning = 12,
    OpenNotRunningAssigned = 13,
    OpenNotRunningReserved = 14,
    OpenNotRunningSuspendedAssigned = 15,
    OpenNotRunningSuspendedReserved = 16,
    OpenRunning = 17,
    OpenRunningInProgress = 18,
    OpenRunningSuspended = 19,
}

#[derive(FromPrimitive)]
pub enum StandardLifecycle {
    Unspecified = 0,
    Assign = 1,
    AteAbort = 2,
    Autoskip = 3,
    Complete = 4,
    ManualSkip = 5,
    PiAbort = 6,
    ReAssign = 7,
    Resume = 8,
    Schedule = 9,
    Start = 10,
    Suspend = 11,
    Unknown = 12,
    Withdraw = 13,
}

pub struct BxesEventLog {

}

pub struct BxesTraceVariant {
    pub traces_count: u32,
    pub events: Vec<BxesEvent>
}

pub struct BxesEvent {

}

pub enum BxesReadError {
    FailedToOpenFile(String),
    FailedToReadValue(FailedToReadValueError),
    FailedToReadPos(String),
    FailedToCreateUtf8String(FromUtf8Error),
    FailedToParseTypeId(u8),
}

pub struct FailedToReadValueError {
    pub offset: usize,
    pub message: String
}

impl FailedToReadValueError {
    pub fn new(offset: usize, message: String) -> Self {
        Self {
            offset,
            message
        }
    }
}

pub fn read_bxes(path: &str) -> Result<BxesEventLog, BxesReadError> {
    let mut reader = BinaryReader::new(&mut try_open_file_stream(path)?, Endian::Little);
    let version = try_read_u32(&mut reader);

    let values = try_read_values(&mut reader);
    let kv_pairs = try_read_key_values(&mut reader);

}

fn try_read_traces_variants(reader: &mut BinaryReader) -> Result<Vec<BxesTraceVariant>, BxesReadError> {
    let mut variants = vec![];
    let variant_count = try_read_u32(reader)?;

    for _ in 0..variant_count {
        variants.push(try_read_trace_variant(reader)?);
    }

    Ok(variants)
}

fn try_read_trace_variant(reader: &mut BinaryReader) -> Result<BxesTraceVariant, BxesReadError> {
    let traces_count = try_read_u32(reader)?;
    let events_count = try_read_u32(reader)?;

    for _ in 0..events_count {
    }


}

fn try_read_key_values(reader: &mut BinaryReader) -> Result<Vec<(u32, u32)>, BxesReadError> {
    let mut key_values = vec![];

    let key_values_count = try_read_u32(reader)?;
    for _ in 0..key_values_count {
        key_values.push((try_read_u32(reader)?, try_read_u32(reader)?));
    }

    Ok(key_values)
}

fn try_read_values(reader: &mut BinaryReader) -> Result<Vec<BxesValue>, BxesReadError> {
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
        type_ids::STRING => Ok(BxesValue::String(try_read_string(reader)?)),
        type_ids::TIMESTAMP => Ok(BxesValue::Timestamp(try_read_i64(reader)?)),
        type_ids::BRAF_LIFECYCLE => Ok(BxesValue::BrafLifecycle(try_read_braf_lifecycle(reader)?)),
        type_ids::STANDARD_LIFECYCLE => Ok(BxesValue::StandardLifecycle(try_read_standard_lifecycle(reader)?)),
        _ => Err(BxesReadError::FailedToParseTypeId(type_id))
    }
}

fn try_read_i32(reader: &mut BinaryReader) -> Result<i32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_i32())
}

fn try_read_i64(reader: &mut BinaryReader) -> Result<i64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_i64())
}

fn try_read_u32(reader: &mut BinaryReader) -> Result<u32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u32())
}

fn try_read_u64(reader: &mut BinaryReader) -> Result<u64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u64())
}

fn try_read_f32(reader: &mut BinaryReader) -> Result<f32, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_f32())
}

fn try_read_f64(reader: &mut BinaryReader) -> Result<f64, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_f64())
}

fn try_read_bool(reader: &mut BinaryReader) -> Result<bool, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_bool())
}

fn try_read_u8(reader: &mut BinaryReader) -> Result<u8, BxesReadError> {
    try_read_primitive_value(try_tell_pos(reader)?, || reader.read_u8())
}

fn try_read_string(reader: &mut BinaryReader) -> Result<String, BxesReadError> {
    let string_length = try_read_u32(reader)?;
    let bytes = try_read_bytes(reader, string_length as usize)?;

    match String::from_utf8(bytes) {
        Ok(string) => Ok(string),
        Err(err) => Err(BxesReadError::FailedToCreateUtf8String(err))
    }
}

fn try_read_bytes(reader: &mut BinaryReader, length: usize) -> Result<Vec<u8>, BxesReadError> {
    let offset = try_tell_pos(reader)?;
    match reader.read_bytes(length) {
        Ok(bytes) => Ok(bytes),
        Err(err) => Err(BxesReadError::FailedToReadValue(FailedToReadValueError::new(offset, err.to_string())))
    }
}

fn try_read_braf_lifecycle(reader: &mut BinaryReader) -> Result<BrafLifecycle, BxesReadError> {
    try_read_enum::<BrafLifecycle>(reader)
}

fn try_read_enum<T: FromPrimitive>(reader: &mut BinaryReader) -> Result<T, BxesReadError> {
    let offset = try_tell_pos(reader)?;
    match reader.read_u8() {
        Ok(byte) => Ok(T::from_u8(byte).unwrap()),
        Err(err) => Err(BxesReadError::FailedToReadValue(FailedToReadValueError::new(offset, err.to_string())))
    }
}

fn try_read_standard_lifecycle(reader: &mut BinaryReader) -> Result<StandardLifecycle, BxesReadError> {
    try_read_enum::<StandardLifecycle>(reader)
}

fn try_open_file_stream(path: &str) -> Result<FileStream, BxesReadError> {
    match FileStream::open(path) {
        Ok(fs) => Ok(fs),
        Err(err) => Err(BxesReadError::FailedToOpenFile(err.to_string()))
    }
}

fn try_read_primitive_value<T>(reader_pos: usize, read_func: impl Fn() -> binary_rw::Result<T>) -> Result<T, BxesReadError> {
    match read_func() {
        Ok(value) => Ok(value),
        Err(err) => Err(BxesReadError::FailedToReadValue(FailedToReadValueError::new(reader_pos, err.to_string())))
    }
}

fn try_tell_pos(reader: &mut BinaryReader) -> Result<usize, BxesReadError> {
    match reader.tell() {
        Ok(pos) => Ok(pos),
        Err(err) => Err(BxesReadError::FailedToReadPos(err.to_string()))
    }
}