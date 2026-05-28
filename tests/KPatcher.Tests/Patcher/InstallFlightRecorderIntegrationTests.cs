using System;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using KPatcher.Core.Logger;
using KPatcher.Core.Patcher;
using Xunit;

namespace KPatcher.Core.Tests.Patcher
{
    public sealed class InstallFlightRecorderIntegrationTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly string _modRoot;
        private readonly string _gameRoot;
        private readonly string _tslPatchDataPath;

        public InstallFlightRecorderIntegrationTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "KPatcher_InstallFlightRecorderIntegrationTests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            _modRoot = Path.Combine(_tempRoot, "mod");
            _gameRoot = Path.Combine(_tempRoot, "game");
            _tslPatchDataPath = Path.Combine(_modRoot, "tslpatchdata");

            Directory.CreateDirectory(_tslPatchDataPath);
            Directory.CreateDirectory(_gameRoot);
            File.WriteAllText(Path.Combine(_gameRoot, "swkotor2.exe"), string.Empty);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                try
                {
                    Directory.Delete(_tempRoot, true);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        [Fact]
        public void Install_Success_WritesFlightRecordAndAnnouncesPath()
        {
            WriteChangesIni("[Settings]\nLogLevel=3\n");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string recordPath = Path.Combine(_tslPatchDataPath, "installrecord.txt");
            File.Exists(recordPath).Should().BeTrue();
            File.ReadAllText(recordPath).Should().Contain("Outcome: Success");
            logger.Notes.Should().Contain(note => note.Message.Contains("Install record written to"));
        }

        [Fact]
        public void Install_Failure_FinalizesFlightRecordBeforeThrowing()
        {
            WriteChangesIni("[Settings]\nRequired=missing.file\nRequiredMsg=Need missing.file\n");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            Action act = () => installer.Install();

            act.Should().Throw<InvalidOperationException>();

            string recordPath = Path.Combine(_tslPatchDataPath, "installrecord.txt");
            File.Exists(recordPath).Should().BeTrue();
            string content = File.ReadAllText(recordPath);
            content.Should().Contain("Outcome: Failure");
            content.Should().Contain("Need missing.file");
            logger.Notes.Should().Contain(note => note.Message.Contains("Install record written to"));
        }

        [Fact]
        public void Install_Cancellation_FinalizesFlightRecordAndThrowsOperationCanceledException()
        {
            WriteChangesIni(@"
[Settings]
LogLevel=3

[InstallList]
install_folder0=Override

[install_folder0]
!SourceFolder=.
File0=sample.wav
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Action act = () => installer.Install(cancellationTokenSource.Token);

            act.Should().Throw<OperationCanceledException>();

            string recordPath = Path.Combine(_tslPatchDataPath, "installrecord.txt");
            File.Exists(recordPath).Should().BeTrue();
            string content = File.ReadAllText(recordPath);
            content.Should().Contain("Outcome: Cancelled");
            logger.Notes.Should().Contain(note => note.Message.Contains("Install record written to"));
        }

        [Fact]
        public void Install_HackListMissingAlternateSource_UsesVendorErrorWithoutGenericFollowup()
        {
            WriteChangesIni(@"
[Settings]
LogLevel=3

[HACKList]
script.ncs=script.ncs

[script.ncs]
!SourceFile=source-alt.ncs
!SaveAs=patched.ncs
0x0=u8:1
");

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            logger.Errors.Should().Contain(log => log.Message.Contains("Unable to locate source file \"source-alt.ncs\" to rename to \"script.ncs\" and install, skipping...", StringComparison.Ordinal));
            logger.Errors.Should().NotContain(log => log.Message.Contains("Could not load source file to hack", StringComparison.OrdinalIgnoreCase));
            logger.Errors.Should().NotContain(log => log.Message.Contains("Critical error: Unable to locate file to patch", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Install_HackListRenamedSource_UsesVendorCopyToOverrideMessage()
        {
            WriteChangesIni(@"
[Settings]
LogLevel=3

[HACKList]
script.ncs=script.ncs

[script.ncs]
!SourceFile=source-alt.ncs
!SaveAs=patched.ncs
0x0=u8:1
");
            File.WriteAllBytes(Path.Combine(_tslPatchDataPath, "source-alt.ncs"), new byte[] { 0, 2, 3, 4 });

            var logger = new PatchLogger();
            var installer = new ModInstaller(_modRoot, _gameRoot, Path.Combine(_tslPatchDataPath, "changes.ini"), logger);

            installer.Install();

            string installedPath = Path.Combine(_gameRoot, "Override", "patched.ncs");
            File.Exists(installedPath).Should().BeTrue();
            File.ReadAllBytes(installedPath)[0].Should().Be(1);
            logger.Notes.Should().Contain(log => log.Message.Contains("Copying file patched.ncs to Override folder...", StringComparison.Ordinal));
            logger.Notes.Should().NotContain(log => log.Message.Contains("Hacking 'source-alt.ncs' and saving as 'patched.ncs'", StringComparison.Ordinal));
        }

        private void WriteChangesIni(string body)
        {
            File.WriteAllText(Path.Combine(_tslPatchDataPath, "changes.ini"), body);
        }
    }
}
