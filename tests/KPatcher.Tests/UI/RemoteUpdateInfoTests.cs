using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using FluentAssertions;
using KPatcher.UI.Update;
using Xunit;

namespace KPatcher.Core.Tests.UI
{
    public sealed class RemoteUpdateInfoTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void Json_roundtrip_populates_release_fields()
        {
            const string json = "{ \"kpatcherLatestVersion\": \"2.1.0\", \"kpatcherLatestNotes\": \"Notes here\", \"kpatcherDownloadLink\": \"https://example.com/releases\" }";
            var info = JsonSerializer.Deserialize<RemoteUpdateInfo>(json, JsonOptions);
            info.Should().NotBeNull();
            if (info == null)
            {
                return;
            }

            info.LatestVersion.Should().Be("2.1.0");
            info.LatestReleaseNotes.Should().Be("Notes here");
            info.ReleaseDownloadPage.Should().Be("https://example.com/releases");
        }

        [Fact]
        public void GetChannelVersion_prefers_stable_then_falls_back_when_beta_empty()
        {
            var info = new RemoteUpdateInfo
            {
                LatestVersion = "1.0.0",
                LatestBetaVersion = ""
            };
            info.GetChannelVersion(useBetaChannel: false).Should().Be("1.0.0");
            info.GetChannelVersion(useBetaChannel: true).Should().Be("1.0.0");
        }

        [Fact]
        public void GetChannelVersion_uses_beta_when_set()
        {
            var info = new RemoteUpdateInfo
            {
                LatestVersion = "1.0.0",
                LatestBetaVersion = "1.1.0-beta"
            };
            info.GetChannelVersion(useBetaChannel: true).Should().Be("1.1.0-beta");
        }

        [Fact]
        public void GetChannelNotes_returns_empty_when_missing()
        {
            var info = new RemoteUpdateInfo();
            info.GetChannelNotes(false).Should().BeEmpty();
            info.GetChannelNotes(true).Should().BeEmpty();
        }

        [Fact]
        public void GetDownloadPage_falls_back_to_release_when_beta_page_empty()
        {
            var info = new RemoteUpdateInfo
            {
                ReleaseDownloadPage = "https://a.example",
                BetaDownloadPage = ""
            };
            info.GetDownloadPage(true).Should().Be("https://a.example");
        }

        [Fact]
        public void GetPlatformMirrors_returns_list_for_current_runtime_keys()
        {
            var (platformKey, archKey) = CurrentPlatformKeys();
            var info = new RemoteUpdateInfo
            {
                ReleaseDirectLinks = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase)
                {
                    [platformKey] = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        [archKey] = new List<string> { "https://mirror.example/pkg.zip" }
                    }
                }
            };

            info.GetPlatformMirrors(false).Should().Equal("https://mirror.example/pkg.zip");
        }

        private static (string Platform, string Arch) CurrentPlatformKeys()
        {
            string platform;
            if (OperatingSystem.IsWindows())
            {
                platform = "Windows";
            }
            else if (OperatingSystem.IsLinux())
            {
                platform = "Linux";
            }
            else if (OperatingSystem.IsMacOS() || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "Darwin";
            }
            else
            {
                throw new PlatformNotSupportedException("Test runner OS not mapped");
            }

            string arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                          || RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "64bit"
                : "32bit";

            return (platform, arch);
        }
    }
}
