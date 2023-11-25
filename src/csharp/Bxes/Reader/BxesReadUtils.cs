using System.IO.Compression;
using Bxes.Models;
using Bxes.Utils;
using Bxes.Writer;

namespace Bxes.Reader;

public readonly struct ExtractedFileCookie(string filePath) : IDisposable
{
  public FileStream Stream { get; } = File.OpenRead(filePath);


  public void Dispose()
  {
    Stream.Dispose();
    File.Delete(filePath);
  }
}

public static class BxesReadUtils
{
  public static ExtractedFileCookie ReadZipArchive(string path)
  {
    var filePath = Path.GetTempFileName();
    PathUtil.EnsureDeleted(filePath);

    ZipFile.OpenRead(path).Entries.First().ExtractToFile(filePath);
    return new ExtractedFileCookie(filePath);
  }

  public static List<BxesValue> ReadValues(BinaryReader reader)
  {
    var valuesCount = reader.ReadUInt32();
    var values = new List<BxesValue>();

    for (uint i = 0; i < valuesCount; ++i)
    {
      values.Add(BxesValue.Parse(reader, values));
    }

    return values;
  }

  public static List<KeyValuePair<uint, uint>> ReadKeyValuePairs(BinaryReader reader)
  {
    var kvPairsCount = reader.ReadUInt32();
    var keyValues = new List<KeyValuePair<uint, uint>>();

    for (uint i = 0; i < kvPairsCount; ++i)
    {
      keyValues.Add(new KeyValuePair<uint, uint>(reader.ReadUInt32(), reader.ReadUInt32()));
    }

    return keyValues;
  }

  public static IEnumerable<AttributeKeyValue> ReadMetadata(
    BinaryReader reader, List<KeyValuePair<uint, uint>> keyValues, List<BxesValue> values)
  {
    var metadataCount = reader.ReadUInt32();
    var metadata = new List<AttributeKeyValue>();
    for (uint i = 0; i < metadataCount; ++i)
    {
      var kv = keyValues[(int)reader.ReadUInt32()];
      metadata.Add(new((BxesStringValue)values[(int)kv.Key], values[(int)kv.Value]));
    }

    return metadata;
  }

  public static List<ITraceVariant> ReadVariants(
    BinaryReader reader, List<KeyValuePair<uint, uint>> keyValues, List<BxesValue> values)
  {
    var variantsCount = reader.ReadUInt32();
    var variants = new List<ITraceVariant>();

    for (uint i = 0; i < variantsCount; ++i)
    {
      var tracesCount = reader.ReadUInt32();
      var eventsCount = reader.ReadUInt32();
      var events = new List<InMemoryEventImpl>();

      for (uint j = 0; j < eventsCount; ++j)
      {
        var name = (BxesStringValue)values[(int)reader.ReadUInt32()];
        var timestamp = reader.ReadInt64();
        var lifecycle = (IEventLifecycle)BxesValue.Parse(reader, values);

        var attributesCount = reader.ReadUInt32();
        var eventAttributes = new List<AttributeKeyValue>();

        for (uint k = 0; k < attributesCount; ++k)
        {
          var kv = keyValues[(int)reader.ReadUInt32()];
          eventAttributes.Add(new((BxesStringValue)values[(int)kv.Key], values[(int)kv.Value]));
        }

        events.Add(new InMemoryEventImpl(timestamp, name, lifecycle, eventAttributes));
      }

      variants.Add(new TraceVariantImpl(tracesCount, events));
    }

    return variants;
  }
}