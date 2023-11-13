using Bxes.Models;

namespace Bxes.Reader;

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

    return new InMemoryEventLog(version, metadata, variants);
  }
}