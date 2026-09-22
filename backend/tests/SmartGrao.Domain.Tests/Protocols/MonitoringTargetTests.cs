using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;
using Xunit;

namespace SmartGrao.Domain.Tests.Protocols;

public sealed class MonitoringTargetTests
{
    private static MonitoringTarget Armyworm() => MonitoringTarget.Create(
        "spodoptera_frugiperda", " Lagarta-do-cartucho ", " Spodoptera frugiperda ",
        TargetKind.Pest, Crop.Corn);

    [Fact]
    public void ANewTargetPromisesNoAutomation()
    {
        Assert.Equal(AutomationCapability.ManualRecord, Armyworm().Automation);
    }

    [Fact]
    public void EnablingAutomationWithoutValidationIsRejected()
    {
        var target = Armyworm();

        var error = Assert.Throws<DomainException>(
            () => target.ChangeAutomation(AutomationCapability.AutomationEnabled));

        Assert.Equal("protocol.automation_skips_validation", error.Error.Code);
        Assert.Equal(AutomationCapability.ManualRecord, target.Automation);
    }

    [Fact]
    public void AutomationIsReachedThroughValidationAndCanBeRolledBackInOneStep()
    {
        var target = Armyworm();

        target.ChangeAutomation(AutomationCapability.UnderValidation);
        target.ChangeAutomation(AutomationCapability.AutomationEnabled);
        Assert.Equal(AutomationCapability.AutomationEnabled, target.Automation);

        target.ChangeAutomation(AutomationCapability.ManualRecord);
        Assert.Equal(AutomationCapability.ManualRecord, target.Automation);
    }

    [Fact]
    public void RepeatingTheCurrentCapabilityLeavesTheTargetUntouched()
    {
        var target = Armyworm();
        target.ChangeAutomation(AutomationCapability.ManualRecord);
        Assert.Null(target.UpdatedAt);
    }

    [Theory]
    [InlineData("Spodoptera")]
    [InlineData("spodoptera frugiperda")]
    [InlineData("spodoptera-frugiperda")]
    [InlineData("_frugiperda")]
    [InlineData("frugiperda_")]
    [InlineData("")]
    public void AnUnstableCodeIsRejected(string code)
    {
        var error = Assert.Throws<DomainException>(() => MonitoringTarget.Create(
            code, "Lagarta-do-cartucho", "Spodoptera frugiperda", TargetKind.Pest, Crop.Corn));

        Assert.Equal("protocol.invalid_target_code", error.Error.Code);
    }

    [Fact]
    public void NamesAreTrimmedAndLengthLimited()
    {
        var target = Armyworm();

        Assert.Equal("spodoptera_frugiperda", target.Code);
        Assert.Equal("Lagarta-do-cartucho", target.CommonName);
        Assert.Equal("Spodoptera frugiperda", target.ScientificName);
        Assert.Throws<DomainException>(() => target.Rename(new string('x', 121), "Valid"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AnUndefinedOrUnknownKindIsRejected(int kind)
    {
        Assert.Throws<DomainException>(() => MonitoringTarget.Create(
            "code", "Common", "Scientific", (TargetKind)kind, Crop.Corn));
    }

    [Fact]
    public void AnUndefinedCropIsRejected()
    {
        Assert.Throws<DomainException>(() => MonitoringTarget.Create(
            "code", "Common", "Scientific", TargetKind.FoliarDisease, Crop.Undefined));
    }
}
