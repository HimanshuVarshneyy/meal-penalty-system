using MealPenalty.Application;
using MealPenalty.Application.Engine;
using MealPenalty.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MealPenalty.Tests;

/// <summary>
/// Regression tests built directly from the handwritten "sample rule" / "sample timesheet"
/// worked examples: hourly rate $40, rows (6-7 AMOUNT $25), (7-9 HOUR 0.5h/0.5h),
/// (9-24 DAILY 10%/1h).
/// </summary>
public class PenaltyCalculationEngineTests
{
    private readonly IPenaltyCalculationEngine _engine;

    public PenaltyCalculationEngineTests()
    {
        var services = new ServiceCollection().AddMealPenaltyApplication().BuildServiceProvider();
        _engine = services.GetRequiredService<IPenaltyCalculationEngine>();
    }

    private static PenaltyRuleSet SampleRuleSet() => new()
    {
        Id = 1,
        Name = "Sample",
        Rules =
        [
            new PenaltyRule { Id = 1, RowOrder = 1, StartHours = 6, EndHours = 7, PenaltyType = PenaltyType.Amount, Value = 25, IntervalHours = null },
            new PenaltyRule { Id = 2, RowOrder = 2, StartHours = 7, EndHours = 9, PenaltyType = PenaltyType.Hour, Value = 0.5m, IntervalHours = 0.5m },
            new PenaltyRule { Id = 3, RowOrder = 3, StartHours = 9, EndHours = 24, PenaltyType = PenaltyType.Daily, Value = 10, IntervalHours = 1m }
        ]
    };

    [Fact]
    public void Monday_TwoWindowsUnderSixHours_NoPenalty()
    {
        var entry = new TimesheetEntry
        {
            Label = "Mon",
            InHours = 8.0m,
            OutHours = 18.0m,
            HourlyRate = 40m,
            MealBreaks = [new MealBreak { SeqNo = 1, MealStart = 13.0m, MealEnd = 14.0m }]
        };

        var result = _engine.Calculate(entry, SampleRuleSet());

        Assert.Equal(9.0m, result.PaidHours);
        Assert.Equal(0m, result.TotalPenalty);
        Assert.Equal(360m, result.TotalPaidAmount);
        Assert.Empty(result.Triggers);
    }

    [Fact]
    public void Tuesday_FirstWindowOverSixHours_TriggersRowsOneAndTwo()
    {
        var entry = new TimesheetEntry
        {
            Label = "Tue",
            InHours = 8.0m,
            OutHours = 20.0m,
            HourlyRate = 40m,
            MealBreaks = [new MealBreak { SeqNo = 1, MealStart = 15.2m, MealEnd = 15.7m }]
        };

        var result = _engine.Calculate(entry, SampleRuleSet());

        Assert.Equal(11.5m, result.PaidHours);
        Assert.Equal(45m, result.TotalPenalty);
        Assert.Equal(505m, result.TotalPaidAmount);
        Assert.Equal(2, result.Triggers.Count);

        var row1 = result.Triggers.Single(t => t.RuleId == 1);
        Assert.Equal(1, row1.TriggerCount);
        Assert.Equal(25m, row1.PenaltyAmount);

        var row2 = result.Triggers.Single(t => t.RuleId == 2);
        Assert.Equal(1, row2.TriggerCount);
        Assert.Equal(20m, row2.PenaltyAmount);
    }

    [Fact]
    public void Wednesday_NoMealSingleTenHourWindow_TriggersAllThreeRows()
    {
        var entry = new TimesheetEntry
        {
            Label = "Wed",
            InHours = 6.0m,
            OutHours = 16.0m,
            HourlyRate = 40m,
            MealBreaks = []
        };

        var result = _engine.Calculate(entry, SampleRuleSet());

        Assert.Equal(10.0m, result.PaidHours);
        Assert.Equal(137m, result.TotalPenalty);
        Assert.Equal(537m, result.TotalPaidAmount);

        var row1 = result.Triggers.Single(t => t.RuleId == 1);
        Assert.Equal(1, row1.TriggerCount);
        Assert.Equal(25m, row1.PenaltyAmount);

        var row2 = result.Triggers.Single(t => t.RuleId == 2);
        Assert.Equal(4, row2.TriggerCount);
        Assert.Equal(80m, row2.PenaltyAmount);

        var row3 = result.Triggers.Single(t => t.RuleId == 3);
        Assert.Equal(1, row3.TriggerCount);
        Assert.Equal(32m, row3.PenaltyAmount);
    }

    [Fact]
    public void Row_ExactlyAtStartHours_DoesNotTrigger()
    {
        var entry = new TimesheetEntry
        {
            Label = "EdgeCase",
            InHours = 0m,
            OutHours = 6.0m,
            HourlyRate = 40m,
            MealBreaks = []
        };

        var result = _engine.Calculate(entry, SampleRuleSet());

        Assert.Empty(result.Triggers);
    }
}
