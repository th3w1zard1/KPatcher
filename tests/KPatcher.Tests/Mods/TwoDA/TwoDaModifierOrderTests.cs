using System.Collections.Generic;
using FluentAssertions;
using KPatcher.Core.Common;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Mods.TwoDA;
using Xunit;
using TwoDAFile = global::KPatcher.Core.Formats.TwoDA.TwoDA;

namespace KPatcher.Core.Tests.Mods.TwoDA
{
    /// <summary>
    /// TSLPatcher applies 2DA modifiers in changes.ini section order, not grouped by type.
    /// </summary>
    public class TwoDaModifierOrderTests
    {
        [Fact]
        public void Apply_ChangeRowBeforeAddColumn_InIniOrder_ShouldStopWithoutAddingColumn()
        {
            var twoda = new TwoDAFile(new List<string> { "Col1" });
            twoda.AddRow("0", new Dictionary<string, object> { { "Col1", "A" } });

            var config = new Modifications2DA("test.2da");
            config.Modifiers.Add(new ChangeRow2DA(
                "change_first",
                new Target(TargetType.ROW_LABEL, "0"),
                new Dictionary<string, RowValue> { { "NewCol", new RowValueConstant("X") } }));
            config.Modifiers.Add(new AddColumn2DA(
                "add_second",
                "NewCol",
                string.Empty,
                new Dictionary<int, RowValue>(),
                new Dictionary<string, RowValue>(),
                new Dictionary<int, string>()));

            var logger = new PatchLogger();
            config.Apply(twoda, new PatcherMemory(), logger, Game.K1);

            twoda.GetHeaders().Should().NotContain("NewCol");
            logger.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void Apply_AddColumnBeforeChangeRow_InIniOrder_ShouldApplyBoth()
        {
            var twoda = new TwoDAFile(new List<string> { "Col1" });
            twoda.AddRow("0", new Dictionary<string, object> { { "Col1", "A" } });

            var config = new Modifications2DA("test.2da");
            config.Modifiers.Add(new AddColumn2DA(
                "add_first",
                "NewCol",
                string.Empty,
                new Dictionary<int, RowValue>(),
                new Dictionary<string, RowValue>(),
                new Dictionary<int, string>()));
            config.Modifiers.Add(new ChangeRow2DA(
                "change_second",
                new Target(TargetType.ROW_LABEL, "0"),
                new Dictionary<string, RowValue> { { "NewCol", new RowValueConstant("X") } }));

            var logger = new PatchLogger();
            config.Apply(twoda, new PatcherMemory(), logger, Game.K1);

            twoda.GetHeaders().Should().Contain("NewCol");
            twoda.GetCellString("0", "NewCol").Should().Be("X");
            logger.Errors.Should().BeEmpty();
        }
    }
}
