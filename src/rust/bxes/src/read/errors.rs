use std::string::FromUtf8Error;

use crate::models::BxesValue;

#[derive(Debug)]
pub enum BxesReadError {
    FailedToOpenFile(String),
    FailedToReadValue(FailedToReadValueError),
    FailedToReadPos(String),
    FailedToCreateUtf8String(FromUtf8Error),
    FailedToParseTypeId(u8),
    FailedToIndexValue(usize),
    FailedToIndexKeyValue(usize),
    LifecycleOfEventOutOfRange,
    EventAttributeKeyIsNotAString,
    VersionsMismatchError(VersionsMismatchError),
    FailedToExtractArchive,
    TooManyFilesInArchive,
    FailedToCreateTempDir,
    InvalidArchive(String),
    ExpectedString(BxesValue),
    Leb128ReadError(String)
}

#[derive(Debug)]
pub struct FailedToReadValueError {
    pub offset: usize,
    pub message: String,
}

impl FailedToReadValueError {
    pub fn new(offset: usize, message: String) -> Self {
        Self { offset, message }
    }
}

#[derive(Debug)]
pub struct VersionsMismatchError {
    previous_version: u32,
    current_version: u32,
}

impl VersionsMismatchError {
    pub fn new(previous_version: u32, current_version: u32) -> Self {
        Self {
            previous_version,
            current_version,
        }
    }
}
