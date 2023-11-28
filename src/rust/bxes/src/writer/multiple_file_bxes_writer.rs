use std::{cell::RefCell, path::Path, rc::Rc};

use binary_rw::{BinaryWriter, Endian};

use crate::{constants, models::BxesEventLog};

use super::{
    errors::BeesWriteError,
    write_context::BxesWriteContext,
    writer_utils::{
        try_open_write, try_write_key_values, try_write_log_metadata, try_write_u32_no_type_id,
        try_write_values, try_write_variants,
    },
};

type WriterFunc = dyn for<'a, 'b> Fn(
    &'a BxesEventLog,
    Rc<RefCell<BxesWriteContext<'a, 'b>>>,
) -> Result<(), BeesWriteError>;

pub fn write_bxes_multiple_files<'a>(
    log: &'a BxesEventLog,
    directory_path: &'a str,
) -> Result<(), BeesWriteError> {
    let context = BxesWriteContext::empty();

    let writer = |file_path: &'static str, action: Box<WriterFunc>| {
        execute_with_writer(log, directory_path, file_path, &context, action)
    };

    writer(
        constants::VALUES_FILE_NAME,
        Box::new(|log, context| try_write_values(log, context)),
    )?;

    writer(
        constants::KEY_VALUES_FILE_NAME,
        Box::new(|log, context| try_write_key_values(log, context)),
    )?;

    writer(
        constants::METADATA_FILE_NAME,
        Box::new(|log, context| try_write_log_metadata(log, context)),
    )?;

    writer(
        constants::VARIANTS_FILE_NAME,
        Box::new(|log, context| try_write_variants(log, context)),
    )
}

fn execute_with_writer<'a, T>(
    log: &'a BxesEventLog,
    directory_path: &'a str,
    file_name: &'static str,
    context: &'a BxesWriteContext<'a, '_>,
    action: T,
) -> Result<(), BeesWriteError>
where
    T: for<'x> Fn(
        &'a BxesEventLog,
        Rc<RefCell<BxesWriteContext<'a, 'x>>>,
    ) -> Result<(), BeesWriteError>,
{
    let directory_path = Path::new(directory_path);
    let file_path = directory_path.join(file_name);
    let file_path = file_path.to_str().unwrap();

    let mut file_stream = try_open_write(file_path)?;
    let mut writer = BinaryWriter::new(&mut file_stream, Endian::Little);

    try_write_u32_no_type_id(&mut writer, log.version)?;
    action(log, Rc::new(RefCell::new(context.with_writer(&mut writer))))
}
