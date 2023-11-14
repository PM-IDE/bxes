using Bxes.Models;

namespace Bxes.Tests;

public static class TestUtils
{
  public static void ExecuteTestWithTempFile(Action<string> testAction)
  {
    var tempFilePath = Path.GetTempFileName();

    try
    {
      testAction(tempFilePath);
    }
    finally
    {
      File.Delete(tempFilePath);
    }
  }

  public static void ExecuteWithTempDirectory(Action<string> testAction)
  {
    var tempDirectory = Directory.CreateTempSubdirectory();

    try
    {
      testAction(tempDirectory.FullName);
    }
    finally
    {
      Directory.Delete(tempDirectory.FullName, true);
    }
  }

  public static void ExecuteTestWithLog(IEventLog initialLog, Func<IEventLog> logProducer)
  {
    var newLog = logProducer();
    Assert.That(initialLog.Equals(newLog));
  }
}