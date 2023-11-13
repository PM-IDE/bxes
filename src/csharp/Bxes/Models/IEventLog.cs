namespace Bxes.Models;

public interface IEventLog
{
  uint Version { get; }

  IEventLogMetadata Metadata { get; }
  IEnumerable<ITraceVariant> Traces { get; }
}

public interface IEventLogMetadata : IEventAttributes;

public class EventLogMetadataImpl : EventAttributesImpl, IEventLogMetadata;

public class InMemoryEventLog(uint version, IEventLogMetadata metadata, List<ITraceVariant> traces) : IEventLog
{
  public uint Version { get; } = version;

  public IEventLogMetadata Metadata { get; } = metadata;
  public IEnumerable<ITraceVariant> Traces { get; } = traces;
}