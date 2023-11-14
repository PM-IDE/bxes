using Bxes.Models;

namespace Bxes.Writer;

using IndexType = uint;

internal static class BxesWriteUtils
{
  private static void WriteCollectionAndCount<TElement>(
    IEnumerable<TElement> collection,
    BxesWriteContext context,
    Func<TElement, BxesWriteContext, IndexType> elementWriter)
  {
    var countPos = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    IndexType count = 0;
    foreach (var element in collection)
    {
      count += elementWriter.Invoke(element, context);
    }

    WriteCount(context.Writer, countPos, count);
  }

  public static void WriteCount(BinaryWriter writer, long countPos, IndexType count)
  {
    var currentPosition = writer.BaseStream.Position;

    writer.BaseStream.Seek(countPos, SeekOrigin.Begin);
    writer.Write(count);

    writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
  }

  public static void WriteBxesVersion(BinaryWriter writer, IndexType version) => writer.Write(version);

  public static IndexType WriteEventValues(IEvent @event, BxesWriteContext context)
  {
    IndexType writtenCount = 0;
    foreach (var value in EnumerateEventValues(@event))
    {
      writtenCount += WriteValue(value, context);
    }
    
    return writtenCount;
  }
  
  private static IndexType WriteValue(BxesValue value, BxesWriteContext context)
  {
    if (WriteValueIfNeeded(value, context)) return 1;

    return 0;
  }

  private static IEnumerable<BxesValue> EnumerateEventValues(IEvent @event)
  {
    yield return new BXesStringValue(@event.Name);

    foreach (var (key, value) in @event.Attributes)
    {
      yield return key;
      yield return value;
    }
  }

  public static bool WriteValueIfNeeded(BxesValue value, BxesWriteContext context)
  {
    if (!context.ValuesIndices.TryAdd(value, (IndexType)context.ValuesIndices.Count)) return false;

    value.WriteTo(context.Writer);
    return true;
  }

  public static void WriteKeyValuePairs(IEventLog log, BxesWriteContext context)
  {
    var pairs = log.Traces
      .SelectMany(variant => variant.Events.SelectMany(EnumerateEventKeyValuePairs))
      .Concat(log.Metadata);
    
    WriteCollectionAndCount(pairs, context, WriteKeyValuePair);
  }

  public static IndexType WriteEventKeyValuePairs(IEvent @event, BxesWriteContext context)
  {
    IndexType writtenCount = 0;
    foreach (var pair in @event.Attributes)
    {
      if (WriteKeyValuePairIfNeeded(pair, context))
      {
        ++writtenCount;
      }
    }

    return writtenCount;
  }

  private static IEnumerable<KeyValuePair<BXesStringValue, BxesValue>> EnumerateEventKeyValuePairs(IEvent @event)
  {
    return @event.Attributes;
  }

  private static IndexType WriteKeyValuePair(
    KeyValuePair<BXesStringValue, BxesValue> pair, BxesWriteContext context)
  {
    return WriteKeyValuePairIfNeeded(pair, context) switch
    {
      true => 1,
      false => 0
    };
  }

  public static bool WriteKeyValuePairIfNeeded(
    KeyValuePair<BXesStringValue, BxesValue> pair, BxesWriteContext context)
  {
    if (!context.KeyValueIndices.TryAdd(pair, (IndexType)context.KeyValueIndices.Count)) return false;

    context.Writer.Write(context.ValuesIndices[pair.Key]);
    context.Writer.Write(context.ValuesIndices[pair.Value]);

    return true;
  }

  public static void WriteEventLogMetadata(IEventLog log, BxesWriteContext context)
  {
    context.Writer.Write((IndexType)log.Metadata.Count);

    foreach (var tuple in log.Metadata)
    {
      WriteKeyValueIndex(tuple, context);
    }
  }

  public static void WriteKeyValueIndex(KeyValuePair<BXesStringValue, BxesValue> tuple, BxesWriteContext context)
  {
    context.Writer.Write(context.KeyValueIndices[tuple]);
  }

  public static void WriteTracesVariants(IEventLog log, BxesWriteContext context) =>
    WriteCollectionAndCount(log.Traces, context, WriteTraceVariant);

  private static IndexType WriteTraceVariant(ITraceVariant variant, BxesWriteContext context)
  {
    context.Writer.Write(variant.Count);
    WriteCollectionAndCount(variant.Events, context, WriteEvent);
    return 1;
  }

  public static IndexType WriteEvent(IEvent @event, BxesWriteContext context)
  {
    context.Writer.Write(context.ValuesIndices[new BXesStringValue(@event.Name)]);
    context.Writer.Write(@event.Timestamp);
    @event.Lifecycle.WriteTo(context.Writer);

    context.Writer.Write((IndexType)@event.Attributes.Count);

    foreach (var pair in @event.Attributes)
    {
      context.Writer.Write(context.KeyValueIndices[pair]);
    }

    return 1;
  }

  public static void WriteValues(IEventLog log, BxesWriteContext context)
  {
    var values = log.Traces
      .SelectMany(variant => variant.Events.SelectMany(EnumerateEventValues))
      .Concat(EnumerateMetadataValues(log.Metadata));
    
    WriteCollectionAndCount(values, context, WriteValue);
  }

  private static IEnumerable<BxesValue> EnumerateMetadataValues(IEventLogMetadata metadata)
  {
    foreach (var (key, value) in metadata)
    {
      yield return key;
      yield return value;
    }
  }

  public static async Task ExecuteWithFile(string filePath, Action<BinaryWriter> writeAction)
  {
    await using var fs = File.OpenWrite(filePath);
    await using var bw = new BinaryWriter(fs, BxesConstants.BxesEncoding);

    writeAction(bw);
  }
}