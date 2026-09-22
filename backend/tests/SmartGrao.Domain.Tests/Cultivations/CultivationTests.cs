using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using Xunit;

namespace SmartGrao.Domain.Tests.Cultivations;

public sealed class CultivationTests
{
    private static readonly DateOnly Planting = new(2026, 1, 10);
    private static Cultivation Create() => Cultivation.Create(
        FieldId.New(), SeasonId.New(), Crop.Corn, " Hybrid A ", Planting);

    [Fact]
    public void SuccessiveCyclesKeepSeparateContextAndStages()
    {
        var field = FieldId.New();
        var season = SeasonId.New();
        var first = Cultivation.Create(field, season, Crop.Corn, "A", Planting);
        first.RecordStage(Planting.AddDays(20), "V4", "First cycle");
        first.Close(Planting.AddDays(100));
        var second = Cultivation.Create(field, season, Crop.Soybean, "B", Planting.AddDays(101));
        second.RecordStage(Planting.AddDays(120), "V2", null);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(Crop.Corn, first.Crop);
        Assert.Equal("A", first.Cultivar);
        Assert.Equal("V4", Assert.Single(first.Stages).Stage);
        Assert.Equal("V2", Assert.Single(second.Stages).Stage);
        Assert.Null(second.EndedOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void UndefinedOrUnknownCropIsRejected(int value)
    {
        Assert.Throws<DomainException>(() => Cultivation.Create(
            FieldId.New(), SeasonId.New(), (Crop)value, "A", Planting));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CultivarIsRequired(string cultivar)
    {
        Assert.Throws<DomainException>(() => Cultivation.Create(
            FieldId.New(), SeasonId.New(), Crop.Corn, cultivar, Planting));
    }

    [Fact]
    public void ClosingBeforeAnObservationIsRejectedWithoutChangingTheCycle()
    {
        var cycle = Create();
        cycle.RecordStage(Planting.AddDays(30), "V6", null);
        var error = Assert.Throws<DomainException>(() => cycle.Close(Planting.AddDays(29)));
        Assert.Equal("cultivation.invalid_date", error.Error.Code);
        Assert.Null(cycle.EndedOn);
        Assert.Single(cycle.Stages);
    }

    [Fact]
    public void ClosureIsIdempotentButCannotBeChanged()
    {
        var cycle = Create();
        cycle.Close(Planting.AddDays(100));
        var updatedAt = cycle.UpdatedAt;
        cycle.Close(Planting.AddDays(100));
        Assert.Equal(updatedAt, cycle.UpdatedAt);
        Assert.Throws<DomainException>(() => cycle.Close(Planting.AddDays(101)));
    }

    [Fact]
    public void ObservationsMustFallWithinTheCycleIncludingAfterClosure()
    {
        var cycle = Create();
        cycle.Close(Planting.AddDays(100));
        Assert.Throws<DomainException>(() => cycle.RecordStage(Planting.AddDays(-1), "V1", null));
        Assert.Throws<DomainException>(() => cycle.RecordStage(Planting.AddDays(101), "R6", null));
        cycle.RecordStage(Planting.AddDays(90), "R6", "Retrospective record");
        Assert.Single(cycle.Stages);
    }

    [Fact]
    public void DuplicateObservationDateDoesNotOverwriteTheOriginal()
    {
        var cycle = Create();
        cycle.RecordStage(Planting, "VE", "original");
        Assert.Throws<DomainException>(() => cycle.RecordStage(Planting, "V2", "replacement"));
        Assert.Equal("original", Assert.Single(cycle.Stages).Notes);
    }

    [Fact]
    public void SeasonAndCultivarAreTrimmedAndLengthLimited()
    {
        Assert.Equal("2026/27", Season.Create(FarmId.New(), " 2026/27 ").Name);
        Assert.Equal("Hybrid A", Create().Cultivar);
        Assert.Throws<DomainException>(() => Season.Create(FarmId.New(), new string('x', 81)));
        Assert.Throws<DomainException>(() => Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Corn, new string('x', 121), Planting));
    }

    // ── Escala fenológica (dívida da fase 1, fechada na fase 2) ───────────────

    [Fact]
    public void AStageOutsideTheScaleOfTheCropIsRejected()
    {
        var cycle = Create();

        var error = Assert.Throws<DomainException>(
            () => cycle.RecordStage(Planting.AddDays(30), "seis folhas", null));

        Assert.Equal("cultivation.stage_not_in_scale", error.Error.Code);
        Assert.Empty(cycle.Stages);
    }

    /// <summary>
    /// "v6" e "V6" sao o mesmo estadio. Gravar os dois como estao separaria em duas coisas o que e
    /// uma so, e quem comparasse dois ciclos — ou alimentasse um modelo — veria duas.
    /// </summary>
    [Theory]
    [InlineData("v6")]
    [InlineData(" V6 ")]
    [InlineData("V6")]
    public void AStageCodeIsStoredInTheCanonicalFormOfTheScale(string typed)
    {
        var cycle = Create();
        cycle.RecordStage(Planting.AddDays(30), typed, null);
        Assert.Equal("V6", Assert.Single(cycle.Stages).Stage);
    }

    [Fact]
    public void TheReproductiveScaleDiffersBetweenCornAndSoybean()
    {
        var corn = Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Corn, "A", Planting);
        var soybean = Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Soybean, "B", Planting);

        // R8 existe na soja (Fehr & Caviness) e nao no milho, cuja escala reprodutiva termina em R6.
        soybean.RecordStage(Planting.AddDays(120), "R8", null);
        Assert.Throws<DomainException>(() => corn.RecordStage(Planting.AddDays(120), "R8", null));

        // VT e pendoamento, e so o milho pendoa.
        corn.RecordStage(Planting.AddDays(60), "VT", null);
        Assert.Throws<DomainException>(() => soybean.RecordStage(Planting.AddDays(60), "VT", null));
    }

