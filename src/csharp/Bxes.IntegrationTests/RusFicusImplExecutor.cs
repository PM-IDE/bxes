using System.Diagnostics;

namespace Bxes.IntegrationTests;

public class RusFicusImplExecutor : ExecutorBase
{
  public override string Name => "rust";


  protected override Process CreateProcess(string xesLogPath, string bxesLogPath) => new()
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = "python",
      Arguments = $"{Environment.GetEnvironmentVariable(EnvVars.RustExecutablePath)} {xesLogPath} {bxesLogPath}"
    }
  };
}