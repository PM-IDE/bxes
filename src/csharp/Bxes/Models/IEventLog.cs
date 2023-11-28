using Bxes.Models.Values;
using Bxes.Utils;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Models;

public interface IEventLog : IEquatable<IEventLog>
{
  uint Version { get; }

  IEventLogMetadata Metadata { get; }
  IList<ITraceVariant> Traces { get; }
}

public interface IEventLogMetadata : IEquatable<IEventLogMetadata>
{
  IList<AttributeKeyValue> Metadata { get; }
  IList<BxesExtension> Extensions { get; }
  IList<BxesClassifier> Classifiers { get; }
  IList<AttributeKeyValue> Properties { get; }
  IList<(GlobalsEntityKind Kind, List<AttributeKeyValue> Globals)> Globals { get; }

  IEnumerable<BxesValue> EnumerateValues()
  {
    foreach (var (key, value) in Metadata.Concat(Properties))
    {
      yield return key;
      yield return value;
    }

    foreach (var extension in Extensions)
    {
      yield return extension.Name;
      yield return extension.Prefix;
      yield return extension.Uri;
    }

    foreach (var classifier in Classifiers)
    {
      yield return classifier.Name;

      foreach (var key in classifier.Keys)
      {
        yield return key;
      }
    }

    foreach (var (_, globals) in Globals)
    {
      foreach (var global in globals)
      {
        yield return global.Key;
        yield return global.Value;
      }
    }
  }

  IEnumerable<AttributeKeyValue> EnumerateKeyValuePairs()
  {
    foreach (var pair in Metadata.Concat(Properties))
    {
      yield return pair;
    }

    foreach (var (_, globals) in Globals)
    {
      foreach (var global in globals)
      {
        yield return global;
      }
    }
  }
}

public enum GlobalsEntityKind : byte
{
  Event = 0,
  Trace = 1,
  Log = 2
}

public class EventLogMetadata : IEventLogMetadata
{
  public IList<AttributeKeyValue> Metadata { get; } = new List<AttributeKeyValue>();
  public IList<BxesExtension> Extensions { get; } = new List<BxesExtension>();
  public IList<BxesClassifier> Classifiers { get; } = new List<BxesClassifier>();
  public IList<AttributeKeyValue> Properties { get; } = new List<AttributeKeyValue>();
  public IList<(GlobalsEntityKind Kind, List<AttributeKeyValue> Globals)> Globals { get; } = 
    new List<(GlobalsEntityKind Kind, List<AttributeKeyValue> Globals)>();


  public bool Equals(IEventLogMetadata? other)
  {
    if (ReferenceEquals(other, this)) return true;

    if (other is null ||
        other.Metadata.Count != Metadata.Count ||
        other.Extensions.Count != Extensions.Count ||
        other.Classifiers.Count != Classifiers.Count ||
        other.Properties.Count != Properties.Count ||
        other.Globals.Count != Globals.Count)
    {
      return false;
    }

    if (!Metadata.Zip(other.Metadata).All(pair => pair.First.Equals(pair.Second))) return false;
    if (!Extensions.Zip(other.Extensions).All(pair => pair.First.Equals(pair.Second))) return false;
    if (!Classifiers.Zip(other.Classifiers).All(pair => pair.First.Equals(pair.Second))) return false;
    if (!Properties.Zip(other.Properties).All(pair => pair.First.Equals(pair.Second))) return false;

    return Globals.Zip(other.Globals).All(pair =>
    {
      return pair.First.Kind == pair.Second.Kind &&
             pair.First.Globals.Count == pair.Second.Globals.Count &&
             pair.First.Globals.Zip(pair.Second.Globals).All(pair => pair.First.Equals(pair.Second));
    });
  }

  public override bool Equals(object? obj) => obj is EventLogMetadata other && Equals(other);

  public override int GetHashCode()
  {
    return HashCode.Combine(
      Metadata.CalculateHashCode(),
      Extensions.CalculateHashCode(),
      Classifiers.CalculateHashCode(),
      Properties.CalculateHashCode(),
      Globals.CalculateHashCode()
    );
  }
}

public record BxesClassifier
{
  public List<BxesStringValue> Keys { get; } = new();
  public required BxesStringValue Name { get; init; }
}

public record BxesExtension
{
  public required BxesStringValue Prefix { get; init; }
  public required BxesStringValue Uri { get; init; }
  public required BxesStringValue Name { get; init; }
}

public class InMemoryEventLog(uint version, IEventLogMetadata metadata, List<ITraceVariant> traces) : IEventLog
{
  public uint Version { get; } = version;

  public IEventLogMetadata Metadata { get; } = metadata;
  public IList<ITraceVariant> Traces { get; } = traces;


  public bool Equals(IEventLog? other)
  {
    return other is { } &&
           Version == other.Version &&
           Metadata.Equals(other.Metadata) &&
           Traces.Count == other.Traces.Count &&
           Traces.Zip(other.Traces).All(pair => pair.First.Equals(pair.Second));
  }
}

public static class EventLogUtil
{
  public static IEnumerable<BxesStreamEvent> ToEventsStream(this IEventLog log)
  {
    yield return new BxesLogMetadataEvent(log.Metadata);

    foreach (var variant in log.Traces)
    {
      yield return new BxesTraceVariantStartEvent(variant.Count, variant.Metadata);

      foreach (var @event in variant.Events)
      {
        yield return new BxesEventEvent<IEvent>(@event);
      }
    }
  }

  public static bool Equals(ICollection<AttributeKeyValue> first, ICollection<AttributeKeyValue> second)
  {
    return first.Count == second.Count &&
           first.Zip(second).All(pair =>
             pair.First.Key.Equals(pair.Second.Key) && pair.First.Value.Equals(pair.Second.Value));
  }
}