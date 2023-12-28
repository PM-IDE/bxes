using System.Xml;

namespace Bxes.Xes;

public class XesReadException(XmlReader reader, string message) : BxesException
{
  public override string Message { get; } = $"{message}, content: {reader.ReadOuterXml()}";
}