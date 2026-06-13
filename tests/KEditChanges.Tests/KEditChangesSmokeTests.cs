using Xunit;
using KEditChanges;

namespace KEditChanges.Tests
{
    public sealed class KEditChangesSmokeTests
    {
        [Fact]
        public void Library_info_is_present()
        {
            Assert.Contains("KEditChanges", ChangeEditReMapping.Info);
        }
    }
}
