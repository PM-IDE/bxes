use std::io::{Read, Write};

use binary_rw::{FileStream, ReadStream, SeekStream, WriteStream};

pub struct BufferedFileStream {
    stream: FileStream,
    buffer: Vec<u8>,
    occupied_size: usize,
    next_buffer_index: usize,
    file_length_bytes: usize,
}

impl BufferedFileStream {
    pub fn new(stream: FileStream, buffer_size: usize) -> Self {
        let length = stream.len().ok().unwrap();
        Self {
            stream,
            buffer: vec![0; buffer_size],
            occupied_size: 0,
            next_buffer_index: 0,
            file_length_bytes: length,
        }
    }
}

impl Write for BufferedFileStream {
    fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
        todo!()
    }

    fn flush(&mut self) -> std::io::Result<()> {
        todo!()
    }
}

impl Read for BufferedFileStream {
    fn read(&mut self, buf: &mut [u8]) -> std::io::Result<usize> {
        let mut out_buff_index = 0;

        loop {
            if out_buff_index >= buf.len() {
                break;
            }

            let to_read = buf.len() - out_buff_index;
            if self.next_buffer_index + to_read <= self.occupied_size {
                for i in 0..to_read {
                    buf[out_buff_index] = self.buffer[self.next_buffer_index + i];
                    out_buff_index += 1;
                }

                self.next_buffer_index += to_read;
                break;
            } else {
                let read_bytes = self.occupied_size - self.next_buffer_index;
                for i in 0..read_bytes {
                    buf[out_buff_index] = self.buffer[self.next_buffer_index + i];
                    out_buff_index += 1;
                }

                let current_pos = self.stream.tell().ok().unwrap();
                let remained_bytes_in_file = self.file_length_bytes - current_pos;
                self.next_buffer_index = 0;

                if remained_bytes_in_file < self.buffer.len() {
                    self.occupied_size = remained_bytes_in_file;
                    self.stream
                        .read_exact(&mut self.buffer[0..remained_bytes_in_file])?;
                } else {
                    self.occupied_size = self.buffer.len();
                    self.stream.read_exact(&mut self.buffer)?;
                }
            }
        }

        Ok(buf.len())
    }
}

impl SeekStream for BufferedFileStream {
    fn seek(&mut self, to: usize) -> binary_rw::Result<usize> {
        self.stream.seek(to)
    }

    fn tell(&mut self) -> binary_rw::Result<usize> {
        self.stream.tell()
    }

    fn len(&self) -> binary_rw::Result<usize> {
        self.stream.len()
    }
}

impl ReadStream for BufferedFileStream {}
impl WriteStream for BufferedFileStream {}
