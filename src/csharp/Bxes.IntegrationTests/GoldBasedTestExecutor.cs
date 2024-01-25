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
      var goldBytes = File.ReadAllBytes(files[0]);

      foreach (var filePath in files[1..])
      {
        var currentBytes = File.ReadAllBytes(filePath);
        Assert.That(currentBytes, Is.EquivalentTo(goldBytes));
      }
    }
    finally
    {
      Directory.Delete(tempPath, true);
    }
  }
}