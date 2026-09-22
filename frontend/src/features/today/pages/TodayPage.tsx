import { Box, Button, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { InspectionSummaryViewModel } from '@/api/generated/model/inspectionSummaryViewModel';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { formatHectares } from '@/shared/format';
import { InspectionRow } from '../components/InspectionRow';
import { PendingSetup } from '../components/PendingSetup';
import { useTodayWorkspace } from '../hooks/useTodayWorkspace';

/**
 * A tela de abertura.
 *
 * Antes o sistema abria numa lista de fazendas — um indice de si mesmo, que nao responde a pergunta
 * de quem acabou de entrar. Aqui a abertura responde "o que eu faco agora", na ordem da urgencia:
 * quem esta em campo, o que venceu, o que vem, o que falta cadastrar. Grupo vazio nao aparece: uma
 * secao com "nenhum" escrito dentro e ruido com aparencia de conteudo.
 */
export function TodayPage() {
  const navigate = useNavigate();
  const workspace = useTodayWorkspace();

  return (
    <PageContainer>
      <PageHeader
        title={workspace.farm ? workspace.farm.name : 'SmartGrão'}
        description={
          workspace.farm
            ? `${workspace.fieldCount} talhão(ões) · ${formatHectares(workspace.totalHectares)} · ${workspace.openCycleCount} cultivo(s) em andamento`
            : 'Monitoramento de pragas e doenças foliares.'
        }
        action={
          workspace.farm && (
            <Button variant="outlined" onClick={() => navigate('/talhoes')}>
              Ver mapa
            </Button>
          )
        }
      />

      <QueryBoundary
        isLoading={workspace.isLoading}
        error={workspace.error}
        onRetry={workspace.refetch}
      >
        <Stack spacing={4}>
          <PendingSetup gaps={workspace.gaps} />

          <Group
            title="Em campo agora"
            hint="Visita aberta. As paradas registradas entram nela."
            inspections={workspace.inProgress}
          />

          <Group
            title="Para fazer"
            hint="Agendadas para hoje ou antes."
            inspections={workspace.due}
            relative
          />

          <Group title="Próximas" inspections={workspace.upcoming} relative />

          <Group title="Últimas visitas" inspections={workspace.recent} />

          {/* Sem nenhuma pendencia e sem nada a mostrar, a tela ficaria em branco justamente para
              quem acabou de terminar o cadastro e nao sabe qual e o proximo passo. */}
          {workspace.gaps.length === 0 && workspace.showsNothing && (
            <Box>
              <Typography variant="h6">Tudo pronto para a primeira vistoria</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Abra o talhão, marque os pontos de coleta e agende a visita.
              </Typography>
              <Button variant="contained" onClick={() => navigate('/talhoes')}>
                Escolher o talhão
              </Button>
            </Box>
          )}
        </Stack>
      </QueryBoundary>
    </PageContainer>
  );
}

function Group({ title, hint, inspections, relative = false }: {
  title: string;
  hint?: string;
  inspections: InspectionSummaryViewModel[];
  relative?: boolean;
}) {
  if (inspections.length === 0) return null;

  return (
    <Box>
      <Typography variant="h6">{title}</Typography>
      {hint && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>{hint}</Typography>
      )}
      <Stack spacing={1.5} sx={{ mt: hint ? 0 : 1.5 }}>
        {inspections.map((inspection) => (
          <InspectionRow key={inspection.id} inspection={inspection} showRelativeDay={relative} />
        ))}
      </Stack>
    </Box>
  );
}
