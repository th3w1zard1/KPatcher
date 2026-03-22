using Xunit;

namespace KEditChanges.Tests
{
    public sealed class KEditChangesSmokeTests
    {
        [Fact]
        public void Placeholder_string_is_present()
        {
            Assert.Contains("KEditChanges", global::KEditChanges.ChangeEditPlaceholder.Info);
        }
    }
}
