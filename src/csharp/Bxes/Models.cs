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

public interface IEventAttributes : IDictionary<string, BxesValue>
{
}

public abstract class BxesValue
{
}

public abstract class BxesValue<TValue> : BxesValue
{
  public TValue Value { get; }

  protected BxesValue(TValue value)
  {
    Value = value;
  }
}

public class BxesInt32Value : BxesValue<int>
{
  public BxesInt32Value(int value) : base(value)
  {
  }
}

public class BxesInt64Value : BxesValue<long>
{
  public BxesInt64Value(long value) : base(value)
  {
  }
}

public class BXesUint32Value : BxesValue<uint>
{
  public BXesUint32Value(uint value) : base(value)
  {
  }
}

public class BXesUint64Value : BxesValue<ulong>
{
  public BXesUint64Value(ulong value) : base(value)
  {
  }
}

public class BXesFloat32Value : BxesValue<float>
{
  public BXesFloat32Value(float value) : base(value)
  {
  }
}

public class BXesFloat64Value : BxesValue<double>
{
  public BXesFloat64Value(double value) : base(value)
  {
  }
}

public class BXesBoolValue : BxesValue<bool>
{
  public BXesBoolValue(bool value) : base(value)
  {
  }
}

public class BXesStringValue : BxesValue<string>
{
  public BXesStringValue(string value) : base(value)
  {
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