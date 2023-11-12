namespace Bxes.Models;

public interface IEventLog
{
  IEventLogMetadata Metadata { get; }
  IEnumerable<ITraceVariant> Traces { get; }
}

public interface IEventLogMetadata : IEventAttributes;