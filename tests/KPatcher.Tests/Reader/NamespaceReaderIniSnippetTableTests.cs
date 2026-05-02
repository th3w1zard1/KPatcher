using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using KPatcher.Core.Reader;
using Xunit;

namespace KPatcher.Core.Tests.Reader
{
    /// <summary>
    /// Table-driven <see cref="NamespaceReader"/> coverage from small namespaces.ini bodies.
    /// </summary>
    public sealed class NamespaceReaderIniSnippetTableTests
    {
        public static IEnumerable<object[]> NamespaceCases()
        {
            yield return new object[]
            {
                @"
[Namespaces]
Default=MainNs

[MainNs]
IniName=changes.ini
InfoName=info.rtf
",
                "MainNs",
                "changes.ini",
                "info.rtf",
                "",
                ""
            };

            yield return new object[]
            {
                @"
[Namespaces]
Release=Rel

[Rel]
IniName=rel_changes.ini
InfoName=rel_info.rtf
DataPath=tslpatchdata\rel
Name=Release build
Description=Test description
",
                "Rel",
                "rel_changes.ini",
                "rel_info.rtf",
                "tslpatchdata\\rel",
                "Release build"
            };
        }

        [Theory]
        [MemberData(nameof(NamespaceCases))]
        public void Load_parses_namespace_sections(
            string iniBody,
            string expectedId,
            string expectedIni,
            string expectedInfo,
            string expectedDataPath,
            string expectedName)
        {
            string path = Path.Combine(Path.GetTempPath(), "ns_snip_" + Guid.NewGuid().ToString("N") + ".ini");
            File.WriteAllText(path, iniBody);
            try
            {
                var list = NamespaceReader.FromFilePath(path);
                list.Should().ContainSingle();
                var ns = list.Single();
                ns.NamespaceId.Should().Be(expectedId);
                ns.IniFilename.Should().Be(expectedIni);
                ns.InfoFilename.Should().Be(expectedInfo);
                ns.DataFolderPath.Should().Be(expectedDataPath);
                ns.Name.Should().Be(expectedName);
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }
    }
}
