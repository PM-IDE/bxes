namespace Bxes.Models;

public interface ITraceVariant
{
  uint Count { get; }
  IEnumerable<IEvent> Events { get; }
}