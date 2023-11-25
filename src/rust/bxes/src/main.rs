use bxes::{read::single_file_bxes_reader::read_bxes, writer::single_file_bxes_writer::write_bxes};

pub fn main() {
    let log = read_bxes(r"/Users/aero/Programming/pmide/Procfiler/data/log.bxes")
        .ok()
        .unwrap();

    write_bxes(
        r"/Users/aero/Programming/pmide/Procfiler/data/log321.bxes",
        &log,
    )
    .ok();

    let read_log = read_bxes(r"/Users/aero/Programming/pmide/Procfiler/data/log321.bxes")
        .ok()
        .unwrap();

    assert!(log.eq(&read_log));
}
