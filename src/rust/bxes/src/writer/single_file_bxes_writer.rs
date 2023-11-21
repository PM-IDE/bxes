use std::{cell::RefCell, collections::HashMap, rc::Rc};

use binary_rw::{BinaryWriter, Endian};

use crate::models::BxesEventLog;

use super::{
    errors::BxesWriteError,
    write_context::BxesWriteContext,
    writer_utils::{
        try_open_write, try_write_key_values, try_write_log_metadata, try_write_values,
        try_write_variants, try_write_version,
    },
};

pub fn write_bxes(path: &str, log: &BxesEventLog) -> Result<(), BxesWriteError> {
    let mut stream = try_open_write(path)?;
    let mut writer = BinaryWriter::new(&mut stream, Endian::Little);

    let context = Rc::new(RefCell::new(BxesWriteContext {
        values_indices: Rc::new(RefCell::new(HashMap::new())),
        kv_indices: Rc::new(RefCell::new(HashMap::new())),
        writer: &mut writer,
    }));

    try_write_version(context.borrow_mut().writer, log.version)?;
    try_write_values(log, context.clone())?;
    try_write_key_values(log, context.clone())?;
    try_write_log_metadata(log, context.clone())?;
    try_write_variants(log, context.clone())?;

    Ok(())
}
