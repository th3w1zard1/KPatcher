using System;

namespace KPatcher.Core.Tests.Patcher.Support
{
    public sealed class EmbeddedInstallScenario
    {
        public EmbeddedInstallScenario(
            string id,
            string changesIniBody,
            Action<ModInstallerIntegrationEnvironment> seed,
            Action<ModInstallerIntegrationEnvironment> assert = null,
            string changesIniRelative = "changes.ini")
        {
            Id = id;
            ChangesIniBody = changesIniBody;
            Seed = seed ?? (_ => { });
            Assert = assert;
            ChangesIniRelative = changesIniRelative ?? "changes.ini";
        }

        public string Id { get; }
        public string ChangesIniBody { get; }
        public string ChangesIniRelative { get; }
        public Action<ModInstallerIntegrationEnvironment> Seed { get; }
        public Action<ModInstallerIntegrationEnvironment> Assert { get; }

        public void RunInstall(ModInstallerIntegrationEnvironment env)
        {
            Seed(env);
            env.WriteChangesIni(ChangesIniBody, ChangesIniRelative);
            env.CreateInstaller(new KPatcher.Core.Logger.PatchLogger(), ChangesIniRelative).Install();
            if (Assert != null)
            {
                Assert(env);
            }
        }
    }
}
