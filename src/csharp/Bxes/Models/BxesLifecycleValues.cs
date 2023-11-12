namespace Bxes.Models;

public enum BrafLifecycleValues : byte
{
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

public enum StandardLifecycleValues : byte
{
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

public interface IEventLifecycle
{
  void WriteTo(BinaryWriter bw);
}

public abstract class EventLifecycle<TLifecycleValue>(TLifecycleValue value) 
  : BxesValue<TLifecycleValue>(value), IEventLifecycle;

public class StandardXesLifecycle(StandardLifecycleValues value) : EventLifecycle<StandardLifecycleValues>(value)
{
  public override byte TypeId => TypeIds.StandardLifecycle;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write((byte)Value);
  }
}

public class BrafLifecycle(BrafLifecycleValues value) : EventLifecycle<BrafLifecycleValues>(value)
{
  public override byte TypeId => TypeIds.BrafLifecycle;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write((byte)Value);
  }
}