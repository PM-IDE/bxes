use bxes::read::multiple_files_bxes_reader::read_bxes_multiple_files;

#[test]
pub fn test_multiple_file_reader() {
    println!(
        "{:?}",
        read_bxes_multiple_files(r"/var/folders/96/6fnlvzmx7vq4vnr8vcbjcdmw0000gn/T/gW0yi3")
    );
}
