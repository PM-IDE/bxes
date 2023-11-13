using Bxes.Models;

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
    var valuesCount = br.ReadUInt32();
    var values = new List<BxesValue>();
    
    for (uint i = 0; i < valuesCount; ++i)
    {
      values.Add(BxesValue.Parse(br));
    }

    var kvPairsCount = br.ReadUInt32();
    var keyValues = new List<KeyValuePair<uint, uint>>();

    for (uint i = 0; i < kvPairsCount; ++i)
    {
      keyValues.Add(new KeyValuePair<uint, uint>(br.ReadUInt32(), br.ReadUInt32()));
    }

    var metadataCount = br.ReadUInt32();
    var metadata = new EventLogMetadataImpl();
    for (uint i = 0; i < metadataCount; ++i)
    {
      var kv = keyValues[(int)br.ReadUInt32()];
      metadata[(BXesStringValue)values[(int)kv.Key]] = values[(int)kv.Value];
    }

    var variantsCount = br.ReadUInt32();
    var variants = new List<ITraceVariant>();
    
    for (uint i = 0; i < variantsCount; ++i)
    {
      var tracesCount = br.ReadUInt32();
      var eventsCount = br.ReadUInt32();
      var events = new List<EventImpl>();

      for (uint j = 0; j < eventsCount; ++j)
      {
        var name = (BXesStringValue)values[(int)br.ReadUInt32()];
        var timestamp = br.ReadInt64();
        var lifecycle = (IEventLifecycle)BxesValue.Parse(br);

        var attributesCount = br.ReadUInt32();
        var eventAttributes = new EventAttributesImpl();
        
        for (uint k = 0; k < attributesCount; ++k)
        {
          var kv = keyValues[(int)br.ReadUInt32()];
          eventAttributes[(BXesStringValue)values[(int)kv.Key]] = values[(int)kv.Value];
        }
        
        events.Add(new EventImpl(timestamp, name, lifecycle, eventAttributes));
      }
      
      variants.Add(new TraceVariantImpl(tracesCount, events));
    }

    return new InMemoryEventLog(metadata, variants);
  }
}

