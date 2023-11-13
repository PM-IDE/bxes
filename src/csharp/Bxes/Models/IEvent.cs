namespace Bxes.Models;

public interface IEvent
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle Lifecycle { get; }

  IEventAttributes Attributes { get; }
}

public class EventImpl(
  long timestamp,
  BXesStringValue name,
  IEventLifecycle lifecycle,
  IEventAttributes attributes
) : IEvent
{
  public long Timestamp { get; } = timestamp;
  public IEventLifecycle Lifecycle { get; } = lifecycle;
  public string Name => name.Value;
  public IEventAttributes Attributes { get; } = attributes;
}

public interface IEventAttributes : IDictionary<BXesStringValue, BxesValue>;

public class EventAttributesImpl : Dictionary<BXesStringValue, BxesValue>, IEventAttributes;