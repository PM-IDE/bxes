use variant_count::VariantCount;

#[derive(FromPrimitive, ToPrimitive, VariantCount)]
pub enum TypeIds {
    I32 = 0,
    I64 = 1,
    U32 = 2,
    U64 = 3,
    F32 = 4,
    F64 = 5,
    String = 6,
    Bool = 7,
    Timestamp = 8,
    BrafLifecycle = 9,
    StandardLifecycle = 10,
    Artifact = 11,
    Drivers = 12,
    Guid = 13,
    SoftwareEventType = 14,
}
