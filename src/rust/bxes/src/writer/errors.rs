use binary_rw::BinaryError;

use crate::models::BxesValue;

#[derive(Debug)]
pub enum BxesWriteError {
    FailedToOpenFileForWriting(String),
    WriteError(BinaryError),
    FailedToGetWriterPosition(String),
    FailedToSeek(String),
    FailedToFindKeyValueIndex((BxesValue, BxesValue)),
    FailedToFindValueIndex(BxesValue),
    FailedToCreateTempFile,
    FailedToCreateArchive,
}
