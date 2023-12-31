use std::io::{Write, Read};

use binary_rw::{FileStream, SeekStream, ReadStream, WriteStream};

pub struct BufferedFileStream {
    stream: FileStream,
    buffer: Vec<u8>,
    occupied_size: usize,
    next_buffer_index: usize
}

impl BufferedFileStream {
    pub fn new(stream: FileStream, buffer_size: usize) -> Self {
        Self {
            stream,
            buffer: vec![0; buffer_size],
            occupied_size: 0,
            next_buffer_index: 0
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
        if self.next_buffer_index + buf.len() <= self.occupied_size {
            for i in 0..buf.len() {
                buf[i] = self.buffer[self.next_buffer_index + i]
            }

            self.next_buffer_index += buf.len()
        } else {
            let read_bytes = self.occupied_size - self.next_buffer_index;
            for i in 0..read_bytes {
                buf[i] = self.buffer[self.next_buffer_index + i];
            }

            let to_read_bytes = buf.len() - read_bytes;
            match self.stream.read(&mut self.buffer) {
                Ok(read_from_stream_bytes) => {
                    self.occupied_size = read_from_stream_bytes;
                    self.next_buffer_index = 0;
                },
                Err(err) => return Err(err),
            }

            for i in 0..to_read_bytes {
                buf[read_bytes + i] = self.buffer[i];
            }

            self.next_buffer_index = to_read_bytes;
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