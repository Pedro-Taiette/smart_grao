using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;
using Xunit;

namespace SmartGrao.Domain.Tests.Protocols;

public sealed class ProtocolTests
{
    private static MonitoringTarget Armyworm() => MonitoringTarget.Create(
        "spodoptera_frugiperda", "Lagarta-do-cartucho", "Spodoptera frugiperda", TargetKind.Pest, Crop.Corn);

    private static MonitoringTarget Cercospora() => MonitoringTarget.Create(
        "cercospora_zeina", "Cercosporiose", "Cercospora zeina", TargetKind.FoliarDisease, Crop.Corn);

    private static MonitoringTarget Leafhopper() => MonitoringTarget.Create(
        "dalbulus_maidis", "Cigarrinha-do-milho", "Dalbulus maidis", TargetKind.Pest, Crop.Corn);

    private static MonitoringTarget AsianRust() => MonitoringTarget.Create(
        "phakopsora_pachyrhizi", "Ferrugem asiatica", "Phakopsora pachyrhizi",
        TargetKind.FoliarDisease, Crop.Soybean);

    private static Protocol CornDraft() => Protocol.CreateDraft("milho_padrao", Crop.Corn, "Milho — padrao");

    private static Protocol PublishedCornProtocol()
    {
        var protocol = CornDraft();
        protocol.AddItem(Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 2,
            "Avaliar plantas ao acaso e contar as raspadas ou perfuradas.",
            ReferenceLevel.Create(20m, "Embrapa, MIP na cultura do milho"));
        protocol.Publish();
        return protocol;
    }

    // ── O criterio da fase 2 ──────────────────────────────────────────────────

    [Fact]
    public void ACornProtocolRefusesASoybeanTarget()
    {
        var protocol = CornDraft();

        var error = Assert.Throws<DomainException>(() => protocol.AddItem(
            AsianRust(), PlantOrgan.Leaf, CountUnit.LesionedLeafAreaPercentage, 1, "Avaliar a folha."));

        Assert.Equal("protocol.target_from_another_crop", error.Error.Code);
        Assert.Empty(protocol.Items);
    }

    // ── Versionamento ─────────────────────────────────────────────────────────

    [Fact]
    public void APublishedProtocolIsImmutable()
    {
        var published = PublishedCornProtocol();

        Assert.Throws<DomainException>(() => published.AddItem(
            Cercospora(), PlantOrgan.EarLeaf, CountUnit.LesionedLeafAreaPercentage, 1, "Avaliar a folha da espiga."));
        Assert.Throws<DomainException>(() => published.RemoveItem(published.Items[0].Id));
        Assert.Throws<DomainException>(() => published.Rename("Outro nome"));
    }

    [Fact]
    public void TheNextVersionCopiesTheTargetsWithoutTouchingThePublishedOne()
    {
        var published = PublishedCornProtocol();

        var next = published.CreateNextVersion();
        next.AddItem(Cercospora(), PlantOrgan.EarLeaf, CountUnit.LesionedLeafAreaPercentage, 1,
            "Avaliar a folha da espiga pela escala diagramatica.");

        Assert.Equal(published.Code, next.Code);
        Assert.Equal(2, next.Version);
        Assert.Equal(ProtocolStatus.Draft, next.Status);
        Assert.Equal(2, next.Items.Count);
        Assert.Single(published.Items);
        Assert.Equal(ProtocolStatus.Published, published.Status);
        Assert.NotEqual(published.Items[0].Id, next.Items[0].Id);
        Assert.Equal(published.Items[0].TargetId, next.Items[0].TargetId);
        Assert.Equal(next.Id, next.Items[0].ProtocolId);
    }

    /// <summary>
    /// O nivel de referencia e um objeto <i>owned</i>: a sua chave e o item que o possui. Se as duas
    /// versoes apontassem para a mesma instancia, gravar a v2 seria entendido como o nivel da v1
    /// trocando de dono, e a persistencia recusaria a operacao inteira.
    /// </summary>
    [Fact]
    public void TheNextVersionCopiesTheReferenceLevelByValueAndNotByReference()
    {
        var published = PublishedCornProtocol();

        var copied = published.CreateNextVersion().Items[0].ReferenceLevel;
        var original = published.Items[0].ReferenceLevel;

        Assert.NotNull(copied);
        Assert.NotNull(original);
        Assert.NotSame(original, copied);
        Assert.Equal(original.Threshold, copied.Threshold);
        Assert.Equal(original.Source, copied.Source);
    }

    [Fact]
    public void ADraftDoesNotBranchIntoASecondUnusedDraft()
    {
        Assert.Throws<DomainException>(() => CornDraft().CreateNextVersion());
    }

    [Fact]
    public void AnEmptyProtocolIsNotPublished()
    {
        var error = Assert.Throws<DomainException>(() => CornDraft().Publish());
        Assert.Equal("protocol.empty", error.Error.Code);
    }

    [Fact]
    public void PublishingAndRetiringAreIdempotentAndRetirementIsTerminal()
    {
        var protocol = PublishedCornProtocol();
        protocol.Publish();
        Assert.Equal(ProtocolStatus.Published, protocol.Status);

        protocol.Retire();
        protocol.Retire();
        Assert.Equal(ProtocolStatus.Retired, protocol.Status);
        Assert.Throws<DomainException>(() => protocol.CreateNextVersion());
        Assert.Throws<DomainException>(() => protocol.Publish());
    }

