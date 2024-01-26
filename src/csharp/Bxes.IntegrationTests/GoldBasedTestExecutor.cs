using Bxes.IntegrationTests.BxesImplExecutors;
using Bxes.Reader;
using Bxes.Xes;

namespace Bxes.IntegrationTests;

public static class GoldBasedTestExecutor
{
  public static void Execute(IEnumerable<IBxesImplExecutor> executors, string xesLogPath)
  {
    var tempPath = Directory.CreateTempSubdirectory().FullName;

    try
    {
      foreach (var executor in executors)
      {
        var bxesLogPath = Path.Combine(tempPath, $"{executor.Name}.bxes");
        executor.ConvertToBxes(xesLogPath, bxesLogPath);
      }

      var files = Directory.EnumerateFiles(tempPath).ToList();
      var goldLog = new SingleFileBxesReader().Read(files[0]);

      foreach (var filePath in files[1..])
      {
        var currentLog = new SingleFileBxesReader().Read(filePath);
        Assert.True(currentLog.Equals(goldLog));
      }
    }
    finally
    {
      Directory.Delete(tempPath, true);
    }
  }
}