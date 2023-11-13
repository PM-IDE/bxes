using Bxes.Models;
using Bxes.Writer;

namespace Bxes;

public interface IBxesReader
{
  IEventLog Read(string path);
}

public class SingleFileBxesReader : IBxesReader
{
  public IEventLog Read(string path)
  {
    using var br = new BinaryReader(File.OpenRead(path));

    var version = br.ReadUInt32();
    var values = BxesReadUtils.ReadValues(br);
    var keyValues = BxesReadUtils.ReadKeyValuePairs(br);
    var metadata = BxesReadUtils.ReadMetadata(br, keyValues, values);
    var variants = BxesReadUtils.ReadVariants(br, keyValues, values);

    return new InMemoryEventLog(metadata, variants);
  }
}

public class MultiFileBxesReader : IBxesReader
{
  public IEventLog Read(string path)
  {
    if (!Directory.Exists(path)) throw new SavePathIsNotDirectoryException(path);

    void OpenRead(string fileName, Action<BinaryReader> action)
    {
      using var reader = new BinaryReader(File.OpenRead(Path.Combine(path, fileName)));
      action(reader);
    }

    List<BxesValue> values = null!;
    OpenRead(BxesConstants.ValuesFileName, reader =>
    {
      var version = reader.ReadUInt32();
      values = BxesReadUtils.ReadValues(reader);
    });

    List<KeyValuePair<uint, uint>> keyValues = null!;
    OpenRead(BxesConstants.KVPairsFileName, reader =>
    {
      var version = reader.ReadUInt32();
      keyValues = BxesReadUtils.ReadKeyValuePairs(reader);
    });

    EventLogMetadataImpl metadata = null!;
    OpenRead(BxesConstants.MetadataFileName, reader =>
    {
      var version = reader.ReadUInt32();
      metadata = BxesReadUtils.ReadMetadata(reader, keyValues, values);
    });

    List<ITraceVariant> variants = null!;
    OpenRead(BxesConstants.TracesFileName, reader =>
    {
      var version = reader.ReadUInt32();
      variants = BxesReadUtils.ReadVariants(reader, keyValues, values);
    });

    return new InMemoryEventLog(metadata, variants);
  }
}


public static class BxesReadUtils
{
  public static List<BxesValue> ReadValues(BinaryReader reader)
  {
    var valuesCount = reader.ReadUInt32();
    var values = new List<BxesValue>();
    
    for (uint i = 0; i < valuesCount; ++i)
    {
      values.Add(BxesValue.Parse(reader));
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

  public static EventLogMetadataImpl ReadMetadata(
    BinaryReader reader, List<KeyValuePair<uint, uint>> keyValues, List<BxesValue> values)
  {
    var metadataCount = reader.ReadUInt32();
    var metadata = new EventLogMetadataImpl();
    for (uint i = 0; i < metadataCount; ++i)
    {
      var kv = keyValues[(int)reader.ReadUInt32()];
      metadata[(BXesStringValue)values[(int)kv.Key]] = values[(int)kv.Value];
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
      var events = new List<EventImpl>();

      for (uint j = 0; j < eventsCount; ++j)
      {
        var name = (BXesStringValue)values[(int)reader.ReadUInt32()];
        var timestamp = reader.ReadInt64();
        var lifecycle = (IEventLifecycle)BxesValue.Parse(reader);

        var attributesCount = reader.ReadUInt32();
        var eventAttributes = new EventAttributesImpl();
        
        for (uint k = 0; k < attributesCount; ++k)
        {
          var kv = keyValues[(int)reader.ReadUInt32()];
          eventAttributes[(BXesStringValue)values[(int)kv.Key]] = values[(int)kv.Value];
        }
        
        events.Add(new EventImpl(timestamp, name, lifecycle, eventAttributes));
      }
      
      variants.Add(new TraceVariantImpl(tracesCount, events));
    }

    return variants;
  }
}