use std::{cell::RefCell, collections::HashMap, rc::Rc};

use binary_rw::BinaryWriter;

use crate::models::BxesValue;

pub struct BxesWriteContext<'a> {
    pub values_indices: Rc<RefCell<HashMap<&'a BxesValue, usize>>>,
    pub kv_indices: Rc<RefCell<HashMap<(&'a BxesValue, &'a BxesValue), usize>>>,
    pub writer: &'a mut BinaryWriter<'a>,
}

impl<'a> BxesWriteContext<'a> {
    pub fn with_writer(&self, new_writer: &'a mut BinaryWriter<'a>) -> Self {
        Self {
            values_indices: self.values_indices.clone(),
            kv_indices: self.kv_indices.clone(),
            writer: new_writer,
        }
    }
}
