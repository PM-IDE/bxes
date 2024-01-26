using System.Diagnostics;

namespace Bxes.IntegrationTests.BxesImplExecutors;

public interface IBxesImplExecutor
{
  string Name { get; }

  void ConvertToBxes(string xesLogPath, string bxesLogPath);
}

public abstract class ExecutorBase : IBxesImplExecutor
{
  public abstract string Name { get; }


  public void ConvertToBxes(string xesLogPath, string bxesLogPath)
  {
    var process = CreateProcess(xesLogPath, bxesLogPath);
    process.Start();

    var timeout = TimeSpan.FromSeconds(10);
    if (!process.WaitForExit(timeout))
    {
      process.Kill();
      Assert.Fail($"Failed to perform conversion in {timeout}, killing process");
    }

    Assert.That(process.ExitCode, Is.Zero);
  }

  protected abstract Process CreateProcess(string xesLogPath, string bxesLogPath);
}
