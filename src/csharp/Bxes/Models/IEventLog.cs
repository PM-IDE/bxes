namespace Bxes.Models;

public interface IEventLog : IEquatable<IEventLog>
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


  public bool Equals(IEventLog? other)
  {
    return other is { } &&
           Version == other.Version &&
           Metadata.Equals(other.Metadata) &&
           Traces.Count() == other.Traces.Count() &&
           Traces.Zip(other.Traces).All(pair => pair.First.Equals(pair.Second));
  }
}