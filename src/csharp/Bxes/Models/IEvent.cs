using System.Runtime.CompilerServices;

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

public static class DictionaryUtils
{
  public static bool DeepEquals<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> other)
  {
    foreach (var key in self.Keys)
    {
      if (!other.ContainsKey(key)) return false;
    }

    foreach (var key in other.Keys)
    {
      if (!self.ContainsKey(key)) return false;
    }

    foreach (var (key, value) in self)
    {
      if (!value.Equals(other[key])) return false;
    }

    return true;
  }
}