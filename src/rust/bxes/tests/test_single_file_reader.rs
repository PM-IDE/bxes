use bxes::read::single_file_bxes_reader::read_bxes;

#[test]
pub fn simple_test1() {
    read_bxes(r"/Users/aero/Programming/pmide/Procfiler/data/log.bxes").ok();
}