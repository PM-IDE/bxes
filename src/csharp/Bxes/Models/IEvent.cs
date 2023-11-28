using Bxes.Models.Values;
using Bxes.Models.Values.Lifecycle;
using Bxes.Writer;

namespace Bxes.Models;

public interface IEvent : IEquatable<IEvent>
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle Lifecycle { get; }

  IEnumerable<AttributeKeyValue> Attributes { get; }

  IEnumerable<BxesValue> EnumerateValues()
  {
    yield return new BxesStringValue(Name);

    foreach (var (key, value) in Attributes)
    {
      yield return key;
      yield return value;
    }
  }

  IEnumerable<AttributeKeyValue> EnumerateKeyValuePairs() => Attributes;
}

public static class EventUtil
{
  public static bool Equals(IEvent first, IEvent second)
  {
    return first.Timestamp == second.Timestamp &&
           first.Name == second.Name &&
           first.Lifecycle.Equals(second.Lifecycle) &&
           EventLogUtil.Equals(first.Attributes.ToList(), second.Attributes.ToList());
  }
}

public class InMemoryEventImpl(
  long timestamp,
  BxesStringValue name,
  IEventLifecycle lifecycle,
  IEnumerable<AttributeKeyValue> attributes
) : IEvent
{
  public long Timestamp { get; } = timestamp;
  public IEventLifecycle Lifecycle { get; } = lifecycle;
  public string Name => name.Value;
  public IEnumerable<AttributeKeyValue> Attributes { get; } = attributes;


  public bool Equals(IEvent? other) => other is { } && EventUtil.Equals(this, other);
}