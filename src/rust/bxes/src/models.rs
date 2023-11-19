use std::rc::Rc;

use num_derive::FromPrimitive;

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

#[derive(Clone, Debug)]
pub enum Lifecycle {
    Braf(BrafLifecycle),
    Standard(StandardLifecycle),
}

#[derive(FromPrimitive, Clone, Debug)]
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

#[derive(FromPrimitive, Clone, Debug)]
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
    pub metadata: Option<Vec<(Rc<Box<String>>, BxesValue)>>,
    pub variants: Vec<BxesTraceVariant>,
}

#[derive(Debug)]
pub struct BxesTraceVariant {
    pub traces_count: u32,
    pub events: Vec<BxesEvent>,
}

#[derive(Debug)]
pub struct BxesEvent {
    pub name: Rc<Box<String>>,
    pub timestamp: i64,
    pub lifecycle: Lifecycle,
    pub attributes: Option<Vec<(Rc<Box<String>>, BxesValue)>>,
}
