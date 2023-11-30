using Bxes.Models;
using Bxes.Models.Values.Lifecycle;
using Bxes.Writer;

namespace Bxes.Xes;

public readonly struct FromXesBxesEvent : IEvent
{
  public required long Timestamp { get; init; }
  public required string Name { get; init; }
  public required IEventLifecycle Lifecycle { get; init; }
  public required IList<AttributeKeyValue> Attributes { get; init; }
  

  public bool Equals(IEvent? other) => other is { } && EventUtil.Equals(this, other);
}