    /// <summary>
    /// Cultura sem escala transcrita continua aceitando texto livre. Inventar uma escala para o
    /// algodao so para fechar a simetria seria pior do que admitir que ela nao esta aqui.
    /// </summary>
    [Fact]
    public void ACropWithoutATranscribedScaleStillAcceptsFreeText()
    {
        var cotton = Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Cotton, "A", Planting);
        cotton.RecordStage(Planting.AddDays(30), "Primeiro botão floral", null);
        Assert.Equal("Primeiro botão floral", Assert.Single(cotton.Stages).Stage);
    }

    [Fact]
    public void TheScaleCoversEmergenceTheVegetativeRangeAndMaturity()
    {
        var corn = GrowthStageCatalog.For(Crop.Corn).Select(stage => stage.Code).ToList();

        Assert.Contains("VE", corn);
        Assert.Contains("V1", corn);
        Assert.Contains($"V{GrowthStageCatalog.MaximumVegetativeStage}", corn);
        Assert.Contains("R6", corn);
        Assert.DoesNotContain("V0", corn);
        Assert.DoesNotContain($"V{GrowthStageCatalog.MaximumVegetativeStage + 1}", corn);
        Assert.Empty(GrowthStageCatalog.For(Crop.Cotton));
        Assert.True(GrowthStageCatalog.HasScale(Crop.Soybean));
        Assert.False(GrowthStageCatalog.HasScale(Crop.Cotton));
    }

    [Fact]
    public void MissingDatesAndOversizedObservationsAreRejected()
    {
        Assert.Throws<DomainException>(() => Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Corn, "A", default));
        var cycle = Create();
        Assert.Throws<DomainException>(() => cycle.Close(default));
        Assert.Throws<DomainException>(() => cycle.RecordStage(Planting, new string('x', 33), null));
        Assert.Throws<DomainException>(() => cycle.RecordStage(Planting, "V1", new string('x', 1001)));
        Assert.Empty(cycle.Stages);
    }
}
