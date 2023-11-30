using System.Xml;
using Bxes.Models;
using Bxes.Models.Values;

namespace Bxes.Xes;

public static class XesReadUtil
{
  public static (string Key, string Value, BxesValue ParsedValue) ParseAttribute(XmlReader reader)
  {
    var key = reader.GetAttribute(XesConstants.KeyAttributeName) ?? throw new XesReadException("No key at attribute");
    var value = reader.GetAttribute(XesConstants.ValueAttributeName) ?? throw new XesReadException("No value at attribute");

    BxesValue bxesValue = reader.Name switch
    {
      XesConstants.StringTagName => new BxesStringValue(value),
      XesConstants.DateTagName => new BxesTimeStampValue(DateTime.Parse(value).Ticks),
      XesConstants.IntTagName => new BxesInt64Value(long.Parse(value)),
      XesConstants.FloatTagName => new BxesFloat64Value(double.Parse(value)),
      XesConstants.BoolTagName => new BxesBoolValue(bool.Parse(value)),
      _ => throw new XesReadException($"Failed to create value for type {reader.Name}")
    };

    return (key, value, bxesValue);
  }
}