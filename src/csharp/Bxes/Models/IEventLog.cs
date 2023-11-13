namespace Bxes.Models;

public interface IEventLog
{
  IEventLogMetadata Metadata { get; }
  IEnumerable<ITraceVariant> Traces { get; }
}

public interface IEventLogMetadata : IEventAttributes;

public class EventLogMetadataImpl : EventAttributesImpl, IEventLogMetadata;

public class InMemoryEventLog(IEventLogMetadata metadata, List<ITraceVariant> traces) : IEventLog
{
  public IEventLogMetadata Metadata { get; } = metadata;
  public IEnumerable<ITraceVariant> Traces { get; } = traces;
}