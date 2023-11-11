namespace Bxes;

public interface IEventLog
{
  IEventLogMetadata Metadata { get; }
  IEnumerable<ITrace> Traces { get; }
}

public interface IEventLogMetadata : IEventAttributes
{
}

public interface ITrace
{
  IEnumerable<IEvent> Events { get; }
}

public interface IEvent
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle? Lifecycle { get; }

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

public abstract class BxesValue<TValue> : BxesValue
{
  public TValue Value { get; }

  protected BxesValue(TValue value)
  {
    Value = value;
  }


  public override void WriteTo(BinaryWriter bw)
  {
    bw.Write(TypeId);
  }
}

public class BxesInt32Value : BxesValue<int>
{
  public override byte TypeId => TypeIds.I32;
  

  public BxesInt32Value(int value) : base(value)
  {
  }


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesInt64Value : BxesValue<long>
{
  public override byte TypeId => TypeIds.I64;
  
  
  public BxesInt64Value(long value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesUint32Value : BxesValue<uint>
{
  public override byte TypeId => TypeIds.U32;
  
  
  public BXesUint32Value(uint value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesUint64Value : BxesValue<ulong>
{
  public override byte TypeId => TypeIds.U64;


  public BXesUint64Value(ulong value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesFloat32Value : BxesValue<float>
{
  public override byte TypeId => TypeIds.F32;


  public BXesFloat32Value(float value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesFloat64Value : BxesValue<double>
{
  public override byte TypeId => TypeIds.F64;
  
  
  public BXesFloat64Value(double value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesBoolValue : BxesValue<bool>
{
  public override byte TypeId => TypeIds.Bool;
  
  
  public BXesBoolValue(bool value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BXesStringValue : BxesValue<string>
{
  public override byte TypeId => TypeIds.String;
  
  
  public BXesStringValue(string value) : base(value)
  {
  }
  
  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
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
}

public abstract class EventLifecycle<TLifecycleValue> : BxesValue<TLifecycleValue>, IEventLifecycle
{
  protected EventLifecycle(TLifecycleValue value) : base(value)
  {
  }
}

public class StandardXesLifecycle : EventLifecycle<StandardLifecycleValues>
{
  public override byte TypeId => TypeIds.StandardLifecycle;


  public StandardXesLifecycle(StandardLifecycleValues value) : base(value)
  {
  }


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write((byte)Value);
  }
}

public class BrafLifecycle : EventLifecycle<BrafLifecycleValues>
{
  public override byte TypeId => TypeIds.BrafLifecycle;


  public BrafLifecycle(BrafLifecycleValues value) : base(value)
  {
  }


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write((byte)Value);
  }
}