using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Models;

public interface IEventLog : IEquatable<IEventLog>
{
  uint Version { get; }

  IEnumerable<AttributeKeyValue> Metadata { get; }
  IEnumerable<ITraceVariant> Traces { get; }
}

public class InMemoryEventLog(uint version, IEnumerable<AttributeKeyValue> metadata, List<ITraceVariant> traces) : IEventLog
{
  public uint Version { get; } = version;

  public IEnumerable<AttributeKeyValue> Metadata { get; } = metadata;
  public IEnumerable<ITraceVariant> Traces { get; } = traces;


  public bool Equals(IEventLog? other)
  {
    return other is { } &&
           Version == other.Version &&
           EventLogUtil.Equals(Metadata.ToList(), other.Metadata.ToList()) &&
           Traces.Count() == other.Traces.Count() &&
           Traces.Zip(other.Traces).All(pair => pair.First.Equals(pair.Second));
  }
}

public static class EventLogUtil
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

  public static bool Equals(ICollection<AttributeKeyValue> first, ICollection<AttributeKeyValue> second)
  {
    return first.Count == second.Count &&
           first.Zip(second).All(pair => pair.First.Key.Equals(pair.Second.Key) && pair.First.Value.Equals(pair.Second.Value));
  }
}