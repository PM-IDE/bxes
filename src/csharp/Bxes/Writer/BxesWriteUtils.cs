using Bxes.Models;

namespace Bxes.Writer;

using IndexType = uint;

internal static class BxesWriteUtils
{
  private static void WriteCollectionAndCount<TElement>(
    IEnumerable<TElement> collection, BxesWriteContext context, Action<TElement, BxesWriteContext> elementWriter)
  {
    var countPos = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    IndexType count = 0;
    foreach (var element in collection)
    {
      elementWriter.Invoke(element, context);
      ++count;
    }

    WriteCount(context.Writer, countPos, count);
  }

  private static void WriteCount(BinaryWriter writer, long countPos, IndexType count)
  {
    var currentPosition = writer.BaseStream.Position;

    writer.BaseStream.Seek(countPos, SeekOrigin.Begin);
    writer.Write(count);

    writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
  }

  public static void WriteBxesVersion(BinaryWriter writer) => writer.Write(BxesConstants.BxesVersion);
  
  private static void WriteEventValues(IEvent @event, BxesWriteContext context, ref IndexType count)
  {
    var nameValue = new BXesStringValue(@event.Name);
    if (!context.ValuesIndices.ContainsKey(nameValue))
    {
      context.ValuesIndices[nameValue] = context.Writer.BaseStream.Position;
      context.Writer.Write(@event.Name);
      ++count;
    }

    foreach (var (key, value) in @event.Attributes)
    {
      if (!context.ValuesIndices.ContainsKey(key))
      {
        context.ValuesIndices[key] = context.Writer.BaseStream.Position;
        context.Writer.Write(key.Value);
        ++count;
      }

      if (!context.ValuesIndices.ContainsKey(value))
      {
        context.ValuesIndices[value] = context.Writer.BaseStream.Position;
        value.WriteTo(context.Writer);
        ++count;
      }
    }
  }
  
  public static void WriteKeyValuePairs(IEventLog log, BxesWriteContext context)
  {
    var keyValueCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    IndexType count = 0;
    foreach (var trace in log.Traces)
    {
      foreach (var @event in trace.Events)
      {
        WriteEventKeyValuePair(@event, context, ref count);
      }
    }

    WriteCount(context.Writer, keyValueCountPosition, count);
  }
  
  private static void WriteEventKeyValuePair(IEvent @event, BxesWriteContext context, ref IndexType count)
  {
    foreach (var (key, value) in @event.Attributes)
    {
      var tuple = (key, value);
      if (!context.KeyValueIndices.ContainsKey(tuple))
      {
        context.KeyValueIndices[tuple] = context.Writer.BaseStream.Position;
        context.Writer.Write(context.ValuesIndices[key]);
        context.Writer.Write(context.ValuesIndices[value]);
        ++count;
      }
    }
  }
  
  public static void WriteEventLogMetadata(IEventLog log, BxesWriteContext context)
  {
    context.Writer.Write((IndexType)log.Metadata.Count);

    foreach (var (key, value) in log.Metadata)
    {
      context.Writer.Write(context.KeyValueIndices[(key, value)]);
    }
  }
  
  public static void WriteTracesVariants(IEventLog log, BxesWriteContext context) =>
    WriteCollectionAndCount(log.Traces, context, WriteTraceVariant);

  private static void WriteTraceVariant(ITraceVariant variant, BxesWriteContext context)
  {
    context.Writer.Write(variant.Count);
    WriteCollectionAndCount(variant.Events, context, WriteEvent);
  }

  private static void WriteEvent(IEvent @event, BxesWriteContext context)
  {
    context.Writer.Write(context.ValuesIndices[new BXesStringValue(@event.Name)]);
    context.Writer.Write(@event.Timestamp);
    @event.Lifecycle.WriteTo(context.Writer);

    context.Writer.Write((IndexType)@event.Attributes.Count);

    foreach (var (key, value) in @event.Attributes)
    {
      context.Writer.Write(context.KeyValueIndices[(key, value)]);
    }
  }
  
  public static void WriteValues(IEventLog log, BxesWriteContext context)
  {
    var valuesCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    IndexType count = 0;
    foreach (var trace in log.Traces)
    {
      foreach (var @event in trace.Events)
      {
        WriteEventValues(@event, context, ref count);
      }
    }

    WriteCount(context.Writer, valuesCountPosition, count);
  }
  
  public static async Task ExecuteWithFile(string filePath, Action<BinaryWriter> writeAction)
  {
    await using var fs = File.OpenWrite(filePath);
    await using var bw = new BinaryWriter(fs, BxesConstants.BxesEncoding);

    writeAction(bw);
  }
}