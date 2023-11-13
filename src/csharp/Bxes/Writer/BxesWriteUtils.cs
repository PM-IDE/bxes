using System.Text.Json;
using Bxes.Models;

namespace Bxes.Writer;

using IndexType = uint;

internal static class BxesWriteUtils
{
  private static void WriteCollectionAndCount<TElement>(
    IEnumerable<TElement> collection, BxesWriteContext context,
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

  public static void WriteBxesVersion(BinaryWriter writer) => writer.Write(BxesConstants.BxesVersion);

  public static IndexType WriteEventValues(IEvent @event, BxesWriteContext context)
  {
    IndexType writtenCount = 0;
    var nameValue = new BXesStringValue(@event.Name);
    if (WriteValueIfNeeded(nameValue, context))
    {
      ++writtenCount;
    }

    foreach (var (key, value) in @event.Attributes)
    {
      if (WriteValueIfNeeded(key, context))
      {
        ++writtenCount;
      }

      if (WriteValueIfNeeded(value, context))
      {
        ++writtenCount;
      }
    }

    return writtenCount;
  }

  public static bool WriteValueIfNeeded(BxesValue value, BxesWriteContext context)
  {
    if (context.ValuesIndices.ContainsKey(value)) return false;

    context.ValuesIndices[value] = context.Writer.BaseStream.Position;
    value.WriteTo(context.Writer);
    return true;
  }

  public static void WriteKeyValuePairs(IEventLog log, BxesWriteContext context)
  {
    WriteCollectionAndCount(log.Traces.SelectMany(variant => variant.Events), context, WriteEventKeyValuePairs);
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

  public static bool WriteKeyValuePairIfNeeded(KeyValuePair<BXesStringValue, BxesValue> pair, BxesWriteContext context)
  {
    if (context.KeyValueIndices.ContainsKey(pair)) return false;
    
    context.KeyValueIndices[pair] = context.Writer.BaseStream.Position;

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
    WriteCollectionAndCount(log.Traces.SelectMany(variant => variant.Events), context, WriteEventValues);
  }

  public static async Task ExecuteWithFile(string filePath, Action<BinaryWriter> writeAction)
  {
    await using var fs = File.OpenWrite(filePath);
    await using var bw = new BinaryWriter(fs, BxesConstants.BxesEncoding);

    writeAction(bw);
  }
}