    // ── Unidade, orgao e nivel de referencia ──────────────────────────────────

    [Fact]
    public void ATrapCountCarriesNoPlantOrgan()
    {
        var protocol = CornDraft();
        var leafhopper = Leafhopper();

        Assert.Throws<DomainException>(() => protocol.AddItem(
            leafhopper, PlantOrgan.Leaf, CountUnit.InsectsPerTrap, 0, "Contar as capturas na armadilha."));

        var item = protocol.AddItem(leafhopper, null, CountUnit.InsectsPerTrap, 0,
            "Armadilha adesiva amarela na bordadura, trocada semanalmente.");
        Assert.Null(item.Organ);
    }

    [Fact]
    public void EveryOtherUnitNeedsTheOrganBeingObserved()
    {
        var protocol = CornDraft();

        var error = Assert.Throws<DomainException>(() => protocol.AddItem(
            Cercospora(), null, CountUnit.LesionedLeafAreaPercentage, 1, "Avaliar a folha."));

        Assert.Equal("protocol.organ_required", error.Error.Code);
    }

    /// <summary>
    /// A cigarrinha e vetor de enfezamento e nao tem nivel de controle estabelecido: a presenca ja
    /// justifica acao. Anexar um limiar a ela inventaria um numero que a literatura nao da.
    /// </summary>
    [Fact]
    public void APresenceRecordTakesNoReferenceLevel()
    {
        var protocol = CornDraft();

        var error = Assert.Throws<DomainException>(() => protocol.AddItem(
            Leafhopper(), PlantOrgan.Whorl, CountUnit.Presence, 1, "Conferir adultos no cartucho.",
            ReferenceLevel.Create(1m, "inventado")));

        Assert.Equal("protocol.reference_level_not_applicable", error.Error.Code);
    }

    [Fact]
    public void AReferenceLevelWithoutASourceIsRejected()
    {
        Assert.Throws<DomainException>(() => ReferenceLevel.Create(20m, "   "));
        Assert.Throws<DomainException>(() => ReferenceLevel.Create(20m, new string('x', 301)));
    }

    [Theory]
    [InlineData(CountUnit.AttackedPlantPercentage, 101)]
    [InlineData(CountUnit.LesionedLeafAreaPercentage, 120)]
    [InlineData(CountUnit.SeverityScore1To9, 10)]
    [InlineData(CountUnit.InsectsPerPlant, 0)]
    public void AThresholdOutsideTheRangeOfItsUnitIsRejected(CountUnit unit, decimal threshold)
    {
        var protocol = CornDraft();

        Assert.Throws<DomainException>(() => protocol.AddItem(
            Armyworm(), PlantOrgan.Leaf, unit, 1, "Instrucao.",
            ReferenceLevel.Create(threshold, "fonte")));
    }

    [Fact]
    public void AnOptionalReferenceLevelKeepsUnsourcedTargetsInTheProtocol()
    {
        var protocol = CornDraft();

        var item = protocol.AddItem(Cercospora(), PlantOrgan.EarLeaf,
            CountUnit.LesionedLeafAreaPercentage, 2,
            "Severidade na folha da espiga; nao ha nivel de dano economico de uso corrente.");

        Assert.Null(item.ReferenceLevel);
        protocol.Publish();
        Assert.Equal(ProtocolStatus.Published, protocol.Status);
    }

    // ── Itens ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TheSameTargetIsNotObservedTwiceOnTheSameOrgan()
    {
        var protocol = CornDraft();
        var armyworm = Armyworm();
        protocol.AddItem(armyworm, PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "Cartucho.");

        Assert.Throws<DomainException>(() => protocol.AddItem(
            armyworm, PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "Cartucho de novo."));

        protocol.AddItem(armyworm, PlantOrgan.Ear, CountUnit.AttackedPlantPercentage, 1, "Espiga.");
        Assert.Equal(2, protocol.Items.Count);
    }

    [Fact]
    public void ItemsAreRemovedWhileTheProtocolIsADraft()
    {
        var protocol = CornDraft();
        var item = protocol.AddItem(Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "Cartucho.");

        protocol.RemoveItem(item.Id);

        Assert.Empty(protocol.Items);
        Assert.Throws<DomainException>(() => protocol.RemoveItem(item.Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void APhotoCountOutsideTheAllowedRangeIsRejected(int photos)
    {
        var protocol = CornDraft();

        Assert.Throws<DomainException>(() => protocol.AddItem(
            Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, photos, "Cartucho."));
    }

    [Fact]
    public void InstructionsAreRequiredAndLengthLimited()
    {
        var protocol = CornDraft();

        Assert.Throws<DomainException>(() => protocol.AddItem(
            Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "   "));
        Assert.Throws<DomainException>(() => protocol.AddItem(
            Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, new string('x', 2001)));
    }

    [Fact]
    public void AnUndefinedUnitIsRejected()
    {
        var protocol = CornDraft();

        Assert.Throws<DomainException>(() => protocol.AddItem(
            Armyworm(), PlantOrgan.Whorl, CountUnit.Undefined, 1, "Cartucho."));
    }

    [Theory]
    [InlineData("Milho Padrao")]
    [InlineData("milho-padrao")]
    [InlineData("")]
    public void AnUnstableProtocolCodeIsRejected(string code)
    {
        Assert.Throws<DomainException>(() => Protocol.CreateDraft(code, Crop.Corn, "Milho"));
    }
}
