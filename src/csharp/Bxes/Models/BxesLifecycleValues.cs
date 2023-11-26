using Bxes.Writer;

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
  public static IEventLifecycle Parse(string value)
  {
    if (Enum.TryParse<StandardLifecycleValues>(value, out var standardLifecycle))
    {
      return new StandardXesLifecycle(standardLifecycle);
    }

    if (Enum.TryParse<BrafLifecycleValues>(value, out var brafLifecycleValues))
    {
      return new BrafLifecycle(brafLifecycleValues);
    }

    return new BrafLifecycle(BrafLifecycleValues.Unspecified);
  }
  
  void WriteTo(BxesWriteContext context);
}

public abstract class EventLifecycle<TLifecycleValue>(TLifecycleValue value)
  : BxesValue<TLifecycleValue>(value), IEventLifecycle;

public class StandardXesLifecycle(StandardLifecycleValues value) : EventLifecycle<StandardLifecycleValues>(value)
{
  public static StandardXesLifecycle Parse(byte value) => Enum.IsDefined(typeof(StandardLifecycleValues), value) switch
  {
    true => new StandardXesLifecycle((StandardLifecycleValues)value),
    false => throw new IndexOutOfRangeException()
  };

  public override TypeIds TypeId => TypeIds.StandardLifecycle;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write((byte)Value);
  }
}

public class BrafLifecycle(BrafLifecycleValues value) : EventLifecycle<BrafLifecycleValues>(value)
{
  public static BrafLifecycle Parse(byte value) => Enum.IsDefined(typeof(BrafLifecycleValues), value) switch
  {
    true => new BrafLifecycle((BrafLifecycleValues)value),
    false => throw new IndexOutOfRangeException()
  };

  public override TypeIds TypeId => TypeIds.BrafLifecycle;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write((byte)Value);
  }
}