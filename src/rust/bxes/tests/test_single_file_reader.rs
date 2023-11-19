use bxes::single_file_bxes_reader::read_bxes;

#[test]
pub fn simple_test1() {
    let log = read_bxes("/Users/aero/Programming/pmide/Procfiler/data/log.bxes").ok();
    println!("{:?}", log)
}