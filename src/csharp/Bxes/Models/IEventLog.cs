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
  IList<BxesExtension> Extensions { get; }
  IList<BxesClassifier> Classifiers { get; }
  IList<AttributeKeyValue> Properties { get; }
  IList<BxesGlobal> Globals { get; }

  IEnumerable<BxesValue> EnumerateValues()
  {
    foreach (var (key, value) in Properties)
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

    foreach (var global in Globals)
    {
      foreach (var attribute in global.Globals)
      {
        yield return attribute.Key;
        yield return attribute.Value;
      }
    }
  }

  IEnumerable<AttributeKeyValue> EnumerateKeyValuePairs()
  {
    foreach (var pair in Properties)
    {
      yield return pair;
    }

    foreach (var global in Globals)
    {
      foreach (var attribute in global.Globals)
      {
        yield return attribute;
      }
    }
  }

  IEnumerable<BxesStreamEvent> ToEventsStream()
  {
    foreach (var extension in Extensions)
      yield return new BxesLogMetadataExtensionEvent(extension);

    foreach (var classifier in Classifiers)
      yield return new BxesLogMetadataClassifierEvent(classifier);

    foreach (var global in Globals)
      yield return new BxesLogMetadataGlobalEvent(global);

    foreach (var property in Properties)
      yield return new BxesLogMetadataPropertyEvent(property);
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
  public IList<BxesExtension> Extensions { get; } = new List<BxesExtension>();
  public IList<BxesClassifier> Classifiers { get; } = new List<BxesClassifier>();
  public IList<AttributeKeyValue> Properties { get; } = new List<AttributeKeyValue>();
  public IList<BxesGlobal> Globals { get; } = new List<BxesGlobal>();


  public bool Equals(IEventLogMetadata? other)
  {
    if (ReferenceEquals(other, this)) return true;

    if (other is null ||
        other.Extensions.Count != Extensions.Count ||
        other.Classifiers.Count != Classifiers.Count ||
        other.Properties.Count != Properties.Count ||
        other.Globals.Count != Globals.Count)
    {
      return false;
    }

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
      Extensions.CalculateHashCode(),
      Classifiers.CalculateHashCode(),
      Properties.CalculateHashCode(),
      Globals.CalculateHashCode()
    );
  }
}

public record BxesClassifier
{
  public required List<BxesStringValue> Keys { get; init; }
  public required BxesStringValue Name { get; init; }
}

public record BxesExtension
{
  public required BxesStringValue Prefix { get; init; }
  public required BxesStringValue Uri { get; init; }
  public required BxesStringValue Name { get; init; }
}

public record BxesGlobal
{
  public required GlobalsEntityKind Kind { get; init; }
  public required List<AttributeKeyValue> Globals { get; init; }
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
    foreach (var @event in log.Metadata.ToEventsStream())
    {
      yield return @event;
    }

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