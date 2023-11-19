use super::{errors::BxesReadError, read_utils::*};
use crate::{constants::*, models::*};
use binary_rw::{BinaryReader, Endian};
use std::path::Path;

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

pub fn read_bxes_multiple_files(directory_path: &str) -> Result<BxesEventLog, BxesReadError> {
    let values = read_file(directory_path, VALUES_FILE_NAME, |reader| {
        try_read_values(reader)
    })?;

    let kv_pairs = read_file(directory_path, KEY_VALUES_FILE_NAME, |reader| {
        try_read_key_values(reader)
    })?;

    let metadata = read_file(directory_path, METADATA_FILE_NAME, |reader| {
        try_read_event_log_metadata(reader, &values, &kv_pairs)
    })?;

    let variants = read_file(directory_path, VARIANTS_FILE_NAME, |reader| {
        try_read_traces_variants(reader, &values, &kv_pairs)
    })?;

    Ok(BxesEventLog {
        version: 0,
        metadata,
        variants,
    })
}

fn read_file<T>(
    directory_path: &str,
    file_name: &str,
    reader_func: impl FnMut(&mut BinaryReader) -> Result<T, BxesReadError>,
) -> Result<T, BxesReadError> {
    let directory_path = Path::new(directory_path);
    let file_path = directory_path.join(file_name);
    let file_path = file_path.to_str().unwrap();

    execute_with_reader(file_path, reader_func)
}

fn execute_with_reader<T>(
    path: &str,
    mut reader_func: impl FnMut(&mut BinaryReader) -> Result<T, BxesReadError>,
) -> Result<T, BxesReadError> {
    let mut stream = try_open_file_stream(path)?;
    let mut reader = BinaryReader::new(&mut stream, Endian::Little);

    reader_func(&mut reader)
}
