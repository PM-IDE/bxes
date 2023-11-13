namespace Bxes.Models;

public interface ITraceVariant
{
  uint Count { get; }
  IEnumerable<IEvent> Events { get; }
}

public class TraceVariantImpl(uint count, List<EventImpl> events) : ITraceVariant
{
  public uint Count { get; } = count;
  public IEnumerable<IEvent> Events { get; } = events;
}