use std::rc::Rc;

use binary_rw::BinaryError;

use crate::models::BxesValue;

#[derive(Debug)]
pub enum BxesWriteError {
    FailedToOpenFileForWriting(String),
    WriteError(BinaryError),
    FailedToGetWriterPosition(String),
    FailedToSeek(String),
    FailedToFindKeyValueIndex((Rc<Box<BxesValue>>, Rc<Box<BxesValue>>)),
    FailedToFindValueIndex(Rc<Box<BxesValue>>),
    FailedToCreateTempFile,
    FailedToCreateArchive,
    LebWriteError(String),
}
