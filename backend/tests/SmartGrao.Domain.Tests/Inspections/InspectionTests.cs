using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;
using Xunit;

namespace SmartGrao.Domain.Tests.Inspections;

/// <summary>
/// O critério da fase 3: uma vistoria pode ser executada, concluída e repetida, preservando cada
/// visita. É o que estes testes tentam quebrar.
/// </summary>
public sealed class InspectionTests
{
    private static readonly DateOnly Planting = new(2026, 1, 10);
    private static readonly DateTimeOffset Morning = new(2026, 2, 1, 8, 0, 0, TimeSpan.FromHours(-3));
    private static readonly GeoCoordinate Somewhere = GeoCoordinate.From(-15.6, -56.1);

    private static Cultivation CornCycle() =>
        Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Corn, "AG 8700", Planting);

    private static SamplingPlan PlanFor(Cultivation cultivation) =>
        SamplingPlan.ForMapping(cultivation.FieldId, SquareOfArea(20d), SamplingSpacing.Standard, cultivation.Id);

    private static MonitoringTarget Armyworm() => MonitoringTarget.Create(
        "spodoptera_frugiperda", "Lagarta-do-cartucho", "Spodoptera frugiperda", TargetKind.Pest, Crop.Corn);

    private static MonitoringTarget Leafhopper() => MonitoringTarget.Create(
        "dalbulus_maidis", "Cigarrinha-do-milho", "Dalbulus maidis", TargetKind.Pest, Crop.Corn);

    private static Protocol CornProtocol(params MonitoringTarget[] extras)
    {
        var protocol = Protocol.CreateDraft("milho_padrao", Crop.Corn, "Milho");
        protocol.AddItem(Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "Cartucho.");
        foreach (var target in extras)
            protocol.AddItem(target, PlantOrgan.Whorl, CountUnit.Presence, 0, "Conferir no cartucho.");
        protocol.Publish();
        return protocol;
    }

    private static Person Scout(FarmId? farmId = null) =>
        Person.Create(farmId ?? FarmId.New(), "Ana", PersonRole.Technician);

    private static Inspection Scheduled(out Cultivation cultivation, out SamplingPlan plan, out Protocol protocol)
    {
        cultivation = CornCycle();
        plan = PlanFor(cultivation);
        protocol = CornProtocol();
        return Inspection.Schedule(cultivation, plan, protocol, Scout(), Planting.AddDays(30));
    }

    private static Inspection Started(out SamplingPlan plan, out Protocol protocol)
    {
        var inspection = Scheduled(out _, out plan, out protocol);
        inspection.Start(Morning);
        return inspection;
    }

    private static IReadOnlyList<ObservationCount> Counts(Protocol protocol, decimal value) =>
        [new ObservationCount(protocol.Items[0], value, value > 0m)];

    // ── O critério: executar, concluir, repetir ───────────────────────────────

    [Fact]
    public void AnInspectionIsExecutedCompletedAndRepeatedOverTheSamePlan()
    {
        var cultivation = CornCycle();
        var plan = PlanFor(cultivation);
        var protocol = CornProtocol();
        var scout = Scout();

        var first = Inspection.Schedule(cultivation, plan, protocol, scout, Planting.AddDays(30));
        first.Start(Morning);
        first.RecordObservation(plan.Points[0], Morning.AddMinutes(10), Somewhere, 5d, "V6", null, Counts(protocol, 12m));
        first.Complete(Morning.AddHours(2));

        var second = Inspection.Schedule(cultivation, plan, protocol, scout, Planting.AddDays(37));
        second.Start(Morning.AddDays(7));
        second.RecordObservation(plan.Points[0], Morning.AddDays(7), Somewhere, 5d, "V8", null, Counts(protocol, 25m));
        second.Complete(Morning.AddDays(7).AddHours(2));

        // A segunda visita nao reescreveu a primeira: o mesmo ponto guarda duas leituras distintas.
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(12m, Assert.Single(Assert.Single(first.Observations).Counts).Value);
        Assert.Equal(25m, Assert.Single(Assert.Single(second.Observations).Counts).Value);
        Assert.Equal("V6", Assert.Single(first.Observations).GrowthStage);
        Assert.Equal(InspectionStatus.Completed, first.Status);
    }

    // ── Agendamento ───────────────────────────────────────────────────────────

    [Fact]
    public void ADraftProtocolNeverGoesToTheField()
    {
        var cultivation = CornCycle();
        var draft = Protocol.CreateDraft("milho_padrao", Crop.Corn, "Milho");
        draft.AddItem(Armyworm(), PlantOrgan.Whorl, CountUnit.AttackedPlantPercentage, 1, "Cartucho.");

        var error = Assert.Throws<DomainException>(() => Inspection.Schedule(
            cultivation, PlanFor(cultivation), draft, Scout(), Planting.AddDays(30)));

        Assert.Equal("inspection.protocol_not_published", error.Error.Code);
    }

    [Fact]
    public void AProtocolOfAnotherCropIsRejected()
    {
        var soybean = Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Soybean, "B", Planting);

        var error = Assert.Throws<DomainException>(() => Inspection.Schedule(
            soybean, PlanFor(soybean), CornProtocol(), Scout(), Planting.AddDays(30)));

        Assert.Equal("inspection.protocol_from_another_crop", error.Error.Code);
    }

    [Fact]
    public void APlanFromAnotherCultivationIsRejected()
    {
        var cultivation = CornCycle();
        var other = CornCycle();

        var error = Assert.Throws<DomainException>(() => Inspection.Schedule(
            cultivation, PlanFor(other), CornProtocol(), Scout(), Planting.AddDays(30)));

        Assert.Equal("inspection.plan_from_another_cultivation", error.Error.Code);
    }

    [Fact]
    public void AClosedCycleAndAnInactivePersonAreRejected()
    {
        var closed = CornCycle();
        var plan = PlanFor(closed);
        var protocol = CornProtocol();
        closed.Close(Planting.AddDays(100));
        Assert.Throws<DomainException>(() => Inspection.Schedule(closed, plan, protocol, Scout(), Planting.AddDays(30)));

        var cultivation = CornCycle();
        var former = Scout();
        former.Deactivate();
        var error = Assert.Throws<DomainException>(() => Inspection.Schedule(
            cultivation, PlanFor(cultivation), CornProtocol(), former, Planting.AddDays(30)));
        Assert.Equal("inspection.person_inactive", error.Error.Code);
    }

    // ── Situação ──────────────────────────────────────────────────────────────

    [Fact]
    public void ObservationsOnlyLandWhileTheInspectionIsInProgress()
    {
        var inspection = Scheduled(out _, out var plan, out var protocol);

        var error = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m)));
        Assert.Equal("inspection.not_in_progress", error.Error.Code);

        inspection.Start(Morning);
        inspection.RecordObservation(plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m));
        inspection.Complete(Morning.AddHours(1));

        Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[1], Morning.AddHours(2), Somewhere, 5d, null, null, Counts(protocol, 1m)));
    }

    /// <summary>
    /// Vistoria sem nenhuma parada nao e vistoria concluida — e vistoria que nao aconteceu, e para
    /// isso existe o cancelamento com motivo.
    /// </summary>
    [Fact]
    public void AnEmptyInspectionIsCancelledWithAReasonAndNotCompleted()
    {
        var inspection = Started(out _, out _);

        var error = Assert.Throws<DomainException>(() => inspection.Complete(Morning.AddHours(1)));
        Assert.Equal("inspection.no_observations", error.Error.Code);

        Assert.Throws<DomainException>(() => inspection.Cancel("   "));
        inspection.Cancel("Chuva forte; não deu para entrar no talhão.");
        Assert.Equal(InspectionStatus.Cancelled, inspection.Status);
        Assert.Equal("Chuva forte; não deu para entrar no talhão.", inspection.CancellationReason);
    }

    [Fact]
    public void ACompletedInspectionIsNotCancelledAndClosureIsIdempotent()
    {
        var inspection = Started(out var plan, out var protocol);
        inspection.RecordObservation(plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m));

        inspection.Complete(Morning.AddHours(1));
        var touched = inspection.UpdatedAt;
        inspection.Complete(Morning.AddHours(3));

        Assert.Equal(touched, inspection.UpdatedAt);
        Assert.Throws<DomainException>(() => inspection.Cancel("mudei de ideia"));
    }

    [Fact]
    public void CompletingBeforeTheStartIsRejected()
    {
        var inspection = Started(out var plan, out var protocol);
        inspection.RecordObservation(plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m));

        var error = Assert.Throws<DomainException>(() => inspection.Complete(Morning.AddHours(-1)));
        Assert.Equal("inspection.invalid_timing", error.Error.Code);
    }

    [Fact]
    public void ReschedulingStopsOnceSomeoneIsInTheField()
    {
        var inspection = Scheduled(out _, out _, out _);
        var other = Scout();

        inspection.Reschedule(other, Planting.AddDays(35));
        Assert.Equal(other.Id, inspection.ResponsibleId);
        Assert.Equal(Planting.AddDays(35), inspection.ScheduledFor);

        inspection.Start(Morning);
        Assert.Throws<DomainException>(() => inspection.Reschedule(other, Planting.AddDays(40)));
    }

    // ── Observações ───────────────────────────────────────────────────────────

    /// <summary>
    /// Quem ve uma reboleira a caminho do proximo ponto precisa registra-la onde ela esta, e nao no
    /// ponto planejado mais proximo.
    /// </summary>
    [Fact]
    public void AnOccurrenceOutsideThePlannedPointsIsRecordedWhereItIs()
    {
        var inspection = Started(out var plan, out var protocol);

        inspection.RecordObservation(plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m));
        var offPlan = inspection.RecordObservation(
            null, Morning.AddMinutes(20), GeoCoordinate.From(-15.61, -56.11), 8d, null,
            "Reboleira a caminho do próximo ponto.", Counts(protocol, 40m));

        Assert.True(offPlan.IsOffPlan);
        Assert.Null(offPlan.SamplingPointId);
        Assert.Equal(1, inspection.VisitedPointCount);
        Assert.Equal(1, inspection.OffPlanObservationCount);
    }

    [Fact]
    public void APointIsRecordedOnceAndOnlyFromThisInspectionsPlan()
    {
        var inspection = Started(out var plan, out var protocol);
        var otherPlan = SamplingPlan.ForMapping(FieldId.New(), SquareOfArea(20d), SamplingSpacing.Standard, CultivationId.New());

        inspection.RecordObservation(plan.Points[0], Morning, Somewhere, 5d, null, null, Counts(protocol, 1m));

        var duplicate = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[0], Morning.AddMinutes(5), Somewhere, 5d, null, null, Counts(protocol, 2m)));
        Assert.Equal("inspection.duplicate_point_observation", duplicate.Error.Code);

        var foreign = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            otherPlan.Points[0], Morning.AddMinutes(5), Somewhere, 5d, null, null, Counts(protocol, 2m)));
        Assert.Equal("inspection.point_from_another_plan", foreign.Error.Code);
    }

    /// <summary>
    /// A precisao ruim e registrada, nao recusada: bloquear faria a pessoa em campo perder o
    /// registro por causa do aparelho. Quem for usar o dado e que decide.
    /// </summary>
    [Fact]
    public void PoorAccuracyIsFlaggedButNeverBlocksTheRecord()
    {
        var inspection = Started(out var plan, out var protocol);

        var sloppy = inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 45d, null, null, Counts(protocol, 1m));
        var fine = inspection.RecordObservation(
            plan.Points[1], Morning.AddMinutes(5), Somewhere, 4d, null, null, Counts(protocol, 1m));
        var unknown = inspection.RecordObservation(
            plan.Points[2], Morning.AddMinutes(10), Somewhere, null, null, null, Counts(protocol, 1m));

        Assert.True(sloppy.HasPoorAccuracy);
        Assert.False(fine.HasPoorAccuracy);
        Assert.False(unknown.HasPoorAccuracy);
        Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[3], Morning.AddMinutes(15), Somewhere, -1d, null, null, Counts(protocol, 1m)));
    }

    [Fact]
    public void TheStageOfAnObservationFollowsTheScaleOfTheCrop()
    {
        var inspection = Started(out var plan, out var protocol);

        var observation = inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, "v6", null, Counts(protocol, 1m));
        Assert.Equal("V6", observation.GrowthStage);

        var error = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[1], Morning, Somewhere, 5d, "seis folhas", null, Counts(protocol, 1m)));
        Assert.Equal("cultivation.stage_not_in_scale", error.Error.Code);
    }

    // ── Contagens ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A regra do roadmap: um resultado negativo precisa indicar quais alvos foram avaliados, e
    /// nunca pode significar "a planta esta saudavel". A linha com zero e justamente esse registro.
    /// </summary>
    [Fact]
    public void EvaluatingATargetAndFindingNothingIsRecordedAndIsNotSilence()
    {
        var cultivation = CornCycle();
        var protocol = CornProtocol(Leafhopper());
        var inspection = Inspection.Schedule(cultivation, PlanFor(cultivation), protocol, Scout(), Planting.AddDays(30));
        inspection.Start(Morning);
        var plan = PlanFor(cultivation);

        var observation = inspection.RecordObservation(
            null, Morning, Somewhere, 5d, null, null,
            [new ObservationCount(protocol.Items[0], 0m, false)]);

        // O alvo avaliado tem linha, com zero. O que nao foi olhado nao tem — e essa ausencia e visivel.
        var recorded = Assert.Single(observation.Counts);
        Assert.Equal(0m, recorded.Value);
        Assert.False(recorded.Detected);
        Assert.Empty(observation.Detections);
        Assert.Equal(2, protocol.Items.Count);
        Assert.Single(observation.Counts);
        Assert.NotNull(plan);
    }

    [Fact]
    public void TheNumberDecidesWhetherTheTargetWasDetected()
    {
        var inspection = Started(out var plan, out var protocol);

        // "3 lagartas, nao detectado" e uma contradicao que nao deve chegar ao banco.
        var observation = inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, null, null,
            [new ObservationCount(protocol.Items[0], 3m, false)]);

        Assert.True(Assert.Single(observation.Counts).Detected);
    }

    [Fact]
    public void APresenceRecordCarriesNoNumberAndACountingUnitDemandsOne()
    {
        var cultivation = CornCycle();
        var protocol = CornProtocol(Leafhopper());
        var inspection = Inspection.Schedule(cultivation, PlanFor(cultivation), protocol, Scout(), Planting.AddDays(30));
        inspection.Start(Morning);

        var presence = protocol.Items.Single(item => item.Unit == CountUnit.Presence);
        var counting = protocol.Items.Single(item => item.Unit == CountUnit.AttackedPlantPercentage);

        var withValue = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            null, Morning, Somewhere, 5d, null, null, [new ObservationCount(presence, 1m, true)]));
        Assert.Equal("inspection.presence_takes_no_value", withValue.Error.Code);

        var withoutValue = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            null, Morning, Somewhere, 5d, null, null, [new ObservationCount(counting, null, true)]));
        Assert.Equal("inspection.count_value_required", withoutValue.Error.Code);

        var observation = inspection.RecordObservation(
            null, Morning, Somewhere, 5d, null, null, [new ObservationCount(presence, null, true)]);
        Assert.Null(Assert.Single(observation.Counts).Value);
        Assert.True(Assert.Single(observation.Counts).Detected);
    }

    [Fact]
    public void AValueOutsideTheRangeOfItsUnitIsRejected()
    {
        var inspection = Started(out var plan, out var protocol);

        var error = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, null, null,
            [new ObservationCount(protocol.Items[0], 140m, true)]));

        Assert.Equal("inspection.count_value_out_of_range", error.Error.Code);
    }

    [Fact]
    public void ATargetFromAnotherProtocolVersionIsRejected()
    {
        var inspection = Started(out var plan, out _);
        var otherProtocol = CornProtocol();

        var error = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, null, null,
            [new ObservationCount(otherProtocol.Items[0], 1m, true)]));

        Assert.Equal("inspection.count_from_another_protocol", error.Error.Code);
    }

    [Fact]
    public void TheSameTargetIsNotCountedTwiceInOneObservation()
    {
        var inspection = Started(out var plan, out var protocol);

        var error = Assert.Throws<DomainException>(() => inspection.RecordObservation(
            plan.Points[0], Morning, Somewhere, 5d, null, null,
            [new ObservationCount(protocol.Items[0], 1m, true), new ObservationCount(protocol.Items[0], 2m, true)]));

        Assert.Equal("inspection.duplicate_count", error.Error.Code);
    }

    private static Boundary SquareOfArea(double hectares)
    {
        const double Latitude = -15.6;
        const double Longitude = -56.1;

        var side = Math.Sqrt(hectares * Wgs84Geodesy.SquareMetersPerHectare);
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(side);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(side, Latitude);

        return Boundary.FromCoordinates(
        [
            GeoCoordinate.From(Latitude, Longitude),
            GeoCoordinate.From(Latitude, Longitude + deltaLongitude),
            GeoCoordinate.From(Latitude + deltaLatitude, Longitude + deltaLongitude),
            GeoCoordinate.From(Latitude + deltaLatitude, Longitude),
        ]);
    }
}
