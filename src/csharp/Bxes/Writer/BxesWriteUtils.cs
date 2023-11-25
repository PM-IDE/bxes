using System.IO.Compression;
using Bxes.Models;

namespace Bxes.Writer;

using IndexType = uint;

internal static class BxesWriteUtils
{
  private static void WriteCollectionAndCount<TElement>(
    IEnumerable<TElement> collection,
    BxesWriteContext context,
    Action<TElement, BxesWriteContext> elementWriter,
    Func<IndexType> countGetter)
  {
    var countPos = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    foreach (var element in collection)
    {
      elementWriter.Invoke(element, context);
    }

    WriteCount(context.Writer, countPos, countGetter());
  }

  public static void WriteCount(BinaryWriter writer, long countPos, IndexType count)
  {
    var currentPosition = writer.BaseStream.Position;

    writer.BaseStream.Seek(countPos, SeekOrigin.Begin);
    writer.Write(count);

    writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
  }

  public static void WriteBxesVersion(BinaryWriter writer, IndexType version) => writer.Write(version);

  public static void WriteEventValues(IEvent @event, BxesWriteContext context)
  {
    foreach (var value in EnumerateEventValues(@event))
    {
      WriteValueIfNeeded(value, context);
    }
  }

  private static IEnumerable<BxesValue> EnumerateEventValues(IEvent @event)
  {
    yield return new BxesStringValue(@event.Name);

    foreach (var (key, value) in @event.Attributes)
    {
      yield return key;
      yield return value;
    }
  }

  public static void WriteValueIfNeeded(BxesValue value, BxesWriteContext context)
  {
    if (context.ValuesIndices.ContainsKey(value)) return;

    value.WriteTo(context);

    context.ValuesIndices[value] = (IndexType)context.ValuesIndices.Count;
  }

  public static void WriteKeyValuePairs(IEventLog log, BxesWriteContext context)
  {
    var pairs = log.Traces
      .SelectMany(variant => variant.Events.SelectMany(EnumerateEventKeyValuePairs))
      .Concat(log.Metadata);

    WriteCollectionAndCount(pairs, context, WriteKeyValuePairIfNeeded, () => (IndexType)context.KeyValueIndices.Count);
  }

  public static void WriteEventKeyValuePairs(IEvent @event, BxesWriteContext context)
  {
    foreach (var pair in @event.Attributes)
    {
      WriteKeyValuePairIfNeeded(pair, context);
    }
  }

  private static IEnumerable<AttributeKeyValue> EnumerateEventKeyValuePairs(IEvent @event)
  {
    return @event.Attributes;
  }

  public static void WriteKeyValuePairIfNeeded(AttributeKeyValue pair, BxesWriteContext context)
  {
    if (context.KeyValueIndices.ContainsKey(pair)) return;

    context.Writer.Write(context.ValuesIndices[pair.Key]);
    context.Writer.Write(context.ValuesIndices[pair.Value]);

    context.KeyValueIndices[pair] = (IndexType)context.KeyValueIndices.Count;
  }

  public static void WriteEventLogMetadata(IEventLog log, BxesWriteContext context)
  {
    WriteCollectionAndCount(log.Metadata, context, WriteKeyValueIndex, () => (IndexType)log.Metadata.Count());
  }

  public static void WriteKeyValueIndex(AttributeKeyValue tuple, BxesWriteContext context)
  {
    context.Writer.Write(context.KeyValueIndices[tuple]);
  }

  public static void WriteTracesVariants(IEventLog log, BxesWriteContext context) =>
    WriteCollectionAndCount(log.Traces, context, WriteTraceVariant, () => (IndexType)log.Traces.Count());

  private static void WriteTraceVariant(ITraceVariant variant, BxesWriteContext context)
  {
    context.Writer.Write(variant.Count);
    WriteCollectionAndCount(variant.Events, context, WriteEvent, () => (IndexType)variant.Events.Count());
  }

  public static void WriteEvent(IEvent @event, BxesWriteContext context)
  {
    context.Writer.Write(context.ValuesIndices[new BxesStringValue(@event.Name)]);
    context.Writer.Write(@event.Timestamp);
    @event.Lifecycle.WriteTo(context);

    WriteCollectionAndCount(@event.Attributes, context, WriteKeyValueIndex, () => (IndexType)@event.Attributes.Count());
  }

  public static void WriteValues(IEventLog log, BxesWriteContext context)
  {
    var values = log.Traces
      .SelectMany(variant => variant.Events.SelectMany(EnumerateEventValues))
      .Concat(EnumerateMetadataValues(log.Metadata))
      .ToList();

    WriteCollectionAndCount(values, context, WriteValueIfNeeded, () => (IndexType)context.ValuesIndices.Count);
  }

  private static IEnumerable<BxesValue> EnumerateMetadataValues(
    IEnumerable<AttributeKeyValue> metadata)
  {
    foreach (var (key, value) in metadata)
    {
      yield return key;
      yield return value;
    }
  }

  public static void ExecuteWithFile(string filePath, Action<BinaryWriter> writeAction)
  {
    using var fs = File.OpenWrite(filePath);
    using var bw = new BinaryWriter(fs, BxesConstants.BxesEncoding);

    writeAction(bw);
  }

  public static void CreateZipArchive(IEnumerable<string> filesPaths, string outputPath)
  {
    using var fs = File.OpenWrite(outputPath);
    using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

    foreach (var filePath in filesPaths)
    {
      var fileName = Path.GetFileName(filePath);
      archive.CreateEntryFromFile(filePath, fileName, CompressionLevel.SmallestSize);
    }
  }
}