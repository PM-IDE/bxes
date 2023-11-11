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
  EventLifecycle? Lifecycle { get; }

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

public abstract class EventLifecycle
{
}

public class StandardXesLifecycle : EventLifecycle
{
}

public class BrafLifecycle : EventLifecycle
{
}