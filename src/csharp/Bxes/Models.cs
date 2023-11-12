using System.Runtime.InteropServices;

namespace Bxes;

public interface IEventLog
{
  IEventLogMetadata Metadata { get; }
  IEnumerable<ITraceVariant> Traces { get; }
}

public interface IEventLogMetadata : IEventAttributes
{
}

public interface ITraceVariant
{
  uint Count { get; }
  IEnumerable<IEvent> Events { get; }
}

public interface IEvent
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle Lifecycle { get; }

  IEventAttributes Attributes { get; }
}

public interface IEventAttributes : IDictionary<BXesStringValue, BxesValue>
{
}

public static class TypeIds
{
  public const byte I32 = 0;
  public const byte I64 = 1;
  public const byte U32 = 2;
  public const byte U64 = 3;
  public const byte F32 = 4;
  public const byte F64 = 5;
  public const byte String = 6;
  public const byte Bool = 7;
  public const byte Timestamp = 8;
  public const byte BrafLifecycle = 9;
  public const byte StandardLifecycle = 10;
}

public abstract class BxesValue
{
  public abstract byte TypeId { get; }
  public abstract void WriteTo(BinaryWriter bw);
}

public abstract class BxesValue<TValue>(TValue value) : BxesValue
{
  public TValue Value { get; } = value;


  public override void WriteTo(BinaryWriter bw)
  {
    bw.Write(TypeId);
  }

  public override bool Equals(object? obj) => obj is BxesValue<TValue> otherValue && otherValue.Value.Equals(Value);

  public override int GetHashCode() => Value.GetHashCode();
}

public class BxesInt32Value(int value) : BxesValue<int>(value)
{
  public override byte TypeId => TypeIds.I32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesInt64Value(long value) : BxesValue<long>(value)
{
  public override byte TypeId => TypeIds.I64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesUint32Value(uint value) : BxesValue<uint>(value)
{
  public override byte TypeId => TypeIds.U32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesUint64Value(ulong value) : BxesValue<ulong>(value)
{
  public override byte TypeId => TypeIds.U64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesFloat32Value(float value) : BxesValue<float>(value)
{
  public override byte TypeId => TypeIds.F32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesFloat64Value(double value) : BxesValue<double>(value)
{
  public override byte TypeId => TypeIds.F64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesBoolValue(bool value) : BxesValue<bool>(value)
{
  public override byte TypeId => TypeIds.Bool;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesStringValue(string value) : BxesValue<string>(value)
{
  public override byte TypeId => TypeIds.String;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesTimeStampValue(long value) : BxesValue<long>(value)
{
  public override byte TypeId => TypeIds.Timestamp;

  public DateTime Timestamp { get; } = new(value, DateTimeKind.Utc);
  

  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(value);
  }
}

public enum BrafLifecycleValues : byte
{
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