use std::rc::Rc;

use crate::{binary_rw::error::BinaryError, models::BxesValue};

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
