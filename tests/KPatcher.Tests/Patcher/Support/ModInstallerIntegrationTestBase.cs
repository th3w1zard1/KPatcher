using System;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Shared disposable mod/game tree for install-path integration tests.
    /// </summary>
    public abstract class ModInstallerIntegrationTestBase : IDisposable
    {
        private readonly ModInstallerIntegrationEnvironment _env;

        protected ModInstallerIntegrationTestBase(string environmentNamePrefix)
        {
            _env = new ModInstallerIntegrationEnvironment(environmentNamePrefix);
        }

        protected string TempRoot => _env.TempRoot;
        protected string ModRoot => _env.ModRoot;
        protected string GameRoot => _env.GameRoot;
        protected string TslPatchDataPath => _env.TslPatchDataPath;
        protected string OverridePath => _env.OverridePath;
        protected string ModulesPath => _env.ModulesPath;

        protected ModInstallerIntegrationEnvironment Environment => _env;

        protected void WriteChangesIni(string body, string relativePath = "changes.ini")
        {
            _env.WriteChangesIni(body, relativePath);
        }

        protected ModInstaller CreateInstaller(PatchLogger logger = null, string changesIniRelative = "changes.ini")
        {
            return _env.CreateInstaller(logger, changesIniRelative);
        }

        public void Dispose()
        {
            _env.Dispose();
        }
    }
}
