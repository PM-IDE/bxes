use std::{cell::RefCell, collections::HashMap, rc::Rc};

use binary_rw::BinaryWriter;

use crate::models::BxesValue;

pub struct BxesWriteContext<'a: 'b, 'b> {
    pub values_indices: Rc<RefCell<HashMap<&'a BxesValue, usize>>>,
    pub kv_indices: Rc<RefCell<HashMap<(&'a BxesValue, &'a BxesValue), usize>>>,
    pub writer: Option<&'b mut BinaryWriter<'b>>,
}

impl<'a, 'b> BxesWriteContext<'a, 'b> {
    pub fn empty() -> Self {
        Self {
            values_indices: Rc::new(RefCell::new(HashMap::new())),
            kv_indices: Rc::new(RefCell::new(HashMap::new())),
            writer: None,
        }
    }

    pub fn new(writer: &'b mut BinaryWriter<'b>) -> Self {
        Self {
            values_indices: Rc::new(RefCell::new(HashMap::new())),
            kv_indices: Rc::new(RefCell::new(HashMap::new())),
            writer: Some(writer),
        }
    }

    pub fn with_writer<'c>(&self, writer: &'c mut BinaryWriter<'c>) -> BxesWriteContext<'a, 'c> {
        BxesWriteContext {
            values_indices: self.values_indices.clone(),
            kv_indices: self.kv_indices.clone(),
            writer: Some(writer),
        }
    }
}