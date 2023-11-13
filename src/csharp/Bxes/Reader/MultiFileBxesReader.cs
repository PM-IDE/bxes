using Bxes.Models;
using Bxes.Writer;

namespace Bxes.Reader;

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