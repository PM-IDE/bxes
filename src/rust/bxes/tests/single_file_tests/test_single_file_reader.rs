use bxes::{
    read::single_file_bxes_reader::read_bxes,
    writer::{
        multiple_file_bxes_writer::write_bxes_multiple_files, single_file_bxes_writer::write_bxes,
    },
};
use tempfile::TempDir;

use crate::test_core::random_log::{self, generate_random_log};

#[test]
pub fn simple_test1() {
    let log = generate_random_log();
    let temp_dir = TempDir::new().unwrap();

    write_bxes(temp_dir.path().as_os_str().to_str().unwrap(), &log).ok();
}
