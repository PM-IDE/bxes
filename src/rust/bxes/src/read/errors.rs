use std::string::FromUtf8Error;

#[derive(Debug)]
pub enum BxesReadError {
    FailedToOpenFile(String),
    FailedToReadValue(FailedToReadValueError),
    FailedToReadPos(String),
    FailedToCreateUtf8String(FromUtf8Error),
    FailedToParseTypeId(u8),
    FailedToIndexValue(usize),
    FailedToIndexKeyValue(usize),
    NameOfEventIsNotAString,
    LifecycleOfEventOutOfRange,
    EventAttributeKeyIsNotAString,
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
