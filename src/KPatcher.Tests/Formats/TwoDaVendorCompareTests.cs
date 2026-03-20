using System.Collections.Generic;
using KPatcher.Core.Formats.TwoDA;
using Xunit;

namespace KPatcher.Core.Tests.Formats
{

    /// <summary>
    /// C# Reference: src/KPatcher.Tests/Formats/TwoDaVendorCompareTests.cs
    /// </summary>
    public class TwoDaVendorCompareTests
    {
        [Fact]
        public void Compare_ShouldReturnFalse_WhenColumnCountsDiffer()
        {
            var older = new TwoDA(new List<string> { "ABC", "123" });
            older.AddRow("0");

            var newer = new TwoDA(new List<string> { "ABC" });
            newer.AddRow("0");

            bool result = older.Compare(newer);

            Assert.False(result);
        }

        [Fact]
        public void Compare_ShouldReturnFalse_WhenRowCountsDiffer()
        {
            var older = new TwoDA(new List<string> { "A", "B" });
            older.AddRow("0");
            older.AddRow("1");

            var newer = new TwoDA(new List<string> { "A", "B" });
            newer.AddRow("0");

            bool result = older.Compare(newer);

            Assert.False(result);
        }

        [Fact]
        public void Compare_ShouldReturnFalse_WhenCellsDiffer()
        {
            var older = new TwoDA(new List<string> { "A", "B" });
            older.AddRow("0");
            older.AddRow("1");

            var newer = new TwoDA(new List<string> { "A", "B" });
            newer.AddRow("0");
            newer.AddRow("1");
            newer.SetCellString(0, "A", "asdf");

            bool result = older.Compare(newer);

            Assert.False(result);
        }

        [Fact]
        public void Compare_ShouldReturnTrue_WhenTablesMatch()
        {
            var older = new TwoDA(new List<string> { "A", "B" });
            older.AddRow("0");
            older.AddRow("1");
            older.SetCellString(0, "A", "asdf");

            var newer = new TwoDA(new List<string> { "A", "B" });
            newer.AddRow("0");
            newer.AddRow("1");
            newer.SetCellString(0, "A", "asdf");

            bool result = older.Compare(newer);

            Assert.True(result);
        }
    }
}

