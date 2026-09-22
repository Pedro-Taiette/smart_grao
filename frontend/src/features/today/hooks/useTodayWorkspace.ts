import { useGetProtocols } from '@/api/generated/protocols/protocols';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import type { InspectionSummaryViewModel } from '@/api/generated/model/inspectionSummaryViewModel';
import { useFarmContext } from '@/features/farms/useFarmContext';
import { useFields } from '@/features/fields/hooks/useFields';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { useFarmInspections } from '@/features/inspections/hooks/useFarmInspections';
import { usePeople } from '@/features/inspections/hooks/usePeople';
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
  const team = usePeople(farmId, true);

  // O protocolo precisa existir publicado na cultura do que esta plantado. Sem cultivo aberto a
  // pergunta nao se coloca, e o milho e so o palpite inicial para nao deixar a consulta sem filtro.
  const openCycles = cultivations.cultivations.filter((cycle) => cycle.endedOn === null);
  const protocols = useGetProtocols(
    { crop: openCycles[0]?.crop ?? 'Corn', status: ProtocolStatus.Published },
    { query: { enabled: Boolean(farmId) } },
  );

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
  const isReady = !farmLoading && !fields.isLoading && !cultivations.isLoading && !team.isLoading;

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
        description: 'O contorno no mapa define a área e, depois, onde ficam os pontos de coleta.',
        actionLabel: 'Desenhar',
        to: '/talhoes',
      });
    } else if (openCycles.length === 0) {
      gaps.push({
        id: 'cultivation',
        title: 'Nenhum talhão com cultivo em andamento',
        description: 'O cultivo diz o que está plantado e desde quando — sem ele não há vistoria a agendar.',
        actionLabel: 'Registrar cultivo',
        to: `/talhoes/${activeFields[0].id}?aba=cultivo`,
      });
    }

    if (farm && team.people.length === 0) {
      gaps.push({
        id: 'team',
        title: 'Ninguém cadastrado na equipe',
        description: 'Toda vistoria tem um responsável. Cadastre quem vai a campo.',
        actionLabel: 'Cadastrar pessoa',
        to: '/ajustes/equipe',
      });
    }

    if (farm && openCycles.length > 0 && !protocols.isPending && (protocols.data ?? []).length === 0) {
      gaps.push({
        id: 'protocol',
        title: 'Nenhum protocolo publicado para esta cultura',
        description: 'O protocolo é a receita da vistoria: o que observar em cada parada e em que unidade.',
        actionLabel: 'Ver protocolos',
        to: '/ajustes/protocolos',
      });
    }
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
