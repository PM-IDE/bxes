using System.Xml.Linq;
using Bxes.Models;
using Bxes.Models.Values;

namespace Bxes.Xes;

public static class XesReadUtil
{
  public static (string Key, string Value, BxesValue ParsedValue) ParseAttribute(XElement child)
  {
    string key = null!;
    string value = null!;
    foreach (var attribute in child.Attributes())
    {
      if (attribute.Name == XesConstants.KeyAttributeName)
      {
        key = attribute.Value;
      }

      if (attribute.Name == XesConstants.ValueAttributeName)
      {
        value = attribute.Value;
      }
    }

    BxesValue bxesValue = child.Name.LocalName switch
    {
      XesConstants.StringTagName => new BxesStringValue(value),
      XesConstants.DateTagName => new BxesTimeStampValue(DateTime.Parse(value).Ticks),
      XesConstants.IntTagName => new BxesInt64Value(long.Parse(value)),
      XesConstants.FloatTagName => new BxesFloat64Value(double.Parse(value)),
      XesConstants.BoolTagName => new BxesBoolValue(bool.Parse(value)),
      _ => throw new XesReadException($"Failed to create value for type {child.Name.LocalName}")
    };

    return (key, value, bxesValue);
  }
}