using System.IO.Compression;
using Bxes.Models;
using Bxes.Models.Values;

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
    foreach (var value in @event.EnumerateValues())
    {
      WriteValueIfNeeded(value, context);
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
      .SelectMany(variant => variant.EnumerateKeyValuePairs())
      .Concat(log.Metadata.EnumerateKeyValuePairs());

    WriteCollectionAndCount(pairs, context, WriteKeyValuePairIfNeeded, () => (IndexType)context.KeyValueIndices.Count);
  }

  public static void WriteEventKeyValuePairs(IEvent @event, BxesWriteContext context)
  {
    foreach (var pair in @event.Attributes)
    {
      WriteKeyValuePairIfNeeded(pair, context);
    }
  }

  public static void WriteKeyValuePairIfNeeded(AttributeKeyValue pair, BxesWriteContext context)
  {
    if (context.KeyValueIndices.ContainsKey(pair)) return;

    context.Writer.Write(context.ValuesIndices[pair.Key]);
    context.Writer.Write(context.ValuesIndices[pair.Value]);

    context.KeyValueIndices[pair] = (IndexType)context.KeyValueIndices.Count;
  }

  public static void WriteEventLogMetadata(IEventLogMetadata metadata, BxesWriteContext context)
  {
    context.Writer.Write((IndexType)metadata.Metadata.Count);
    foreach (var pair in metadata.Metadata)
    {
      context.Writer.Write(context.KeyValueIndices[pair]);
    }

    context.Writer.Write((IndexType)metadata.Properties.Count);
    foreach (var pair in metadata.Properties)
    {
      context.Writer.Write(context.KeyValueIndices[pair]);
    }

    context.Writer.Write((IndexType)metadata.Extensions.Count);
    foreach (var extension in metadata.Extensions)
    {
      context.Writer.Write(context.ValuesIndices[extension.Name]);
      context.Writer.Write(context.ValuesIndices[extension.Prefix]);
      context.Writer.Write(context.ValuesIndices[extension.Uri]);
    }

    context.Writer.Write((IndexType)metadata.Globals.Count);
    foreach (var entityGlobal in metadata.Globals)
    {
      context.Writer.Write((byte)entityGlobal.Kind);
      context.Writer.Write((IndexType)entityGlobal.Globals.Count);

      foreach (var global in entityGlobal.Globals)
      {
        context.Writer.Write(context.KeyValueIndices[global]);
      }
    }

    context.Writer.Write((IndexType)metadata.Classifiers.Count);
    foreach (var classifier in metadata.Classifiers)
    {
      context.Writer.Write(context.ValuesIndices[classifier.Name]);
      context.Writer.Write((IndexType)classifier.Keys.Count);

      foreach (var key in classifier.Keys)
      {
        context.Writer.Write(context.ValuesIndices[new BxesStringValue(key.Value)]);
      }
    }
  }

  private static void WriteKeyValueIndex(AttributeKeyValue tuple, BxesWriteContext context)
  {
    context.Writer.Write(context.KeyValueIndices[tuple]);
  }

  public static void WriteTracesVariants(IEventLog log, BxesWriteContext context) =>
    WriteCollectionAndCount(log.Traces, context, WriteTraceVariant, () => (IndexType)log.Traces.Count);

  private static void WriteTraceVariant(ITraceVariant variant, BxesWriteContext context)
  {
    context.Writer.Write(variant.Count);
    
    WriteVariantMetadata(variant.Metadata, context);
    WriteCollectionAndCount(variant.Events, context, WriteEvent, () => (IndexType)variant.Events.Count);
  }

  public static void WriteVariantMetadata(IList<AttributeKeyValue> metadata, BxesWriteContext context)
  {
    var metadataCount = (IndexType)metadata.Count;
    context.Writer.Write(metadataCount);
    foreach (var pair in metadata)
    {
      context.Writer.Write(context.KeyValueIndices[pair]);
    }
  }

  public static void WriteEvent(IEvent @event, BxesWriteContext context)
  {
    context.Writer.Write(context.ValuesIndices[new BxesStringValue(@event.Name)]);
    context.Writer.Write(@event.Timestamp);
    @event.Lifecycle.WriteTo(context);

    WriteCollectionAndCount(@event.Attributes, context, WriteKeyValueIndex, () => (IndexType)@event.Attributes.Count);
  }

  public static void WriteValues(IEventLog log, BxesWriteContext context)
  {
    var values = log.Traces
      .SelectMany(variant => variant.EnumerateValues())
      .Concat(log.Metadata.EnumerateValues())
      .ToList();

    WriteCollectionAndCount(values, context, WriteValueIfNeeded, () => (IndexType)context.ValuesIndices.Count);
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