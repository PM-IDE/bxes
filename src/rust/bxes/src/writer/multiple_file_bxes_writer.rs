use crate::models::BxesEventLog;

use super::errors::BxesWriteError;

pub fn write_bxes_multiple_files(
    log: &BxesEventLog,
    directory_path: &str,
) -> Result<(), BxesWriteError> {
    Ok(())
}
