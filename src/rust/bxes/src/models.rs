use num_derive::FromPrimitive;
use num_traits::ToBytes;
use std::hash::Hash;
use std::rc::Rc;
use variant_count::VariantCount;

#[derive(Clone, Debug)]
pub enum BxesValue {
    Int32(i32),
    Int64(i64),
    Uint32(u32),
    Uint64(u64),
    Float32(f32),
    Float64(f64),
    String(Rc<Box<String>>),
    Bool(bool),
    Timestamp(i64),
    BrafLifecycle(BrafLifecycle),
    StandardLifecycle(StandardLifecycle),
}

impl Hash for BxesValue {
    fn hash<H: std::hash::Hasher>(&self, state: &mut H) {
        match self {
            BxesValue::Int32(value) => state.write_i32(*value),
            BxesValue::Int64(value) => state.write_i64(*value),
            BxesValue::Uint32(value) => state.write_u32(*value),
            BxesValue::Uint64(value) => state.write_u64(*value),
            BxesValue::Float32(value) => state.write(value.to_le_bytes().as_slice()),
            BxesValue::Float64(value) => state.write(value.to_le_bytes().as_slice()),
            BxesValue::String(value) => state.write(value.as_bytes()),
            BxesValue::Bool(value) => state.write(if *value { &[1] } else { &[0] }),
            BxesValue::Timestamp(value) => state.write_i64(*value),
            BxesValue::BrafLifecycle(value) => value.hash(state),
            BxesValue::StandardLifecycle(value) => value.hash(state),
        }
    }
}

impl PartialEq for BxesValue {
    fn eq(&self, other: &Self) -> bool {
        match (self, other) {
            (Self::Int32(left), Self::Int32(right)) => left == right,
            (Self::Int64(left), Self::Int64(right)) => left == right,
            (Self::Uint32(left), Self::Uint32(right)) => left == right,
            (Self::Uint64(left), Self::Uint64(right)) => left == right,
            (Self::Float32(left), Self::Float32(right)) => left == right,
            (Self::Float64(left), Self::Float64(right)) => left == right,
            (Self::String(left), Self::String(right)) => left == right,
            (Self::Bool(left), Self::Bool(right)) => left == right,
            (Self::Timestamp(left), Self::Timestamp(right)) => left == right,
            (Self::BrafLifecycle(left), Self::BrafLifecycle(right)) => left == right,
            (Self::StandardLifecycle(left), Self::StandardLifecycle(right)) => left == right,
            _ => false,
        }
    }
}

impl Eq for BxesValue {}

#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Lifecycle {
    Braf(BrafLifecycle),
    Standard(StandardLifecycle),
}

#[derive(FromPrimitive, ToPrimitive, Clone, Debug, PartialEq, Eq, Hash, VariantCount)]
pub enum BrafLifecycle {
    Unspecified = 0,
    Closed = 1,
    ClosedCancelled = 2,
    ClosedCancelledAborted = 3,
    ClosedCancelledError = 4,
    ClosedCancelledExited = 5,
    ClosedCancelledObsolete = 6,
    ClosedCancelledTerminated = 7,
    Completed = 8,
    CompletedFailed = 9,
    CompletedSuccess = 10,
    Open = 11,
    OpenNotRunning = 12,
    OpenNotRunningAssigned = 13,
    OpenNotRunningReserved = 14,
    OpenNotRunningSuspendedAssigned = 15,
    OpenNotRunningSuspendedReserved = 16,
    OpenRunning = 17,
    OpenRunningInProgress = 18,
    OpenRunningSuspended = 19,
}

#[derive(FromPrimitive, ToPrimitive, Clone, Debug, PartialEq, Eq, Hash, VariantCount)]
pub enum StandardLifecycle {
    Unspecified = 0,
    Assign = 1,
    AteAbort = 2,
    Autoskip = 3,
    Complete = 4,
    ManualSkip = 5,
    PiAbort = 6,
    ReAssign = 7,
    Resume = 8,
    Schedule = 9,
    Start = 10,
    Suspend = 11,
    Unknown = 12,
    Withdraw = 13,
}

#[derive(Debug)]
pub struct BxesEventLog {
    pub version: u32,
    pub metadata: Option<Vec<(BxesValue, BxesValue)>>,
    pub variants: Vec<BxesTraceVariant>,
}

#[derive(Debug)]
pub struct BxesTraceVariant {
    pub traces_count: u32,
    pub events: Vec<BxesEvent>,
}

#[derive(Debug)]
pub struct BxesEvent {
    pub name: BxesValue,
    pub timestamp: i64,
    pub lifecycle: Lifecycle,
    pub attributes: Option<Vec<(BxesValue, BxesValue)>>,
}

impl PartialEq for BxesEvent {
    fn eq(&self, other: &Self) -> bool {
        if !self.compare_events_by_properties(other) {
            return false;
        }

        compare_list_of_attributes(&self.attributes, &other.attributes)
    }
}

impl BxesEvent {
    fn compare_events_by_properties(&self, other: &Self) -> bool {
        self.name == other.name
            && self.timestamp == other.timestamp
            && self.lifecycle == other.lifecycle
    }
}

fn compare_list_of_attributes(
    first_attributes: &Option<Vec<(BxesValue, BxesValue)>>,
    second_attributes: &Option<Vec<(BxesValue, BxesValue)>>,
) -> bool {
    if first_attributes.is_none() && second_attributes.is_none() {
        return true;
    }

    if let Some(self_attributes) = first_attributes.as_ref() {
        if let Some(other_attributes) = second_attributes.as_ref() {
            if self_attributes.len() != other_attributes.len() {
                return false;
            }

            for (self_attribute, other_attribute) in self_attributes.iter().zip(other_attributes) {
                if !(attributes_equals(self_attribute, other_attribute)) {
                    return false;
                }
            }

            return true;
        }
    }

    return false;
}

fn attributes_equals(
    first_attribute: &(BxesValue, BxesValue),
    second_attribute: &(BxesValue, BxesValue),
) -> bool {
    first_attribute.0.eq(&second_attribute.0) && first_attribute.1.eq(&second_attribute.1)
}

impl PartialEq for BxesTraceVariant {
    fn eq(&self, other: &Self) -> bool {
        if self.traces_count != other.traces_count {
            return false;
        }

        if self.events.len() != other.events.len() {
            return false;
        }

        for (self_event, other_event) in self.events.iter().zip(&other.events) {
            if !self_event.eq(&other_event) {
                return false;
            }
        }

        return true;
    }
}

impl PartialEq for BxesEventLog {
    fn eq(&self, other: &Self) -> bool {
        if self.version != other.version {
            return false;
        }

        if !compare_list_of_attributes(&self.metadata, &other.metadata) {
            return false;
        }

        for (self_variant, other_variant) in self.variants.iter().zip(&other.variants) {
            if !self_variant.eq(&other_variant) {
                return false;
            }
        }

        return true;
    }
}
