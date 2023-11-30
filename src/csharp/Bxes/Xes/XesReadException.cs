namespace Bxes.Xes;

public class XesReadException(string message) : BxesException
{
  public override string Message { get; } = message;
}