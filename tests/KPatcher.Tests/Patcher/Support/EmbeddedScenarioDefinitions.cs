using System;
using System.Collections.Generic;
using System.IO;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Patcher.Support
{
  public sealed class EmbeddedInstallScenario
  {
    public EmbeddedInstallScenario(string id, string changesIniBody, Action<ModInstallerIntegrationEnvironment> seed)
    {
      Id = id;
      ChangesIniBody = changesIniBody;
      Seed = seed ?? (_ => { });
    }

    public string Id { get; }
    public string ChangesIniBody { get; }
    public Action<ModInstallerIntegrationEnvironment> Seed { get; }
  }

  /// <summary>
  /// Inline characterization scenarios (no committed mod trees). Manifest inventory rows without inline bodies
  /// remain tracked in <c>manifest.json</c> for future oracle expansion.
  /// </summary>
  public static class EmbeddedScenarioDefinitions
  {
    public static IReadOnlyList<EmbeddedInstallScenario> RunnableScenarios { get; } = new List<EmbeddedInstallScenario>
    {
      new EmbeddedInstallScenario(
        "inline_settings_only",
        "[Settings]\nLogLevel=3\n",
        env => { }),

      new EmbeddedInstallScenario(
        "inline_install_marker",
        @"
[Settings]
LogLevel=3
InstallerMode=1

[InstallList]
folder0=Override

[folder0]
File0=marker.txt
",
        env =>
        {
          File.WriteAllText(Path.Combine(env.TslPatchDataPath, "marker.txt"), "installed");
        }),

      new EmbeddedInstallScenario(
        "inline_2da_change_row",
        @"
[Settings]
LogLevel=3

[2DAList]
Table0=patch.2da

[patch.2da]
ChangeRow0=change_row_0

[change_row_0]
RowIndex=0
label=patched
",
        env =>
        {
          var twoda = new TwoDAFile(new List<string> { "label" });
          twoda.AddRow("0", new Dictionary<string, object> { { "label", "original" } });
          File.WriteAllBytes(Path.Combine(env.OverridePath, "patch.2da"), twoda.ToBytes());
        }),

      new EmbeddedInstallScenario(
        "inline_hack_byte",
        @"
[Settings]
LogLevel=3

[HACKList]
Replace0=marker.bin

[marker.bin]
0x0=u8:55
",
        env =>
        {
          byte[] bytes = new byte[] { 0, 0, 0, 0 };
          File.WriteAllBytes(Path.Combine(env.OverridePath, "marker.bin"), bytes);
          File.WriteAllBytes(Path.Combine(env.TslPatchDataPath, "marker.bin"), bytes);
        })
    };
  }
}
