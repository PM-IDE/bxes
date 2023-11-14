namespace Bxes.Models;

public interface ITraceVariant : IEquatable<ITraceVariant>
{
  uint Count { get; }
  IEnumerable<IEvent> Events { get; }
}

public class TraceVariantImpl(uint count, List<InMemoryEventImpl> events) : ITraceVariant
{
  public uint Count { get; } = count;
  public IEnumerable<IEvent> Events { get; } = events;
  

  public bool Equals(ITraceVariant? other)
  {
    return other is { } &&
           Count == other.Count &&
           Events.Count() == other.Events.Count() &&
           Events.Zip(other.Events).All(pair => pair.First.Equals(pair.Second));
  }
}