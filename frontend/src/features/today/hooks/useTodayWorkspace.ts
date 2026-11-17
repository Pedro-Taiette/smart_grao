import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import type { InspectionSummaryViewModel } from '@/api/generated/model/inspectionSummaryViewModel';
import { useFarmContext } from '@/features/farms/useFarmContext';
import { useFields } from '@/features/fields/hooks/useFields';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { useFarmInspections } from '@/features/inspections/hooks/useFarmInspections';
import { daysFromToday } from '@/features/inspections/inspectionLabels';
import type { SetupGap } from '../components/PendingSetup';

/**
 * O estado da propriedade hoje, ja separado em "o que fazer" e "o que falta cadastrar".
 *
 * Toda a decisao de prioridade mora aqui, e nao na tela: a pagina e so a ordem em que os grupos
 * aparecem. Sao cinco consultas da fazenda inteira — nenhuma por talhao — porque esta e a tela que
 * abre a cada acesso.
 */
export function useTodayWorkspace() {
  const { farm, farmId, isLoading: farmLoading, error: farmError } = useFarmContext();
  const fields = useFields(farmId);
  const cultivations = useFarmCultivations(farmId);
  const inspections = useFarmInspections(farmId);


  const openCycles = cultivations.cultivations.filter((cycle) => cycle.endedOn === null);
  const activeFields = fields.fields.filter((field) => field.active);
  const scheduled = (inspection: InspectionSummaryViewModel) =>
    inspection.status === InspectionStatus.Scheduled;

  // Em andamento primeiro: alguem esta em campo agora, e essa visita fica aberta ate ser fechada.
  const inProgress = inspections.inspections.filter(
    (inspection) => inspection.status === InspectionStatus.InProgress,
  );

  // Atrasadas junto com as de hoje, e nao num grupo proprio: sao a mesma acao, e separa-las esconde
  // a mais urgente das duas embaixo da outra.
  const due = inspections.inspections
    .filter((inspection) => scheduled(inspection) && daysFromToday(inspection.scheduledFor) <= 0)
    .sort((a, b) => a.scheduledFor.localeCompare(b.scheduledFor));

  const upcoming = inspections.inspections
    .filter((inspection) => scheduled(inspection) && daysFromToday(inspection.scheduledFor) > 0)
    .sort((a, b) => a.scheduledFor.localeCompare(b.scheduledFor))
    .slice(0, 3);

  // Concluida e nao realizada entram juntas. Uma visita cancelada e um fato registrado, com motivo,
  // e nao um nada: some-la daqui deixava a tela em branco para quem so tinha uma vistoria e ela nao
  // aconteceu — que e exatamente o momento em que a pessoa mais precisa ver o que houve.
  const recent = inspections.inspections
    .filter((inspection) => inspection.status === InspectionStatus.Completed
      || inspection.status === InspectionStatus.Cancelled)
    .slice(0, 3);

  // Na ordem em que precisam ser resolvidas: cada uma e pre-requisito da seguinte, e mostrar as
  // cinco de uma vez transformaria a tela de abertura numa lista de reclamacoes.
  const gaps: SetupGap[] = [];
  const isReady = !farmLoading && !fields.isLoading && !cultivations.isLoading;

  if (isReady) {
    if (!farm) {
      gaps.push({
        id: 'farm',
        title: 'Cadastre a propriedade',
        description: 'A fazenda é o ponto de partida: é ela que ancora talhões, equipe e histórico.',
        actionLabel: 'Cadastrar',
        to: '/ajustes/fazendas',
      });
    } else if (activeFields.length === 0) {
      gaps.push({
        id: 'field',
        title: 'Desenhe o primeiro talhão',
        description: 'Marque no mapa o contorno da lavoura. É ali que o sistema lê o clima e o satélite.',
        actionLabel: 'Desenhar',
        to: '/talhoes',
      });
    } else if (openCycles.length === 0) {
      gaps.push({
        id: 'cultivation',
        title: 'Registre o que foi plantado',
        description: 'Diga qual milho foi plantado e quando. É a partir daí que o sistema calcula o risco.',
        actionLabel: 'Registrar plantio',
        to: `/talhoes/${activeFields[0].id}?aba=plantio&registrar=plantio`,
      });
    }

    // A equipe deixou de ser pendencia: "Vistoriar agora" pede o nome de quem vai na hora, quando
    // ninguem esta cadastrado. Cadastrar antes era um passo sem motivo aparente.

  }

  return {
    farm,
    fieldCount: activeFields.length,
    totalHectares: activeFields.reduce((sum, field) => sum + field.areaHectares, 0),
    openCycleCount: openCycles.length,
    inProgress, due, upcoming, recent, gaps,
    // Derivado do que a tela de fato mostra, e nao de "existe alguma vistoria": um status que nao
    // caia em nenhum grupo deixaria a pagina inteira em branco, sem nem o convite para comecar.
    showsNothing: inProgress.length === 0 && due.length === 0
      && upcoming.length === 0 && recent.length === 0,
    isLoading: farmLoading || fields.isLoading || cultivations.isLoading || inspections.isLoading,
    error: farmError ?? fields.error ?? cultivations.error ?? inspections.error,
    refetch: () => {
      fields.refetch();
      cultivations.refetch();
      inspections.refetch();
    },
  };
}
