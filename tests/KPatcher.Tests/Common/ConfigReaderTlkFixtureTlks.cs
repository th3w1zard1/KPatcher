using KPatcher.Core.Common;
using KPatcher.Core.Formats.TLK;

namespace KPatcher.Core.Tests.Common
{
    /// <summary>
    /// TLK graphs for <see cref="Reader.ConfigReaderTLKTests"/> built in code (no embedded .tlk bytes).
    /// Strings match assertions in <c>TLK_ComplexChanges_ShouldLoadAllModifiers</c> and default <c>append.tlk</c> StrRef rows.
    /// </summary>
    internal static class ConfigReaderTlkFixtureTlks
    {
        internal static void WriteComplexTlk(string fullPath)
        {
            BuildComplexTlk().Save(fullPath);
        }

        internal static void WriteDefaultAppendTlk(string fullPath)
        {
            BuildDefaultAppendTlk().Save(fullPath);
        }

        /// <summary>Rows 0–11 for <c>complex.tlk</c> / ReplaceFile mapping in the complex-changes test.</summary>
        internal static TLK BuildComplexTlk()
        {
            var tlk = new TLK(Language.English);
            tlk.Add(
                "Climate: None\nTerrain: Asteroid\nDocking: Peragus Mining Station\nNative Species: None",
                "");
            tlk.Add("Lehon", "");
            tlk.Add(
                "Climate: Tropical\nTerrain: Islands\nDocking: Beach Landing\nNative Species: Rakata",
                "");
            tlk.Add(
                "Climate: Temperate\nTerrain: Decaying urban zones\nDocking: Refugee Landing Pad\nNative Species: None",
                "");
            tlk.Add(
                "Climate: Tropical\nTerrain: Jungle\nDocking: Jungle Clearing\nNative Species: None",
                "");
            tlk.Add(
                "Climate: Temperate\nTerrain: Forest\nDocking: Iziz Spaceport\nNative Species: None",
                "");
            tlk.Add(
                "Climate: Temperate\nTerrain: Grasslands\nDocking: Khoonda Plains Settlement\nNative Species: None",
                "");
            tlk.Add(
                "Climate: Tectonic-Generated Storms\nTerrain: Shattered Planetoid\nDocking: No Docking Facilities Present\nNative Species: None",
                "");
            tlk.Add(
                "Climate: Arid\nTerrain: Volcanic\nDocking: Dreshae Settlement\nNative Species: Unknown",
                "");
            tlk.Add(
                "Climate: Artificially Maintained \nTerrain: Droid Cityscape\nDocking: Landing Arm\nNative Species: Unknown",
                "");
            tlk.Add(
                "Climate: Artificially Maintained\nTerrain: Space Station\nDocking: Landing Zone\nNative Species: None",
                "");
            tlk.Add(
                "Opo Chano, Czerka's contracted droid technician, can't give you his droid credentials unless you help relieve his 2,500 credit gambling debt to the Exchange. Without them, you can't take B-4D4.",
                "");
            return tlk;
        }

        /// <summary>Rows 0–13 for default <c>append.tlk</c> (StrRef0..StrRef13 in the complex-changes INI).</summary>
        internal static TLK BuildDefaultAppendTlk()
        {
            var tlk = new TLK(Language.English);
            tlk.Add("Yavin", "");
            tlk.Add(
                "Climate: Artificially Controled\nTerrain: Space Station\nDocking: Orbital Docking\nNative Species: Unknown",
                "");
            tlk.Add("Tatooine", "");
            tlk.Add(
                "Climate: Arid\nTerrain: Desert\nDocking: Anchorhead Spaceport\nNative Species: Unknown",
                "");
            tlk.Add("Manaan", "");
            tlk.Add(
                "Climate: Temperate\nTerrain: Ocean\nDocking: Ahto City Docking Bay\nNative Species: Selkath",
                "");
            tlk.Add("Kashyyyk", "");
            tlk.Add(
                "Climate: Temperate\nTerrain: Forest\nDocking: Czerka Landing Pad\nNative Species: Wookies",
                "");
            tlk.Add("", "");
            tlk.Add("", "");
            tlk.Add("Sleheyron", "");
            tlk.Add(
                "Climate: Unknown\nTerrain: Cityscape\nDocking: Unknown\nNative Species: Unknown",
                "");
            tlk.Add("Coruscant", "");
            tlk.Add(
                "Climate: Unknown\nTerrain: Unknown\nDocking: Unknown\nNative Species: Unknown",
                "");
            return tlk;
        }
    }
}
