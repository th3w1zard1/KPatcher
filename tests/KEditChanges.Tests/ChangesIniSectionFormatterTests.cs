using KEditChanges;
using Xunit;

namespace KEditChanges.Tests
{
    public sealed class ChangesIniSectionFormatterTests
    {
        [Fact]
        public void FormatEntries_compile_list_uses_source_and_destination()
        {
            var service = new ChangesIniService();
            ChangesIniDocument document = service.LoadFromIniText(
                "[Settings]\n\n[CompileList]\nFile0=test.nss\n",
                modDirectory: "/tmp/mod");

            var lines = ChangesIniSectionFormatter.FormatEntries(
                ChangesIniSectionKind.CompileList,
                document.Config);

            Assert.Single(lines);
            Assert.Contains("test.nss", lines[0]);
        }

        [Fact]
        public void FormatEntries_empty_install_list_returns_no_lines()
        {
            var service = new ChangesIniService();
            ChangesIniDocument document = service.LoadFromIniText(
                "[Settings]\n\n[InstallList]\n",
                modDirectory: "/tmp/mod");

            var lines = ChangesIniSectionFormatter.FormatEntries(
                ChangesIniSectionKind.InstallList,
                document.Config);

            Assert.Empty(lines);
        }
    }
}
