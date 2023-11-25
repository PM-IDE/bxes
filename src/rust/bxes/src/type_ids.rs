pub const I_32: u8 = 0;
pub const I_64: u8 = 1;
pub const U_32: u8 = 2;
pub const U_64: u8 = 3;
pub const F_32: u8 = 4;
pub const F_64: u8 = 5;
pub const STRING: u8 = 6;
pub const BOOL: u8 = 7;
pub const TIMESTAMP: u8 = 8;
pub const BRAF_LIFECYCLE: u8 = 9;
pub const STANDARD_LIFECYCLE: u8 = 10;
pub const ARTIFACT: u8 = 11;
pub const DRIVERS: u8 = 12;
pub const GUID: u8 = 13;
pub const SOFTWARE_EVENT_TYPE: u8 = 14;

pub fn types_count() -> usize {
    15
}
