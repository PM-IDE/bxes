﻿using System.Diagnostics;

namespace Bxes.IntegrationTests.BxesImplExecutors;

public class RustFicusImplExecutor : ExecutorBase
{
  public override string Name => "ficus";


  protected override Process CreateProcess(string xesLogPath, string bxesLogPath) => new()
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = "python",
      Arguments = $"{TestDataProvider.FicusRustExecutable} {xesLogPath} {bxesLogPath}",
    }
  };
}