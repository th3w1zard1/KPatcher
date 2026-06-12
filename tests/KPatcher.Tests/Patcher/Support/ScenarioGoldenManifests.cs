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
            },
            {
                "inline_2da_ssf_memory",
                "Override/memory.2da|55|95bd0398de46d7dfb6f91878f1b5d02227b75f3a79b8c868e10f49328d4bb586\n"
                + "Override/memory.ssf|172|daab368901c7a52796c2b8fcebad083bc17362218ca348ddd31b9b49e047d971\n"
                + EmptyExe
            },
            {
                "inline_2da_exclusive_fallback",
                "Override/excl.2da|53|17a48d1865d6c30649a3a9db5355bb3baf2013e38ddec3cfcc711ab9ea102882\n" + EmptyExe
            },
            {
                "inline_capsule_dialog_tlk",
                "Modules/capsule.mod|263|5694cc0cd0625fdc5087a394894c3869c7bef7e81ce92c9e452f9275fe5423fa\n" + EmptyExe
            },
            {
                "inline_protected_dialog_skip",
                "dialog.tlk|20|df6226dff6699a45a04b77c2b915bb127d49a3238fcb7b2e40428d9cf7ebdc25\n" + EmptyExe
            },
            {
                "inline_backup_files_replace",
                "Override/backup_target.txt|7|d7017ebcd65455e76e953d5b42fa96c3df28c7c3b616c7f069ed930fb4fae5fd\n" + EmptyExe
            },
            {
                "inline_compile_module_capsule",
                "Modules/capsule.mod|215|e395945fdba1dbe9c95bae8a0f15fa873efce3b98ab0e3dde8452075837bb3b1\n" + EmptyExe
            },
            {
                "inline_custom_nwscript_compile",
                "Override/main.ncs|31|d8a5d91faab815bdd423d0494d0509a3d233ee69539bf647b4299fdc3db9db3f\n" + EmptyExe
            },
            {
                "inline_override_type_ignore_module",
                "Modules/test.mod|195|7b33cee343efdd59a69da0bd1e33e0aa243bcd6aeda86ad57149d26722bc7e54\n"
                + "Override/shadow.ncs|3|039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81\n"
                + EmptyExe
            },
            {
                "inline_gff_add_field",
                "Override/nested.gff|96|5721a5efda7c8c02b0ea52ee92d7fdc7942cd4dc4c0a0b95361c72ae0198f9a1\n" + EmptyExe
            },
            {
                "inline_install_source_subfolder",
                "Override/subfile.txt|11|41d88240a85f2f4d5276c5d7d0aa5a55fe872feb8ac62e10cad1dba4fe565a7d\n" + EmptyExe
            },
            {
                "inline_hack_rename_source",
                "Override/patched.ncs|4|9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a\n" + EmptyExe
            },
            {
                "inline_custom_ini_name",
                "Override/custom_ini_marker.txt|10|b073bb66e7b833c2a51a43bab065686e6bd81c91720823ccd13f94b95c0985c9\n" + EmptyExe
            },
            {
                "inline_variant_ini_filename",
                "Override/variant_marker.txt|11|c661bae00e9cdd1f56915c08c4ca107c8d1e4e2dc9deda440b53e1d4274eaf81\n" + EmptyExe
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
