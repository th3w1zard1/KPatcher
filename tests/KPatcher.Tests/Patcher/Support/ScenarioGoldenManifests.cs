using System.Collections.Generic;

namespace KPatcher.Core.Tests.Patcher.Support
{
    /// <summary>
    /// Expected game-tree fingerprints for inline scenarios. Refresh via
    /// <c>KP_CAPTURE_SCENARIO_GOLDENS=1</c> and <c>scripts/compute-inline-scenario-fingerprints.sh</c>.
    /// </summary>
    public static class ScenarioGoldenManifests
    {
        private const string EmptyExe = "swkotor2.exe|0|e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        public static IReadOnlyDictionary<string, string> Fingerprints { get; } = new Dictionary<string, string>
        {
            { "inline_settings_only", EmptyExe },
            {
                "inline_install_marker",
                "Override/marker.txt|9|a51a6c19a1ffc7416827e89adf20749d23ad42452c396cf7e627409f2896922c\n" + EmptyExe
            },
            {
                "inline_2da_change_row",
                "Override/patch.2da|34|814583328ce20b3e854cd535234dfcff5549ae96beff6eab9715db519e72a756\n" + EmptyExe
            },
            {
                "inline_hack_byte",
                "Override/marker.bin|4|e2733b3db9d93b0ac4e656aa12b839b92d1b7e1a4c5d97c74cb05c700672cc87\n" + EmptyExe
            },
            {
                "inline_gff_uint8",
                "Override/patch.gff|96|6d02274ee614c1d41dd51d9d73b08a7b3cd2b615b360bd50ba4fe31e44a98029\n" + EmptyExe
            },
            {
                "inline_tlk_gff_strref",
                "dialog.tlk|68|408c73aec2e86b56f1e1bccc5a269d4c789685750be4334a556a6ef222465c7c\n"
                + "Override/token.gff|96|a80ceca4f3ca1fa92961017b58acbc82c97c79f851d4d1c03f603dc8e32ff192\n"
                + EmptyExe
            },
            {
                "inline_ssf_battlecry",
                "Override/patch.ssf|172|14ab5bd53cbc05c546c7dd64708ba7caf279744aab21d84364389f660177ac94\n" + EmptyExe
            },
            {
                "inline_compile_void_main",
                "Override/main.ncs|23|436c56179b7e9fa346d47819c90bc2a2d5c1210f4af9869e7b15b2ccbb9021f3\n" + EmptyExe
            },
            {
                "inline_install_replace",
                "Override/target.txt|8|6c1aa50442a93e42c0eb2907cf4e017cd19547891fa190f3ea473582b0479290\n" + EmptyExe
            },
            {
                "inline_2da_add_column",
                "Override/cols.2da|37|6b3e8ffd8c559629a1147bad98cb7fa9fed2ab3c5ce6bc44956f4b7a7d11ce19\n" + EmptyExe
            },
            {
                "inline_installer_mode_false_hack",
                "Override/hack.ncs|4|9f076b7eb7fdc0311cd3208cdbbebbf8014dd3a05e35191c96947b358a362b40\n" + EmptyExe
            },
            {
                "inline_namespace_subfolder",
                "Override/ns_marker.txt|12|7c4060e2dabd8f482d2aeb37da65079ac6df3f55329a6be0c6b9e71e9c92e36b\n" + EmptyExe
            }
        };

        public static bool TryGetFingerprint(string scenarioId, out string fingerprint)
        {
            if (Fingerprints.TryGetValue(scenarioId, out fingerprint) && !string.IsNullOrWhiteSpace(fingerprint))
            {
                return true;
            }

            fingerprint = null;
            return false;
        }
    }
}
