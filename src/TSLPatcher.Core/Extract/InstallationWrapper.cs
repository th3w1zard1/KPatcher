using System.Collections.Generic;
using TSLPatcher.Core.Installation;
using ResourceType = TSLPatcher.Core.Resources.ResourceType;
using LocationResult = TSLPatcher.Core.Resources.LocationResult;

namespace TSLPatcher.Core.Extract
{
    // Thin wrapper to mirror PyKotor extract.installation.Installation semantics.
    public class InstallationWrapper
    {
        private readonly Installation.Installation _installation;

        public InstallationWrapper(string installPath)
        {
            _installation = new Installation.Installation(installPath);
        }

        public ResourceResult Resource(string resref, ResourceType restype)
        {
            return _installation.Resources.LookupResource(resref, restype);
        }

        public List<LocationResult> Locate(string resref, ResourceType restype)
        {
            return _installation.Resources.LocateResource(resref, restype);
        }

        public Installation.Installation Inner => _installation;
    }
}

