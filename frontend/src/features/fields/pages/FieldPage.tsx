import { Box, Button, Chip, Stack, Tab, Tabs, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useGetFieldById } from '@/api/generated/fields/fields';
import { useFarmContext, useSyncFarmFromRecord } from '@/features/farms/useFarmContext';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { CultivationSection } from '@/features/cultivations/components/CultivationSection';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { SamplingSection } from '@/features/sampling/components/SamplingSection';
import { FieldInspectionsSection } from '@/features/inspections/components/FieldInspectionsSection';
import { PageContainer } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { formatHectares } from '@/shared/format';
import { cropLabels } from '../cropLabels';

/**
 * A pagina de um talhao.
 *
 * Antes isto eram tres telas — contorno, cultivos, vistorias — alcancadas por botoes dentro uma da
 * outra, e nenhuma delas respondia sozinha "o que e este talhao". Sao tres perguntas sobre o mesmo
 * objeto, e agora sao tres abas de uma pagina: o cabecalho responde a identidade uma vez, e a aba
 * escolhe o assunto.
 *
 * A aba vive na URL (`?aba=`) e nao em estado: e o que permite mandar por mensagem "os pontos do
 * Talhao Sul" e o que faz o botao de voltar do navegador desfazer a troca de aba, que e o que quem
 * clicou espera.
 */
const tabs = [
  { id: 'cultivo', label: 'Cultivo' },
  { id: 'pontos', label: 'Pontos de coleta' },
  { id: 'vistorias', label: 'Vistorias' },
] as const;

export function FieldPage() {
  const { fieldId = '' } = useParams();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();

  const fieldQuery = useGetFieldById(fieldId, { query: { enabled: Boolean(fieldId) } });
  const field = fieldQuery.data;

  // Abrir por link direto corrige a fazenda selecionada, em vez de deixar o resto da navegacao
  // apontando para outra propriedade.
  useSyncFarmFromRecord(field?.farmId);
  const { farmId } = useFarmContext();

  const cultivations = useFarmCultivations(field?.farmId ?? '');
  const open = cultivations.openFor(fieldId) ?? null;

  const requested = params.get('aba');
  const tab = tabs.find((candidate) => candidate.id === requested)?.id ?? 'cultivo';

  return (
    <PageContainer>
      <Button
        startIcon={<ArrowBackIcon />}
        sx={{ mb: 1, ml: -1 }}
        onClick={() => navigate('/talhoes')}
      >
        Talhões
      </Button>

      <QueryBoundary
        isLoading={fieldQuery.isPending || cultivations.isLoading}
        error={fieldQuery.error ?? cultivations.error}
        onRetry={() => { void fieldQuery.refetch(); cultivations.refetch(); }}
      >
        {field && (
          <>
            <Box sx={{ mb: 2 }}>
              <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                <Typography variant="h5" component="h1">{field.name}</Typography>
                {!field.active && <Chip size="small" label="Fora de operação" />}
              </Stack>

              {/* A linha que situa: area, o que esta plantado e desde quando. Sem isto, a aba de
                  pontos ou a de vistorias abririam sem dizer sobre qual ciclo se esta falando. */}
              <Typography variant="body2" color="text.secondary">
                {formatHectares(field.areaHectares)}
                {open
                  ? ` · ${cropLabels[open.crop]} ${open.cultivar} · plantio em ${cycleDate(open.plantedOn)}`
                  : ' · sem cultivo em andamento'}
              </Typography>
            </Box>

            <Tabs
              value={tab}
              onChange={(_, next: string) => setParams({ aba: next }, { replace: true })}
              variant="scrollable"
              scrollButtons="auto"
              sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
            >
              {tabs.map((candidate) => (
                <Tab key={candidate.id} value={candidate.id} label={candidate.label} />
              ))}
            </Tabs>

            {tab === 'cultivo' && <CultivationSection farmId={field.farmId} fieldId={fieldId} />}
            {tab === 'pontos' && <SamplingSection field={field} cultivation={open} />}
            {tab === 'vistorias' && (
              <FieldInspectionsSection farmId={farmId} fieldId={fieldId} cultivation={open} />
            )}
          </>
        )}
      </QueryBoundary>
    </PageContainer>
  );
}
