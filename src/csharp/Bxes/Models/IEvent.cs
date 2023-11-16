using System.Runtime.CompilerServices;
using Bxes.Utils;

namespace Bxes.Models;

public interface IEvent : IEquatable<IEvent>
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle Lifecycle { get; }

  IEventAttributes Attributes { get; }
}

public class InMemoryEventImpl(
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


  public bool Equals(IEvent? other)
  {
    return other is { } &&
           Timestamp == other.Timestamp &&
           Lifecycle.Equals(other.Lifecycle) &&
           Name == other.Name &&
           Attributes.Equals(other.Attributes);
  }
}

public interface IEventAttributes : IDictionary<BXesStringValue, BxesValue>, IEquatable<IEventAttributes>;

public class EventAttributesImpl : Dictionary<BXesStringValue, BxesValue>, IEventAttributes
{
  public bool Equals(IEventAttributes? other) => other is { } && this.DeepEquals(other);

  public override bool Equals(object? obj) => obj is EventAttributesImpl other && Equals(other);

  public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}