namespace Bxes.Logging;

public interface ILogger
{
  void LogWarning(string message);
}

public class BxesLogger : ILogger
{
  public void LogWarning(string message)
  {
    Console.WriteLine(message);
  }
}