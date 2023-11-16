using Bxes.Writer;
using Bxes.Writer.Stream;

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

public static class EventLogUtils
{
  public static IEnumerable<BxesStreamEvent> ToEventsStream(this IEventLog log)
  {
    foreach (var pair in log.Metadata)
    {
      yield return new BxesLogMetadataKeyValueEvent(pair);
    }

    foreach (var variant in log.Traces)
    {
      yield return new BxesTraceVariantStartEvent(variant.Count);

      foreach (var @event in variant.Events)
      {
        yield return new BxesEventEvent<IEvent>(@event);
      }
    }
  }
}