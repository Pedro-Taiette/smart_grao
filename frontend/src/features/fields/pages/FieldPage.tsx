import { Box, Button, Chip, Stack, Tab, Tabs, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useGetFieldById } from '@/api/generated/fields/fields';
import { useFarmContext, useSyncFarmFromRecord } from '@/features/farms/useFarmContext';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { CultivationSection } from '@/features/cultivations/components/CultivationSection';
import { RiskSection } from '@/features/risk/components/RiskSection';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
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
 *
 * A ordem e a da pergunta de quem abre o talhao: "esta tudo bem aqui?" (Situacao), "quando alguem
 * olhou?" (Vistorias) e, por ultimo, o cadastro do plantio. Talhao que nunca teve plantio abre direto
 * no Plantio, porque e a unica coisa que da para fazer nele.
 */
const tabs = [
  { id: 'situacao', label: 'Situação' },
  { id: 'vistorias', label: 'Vistorias' },
  { id: 'plantio', label: 'Plantio' },
] as const;

type TabId = typeof tabs[number]['id'];

/** Nomes antigos das abas, que ainda circulam em links salvos. */
const tabAliases: Record<string, TabId> = { risco: 'situacao', cultivo: 'plantio' };

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

  const raw = params.get('aba') ?? '';
  const requested = tabAliases[raw] ?? raw;
  const hasHistory = cultivations.cultivations.some((cycle) => cycle.fieldId === fieldId);
  const tab: TabId = tabs.find((candidate) => candidate.id === requested)?.id
    ?? (hasHistory ? 'situacao' : 'plantio');

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
                  : ' · nada plantado'}
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

            {tab === 'plantio' && <CultivationSection farmId={field.farmId} fieldId={fieldId} />}
            {tab === 'situacao' && <RiskSection farmId={field.farmId} fieldId={fieldId} />}
            {tab === 'vistorias' && (
              <FieldInspectionsSection farmId={farmId} cultivation={open} />
            )}
          </>
        )}
      </QueryBoundary>
    </PageContainer>
  );
}
