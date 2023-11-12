using System.Text;

namespace Bxes;

public static class BxesConstants
{
  public const UInt32 BxesVersion = 1;

  public const string LogMetadataFileName = "metadata.bxes";
  public const string ValuesFileName = "values.bxes";
  public const string KeyValuePairsFileName = "kvpair.bxes";
  public const string TracesFileName = "traces.bxes";

  public static Encoding BxesEncoding { get; } = Encoding.UTF8;
}