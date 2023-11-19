use bxes::read::single_file_bxes_reader::read_bxes;

pub fn main() {
    read_bxes(r"/Users/aero/Programming/pmide/Procfiler/data/log.bxes").ok();
}
