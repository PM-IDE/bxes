use binary_rw::{BinaryReader, Endian};

use crate::models::*;

use super::{errors::BxesReadError, read_utils::*};

pub fn read_bxes(path: &str) -> Result<BxesEventLog, BxesReadError> {
    let mut stream = try_open_file_stream(path)?;
    let mut reader = BinaryReader::new(&mut stream, Endian::Little);
    let version = try_read_u32(&mut reader)?;

    let values = try_read_values(&mut reader)?;
    let kv_pairs = try_read_key_values(&mut reader)?;
    let metadata = try_read_event_log_metadata(&mut reader, &values, &kv_pairs)?;
    let variants = try_read_traces_variants(&mut reader, &values, &kv_pairs)?;

    Ok(BxesEventLog {
        version,
        metadata,
        variants,
    })
}
