using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using KPatcher.UI.Resources;

namespace KPatcher.UI.Update
{
    /// <summary>
    /// Strongly typed representation of the JSON payload embedded in Config.cs.
    /// </summary>
    public sealed class RemoteUpdateInfo
    {
        [JsonPropertyName("kpatcherLatestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherLatestNotes")]
        public string LatestReleaseNotes { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherDownloadLink")]
        public string ReleaseDownloadPage { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherDirectLinks")]
        public Dictionary<string, Dictionary<string, List<string>>> ReleaseDirectLinks { get; set; }
            = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("kpatcherLatestBetaVersion")]
        public string LatestBetaVersion { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherLatestBetaNotes")]
        public string LatestBetaNotes { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherBetaDownloadLink")]
        public string BetaDownloadPage { get; set; } = string.Empty;

        [JsonPropertyName("kpatcherBetaDirectLinks")]
        public Dictionary<string, Dictionary<string, List<string>>> BetaDirectLinks { get; set; }
            = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the set of mirrors for the current platform/architecture.
        /// </summary>
        public IReadOnlyList<string> GetPlatformMirrors(bool useBetaChannel)
        {
            string platformKey = DetectPlatformKey();
            string archKey = DetectArchitectureKey();
            var table = useBetaChannel ? BetaDirectLinks : ReleaseDirectLinks;

            if (table is null ||
                !table.TryGetValue(platformKey, out var architectures) ||
                architectures is null ||
                !architectures.TryGetValue(archKey, out var mirrors) ||
                mirrors is null ||
                mirrors.Count == 0)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, UIResources.NoDirectDownloadLinksForFormat, platformKey, archKey));
            }

            return mirrors;
        }

        /// <summary>
        /// Gets the latest version string for the selected channel.
        /// </summary>
        public string GetChannelVersion(bool useBetaChannel)
        {
            string version = useBetaChannel ? LatestBetaVersion : LatestVersion;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = LatestVersion;
            }
            return version;
        }

        /// <summary>
        /// Gets the release notes associated with the selected channel.
        /// </summary>
        public string GetChannelNotes(bool useBetaChannel)
        {
            string notes = useBetaChannel ? LatestBetaNotes : LatestReleaseNotes;
            if (string.IsNullOrWhiteSpace(notes))
            {
                return string.Empty;
            }
            return notes;
        }

        /// <summary>
        /// Gets the fallback download page for manual downloads.
        /// </summary>
        public string GetDownloadPage(bool useBetaChannel)
        {
            string url = useBetaChannel ? BetaDownloadPage : ReleaseDownloadPage;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = ReleaseDownloadPage;
            }
            return url;
        }

        private static string DetectPlatformKey()
        {
            if (OperatingSystem.IsWindows())
            {
                return "Windows";
            }

            if (OperatingSystem.IsLinux())
            {
                return "Linux";
            }

            if (OperatingSystem.IsMacOS() || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Darwin";
            }

            throw new PlatformNotSupportedException(UIResources.UnsupportedOsForAutomaticUpdates);
        }

        private static string DetectArchitectureKey()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 || RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                return "64bit";
            }

            return "32bit";
        }
    }

    internal static class RemoteUpdateInfoParser
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static RemoteUpdateInfo FromDictionary(IReadOnlyDictionary<string, object> raw)
        {
            if (raw is null || raw.Count == 0)
            {
                throw new InvalidOperationException(UIResources.RemoteUpdatePayloadEmpty);
            }

            // If the dictionary already contains the typed payload we can shortcut.
            if (raw.TryGetValue("__typed", out object typed) && typed is RemoteUpdateInfo cached)
            {
                return cached;
            }

            // Serialize and rehydrate into the strongly typed model to ensure consistent parsing
            // regardless of whether the dictionary contains JsonElements or CLR objects.
            string json = JsonSerializer.Serialize(raw, SerializerOptions);
            var info = JsonSerializer.Deserialize<RemoteUpdateInfo>(json, SerializerOptions);
            if (info is null)
            {
                throw new InvalidOperationException(UIResources.FailedToParseRemoteUpdatePayload);
            }

            return info;
        }
    }
}

