import { Alert, Box, Button, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useGetCalibration } from '@/api/generated/analysis/analysis';
import type { ModelSummaryViewModel } from '@/api/generated/model/modelSummaryViewModel';
import { apiBaseUrl } from '@/api/httpClient';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { leafLabelName, percent } from '../analysisLabels';

const classNames: Record<string, string> = {
  cercosporiose: 'Cercosporiose',
  saudavel: 'Folha sadia',
  outras_manchas: 'Outra mancha',
};

const domainNames: Record<string, string> = { campo: 'Fotos de campo', lab: 'Fotos de laboratório' };

function Stat({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Box sx={{ minWidth: 150 }}>
      <Typography variant="h4">{value}</Typography>
      <Typography variant="body2">{label}</Typography>
      {hint && <Typography variant="caption" color="text.secondary">{hint}</Typography>}
    </Box>
  );
}

function Confusion({ labels, matrix }: { labels: string[]; matrix: number[][] }) {
  return (
    <Table size="small" sx={{ maxWidth: 560 }}>
      <TableHead>
        <TableRow>
          <TableCell>Era ↓ · modelo disse →</TableCell>
          {labels.map(label => <TableCell key={label} align="right">{label}</TableCell>)}
        </TableRow>
      </TableHead>
      <TableBody>
        {matrix.map((row, i) => (
          <TableRow key={labels[i]}>
            <TableCell component="th">{labels[i]}</TableCell>
            {row.map((value, j) => (
              <TableCell key={j} align="right" sx={{ fontWeight: i === j ? 700 : 400 }}>{value}</TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function Breakdown({ title, rows, names }: { title: string; rows: ModelSummaryViewModel[]; names?: Record<string, string> }) {
  return (
    <Box>
      <Typography variant="subtitle2" sx={{ mb: 0.5 }}>{title}</Typography>
      <Table size="small" sx={{ maxWidth: 640 }}>
        <TableHead>
          <TableRow>
            <TableCell />
            <TableCell align="right">Fotos</TableCell>
            <TableCell align="right">Acerta cercosporiose</TableCell>
            <TableCell align="right">Quando diz cercosporiose, é</TableCell>
            <TableCell align="right">F1 macro</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map(row => (
            <TableRow key={row.name}>
              <TableCell component="th">{names?.[row.name ?? ''] ?? row.name}</TableCell>
              <TableCell align="right">{row.n}</TableCell>
              <TableCell align="right">{percent(row.recallCercosporiose)}</TableCell>
              <TableCell align="right">{percent(row.precisionCercosporiose)}</TableCell>
              <TableCell align="right">{row.macroF1?.toFixed(2) ?? '—'}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}

/** O sistema medindo o quanto ele próprio acerta. Cada número diz de onde veio. */
export function CalibrationPage() {
  const query = useGetCalibration();
  const data = query.data;
  const model = data?.model;
  const reviews = data?.reviews;

  return (
    <PageContainer>
      <PageHeader
        title="Aferimento"
        description="O quanto o sistema acerta: o modelo de imagem num conjunto de teste, o modelo contra as revisões feitas aqui, e quantos dias o aviso chegou antes da confirmação na folha."
        action={<Button variant="outlined" href={`${apiBaseUrl}/api/calibration/reviewed-photos.csv`}>Exportar fotos revisadas</Button>}
      />
      <QueryBoundary isLoading={query.isPending} error={query.error} onRetry={() => void query.refetch()}>
        {data && (
          <Stack spacing={3}>
            <Paper variant="outlined" sx={{ p: 2.5 }}>
              <Stack spacing={2}>
                <Box>
                  <Typography variant="h6">Modelo de imagem no conjunto de teste</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Fotos públicas (EUA, África do Sul, Bangladesh) que o modelo não viu no treino, separadas por planta e
                    sessão de captura. Não é campo brasileiro: é a referência até haver revisões daqui.
                  </Typography>
                </Box>
                {!model && <Alert severity="warning">Serviço de análise fora do ar: {data.modelUnavailableReason}</Alert>}
                {model && <>
                  <Stack direction="row" useFlexGap spacing={4} sx={{ flexWrap: 'wrap' }}>
                    <Stat label="das folhas com cercosporiose são apontadas" value={percent(model.test.recallCercosporiose)}
                      hint="Sensibilidade: o erro caro é deixar passar." />
                    <Stat label="dos alertas de cercosporiose estão certos" value={percent(model.test.precisionCercosporiose)} hint="Precisão" />
                    <Stat label="F1 macro (as três classes)" value={model.test.macroF1?.toFixed(2) ?? '—'} />
                    <Stat label="erro de calibração (ECE)" value={model.test.ece?.toFixed(3) ?? '—'} hint="Quanto a confiança dita difere do acerto real" />
                  </Stack>
                  <Typography variant="body2" color="text.secondary">
                    Modelo {model.modelVersion} · {model.test.n} fotos de teste
                  </Typography>
                  <Confusion labels={model.confusionLabels.map(label => classNames[label] ?? label)} matrix={model.confusionMatrix} />
                  <Breakdown title="Campo e laboratório" rows={model.byDomain} names={domainNames} />
                  <Breakdown title="Por conjunto de origem" rows={model.byDataset} />
                </>}
              </Stack>
            </Paper>

            {reviews && (
              <Paper variant="outlined" sx={{ p: 2.5 }}>
                <Stack spacing={2}>
                  <Typography variant="h6">Modelo contra as revisões</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {reviews.photos} foto(s) · {reviews.analyzed} analisada(s) · {reviews.reviewed} revisada(s) por alguém da equipe
                    {reviews.simulatedExcluded > 0 ? ` · ${reviews.simulatedExcluded} do processador simulado fora da conta` : ''}
                  </Typography>
                  {reviews.reviewed === 0 ? (
                    <Alert severity="info">Nenhuma revisão ainda. Abra uma foto na vistoria e diga o que ela mostra.</Alert>
                  ) : <>
                    <Stack direction="row" useFlexGap spacing={4} sx={{ flexWrap: 'wrap' }}>
                      <Stat label="das cercosporioses confirmadas o modelo apontou" value={percent(reviews.recallCercosporiose)} />
                      <Stat label="dos alertas do modelo foram confirmados" value={percent(reviews.precisionCercosporiose)} />
                      <Stat label="das fotos o modelo e a pessoa concordaram" value={percent(reviews.agreement)} />
                    </Stack>
                    <Confusion labels={reviews.confusionLabels.map(label => leafLabelName[label])} matrix={reviews.confusionMatrix} />
                  </>}
                </Stack>
              </Paper>
            )}

            <Paper variant="outlined" sx={{ p: 2.5 }}>
              <Stack spacing={2}>
                <Typography variant="h6">Antecedência</Typography>
                <Typography variant="body2" color="text.secondary">
                  Quantos dias antes da primeira cercosporiose confirmada na folha o clima chegou a risco alto e o satélite marcou
                  uma área. É a medida de que a detecção foi precoce.
                </Typography>
                {data.leadTimes.length === 0 ? (
                  <Alert severity="info">
                    Sem confirmação em campo neste semestre: a antecedência só existe quando uma folha é confirmada como
                    cercosporiose por alguém da equipe.
                  </Alert>
                ) : (
                  <Table size="small" sx={{ maxWidth: 720 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>Talhão</TableCell>
                        <TableCell>Confirmada em</TableCell>
                        <TableCell align="right">Clima avisou</TableCell>
                        <TableCell align="right">Satélite marcou</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {data.leadTimes.map(lead => (
                        <TableRow key={lead.cultivationId}>
                          <TableCell>{lead.fieldName}</TableCell>
                          <TableCell>{cycleDate(lead.confirmedOn)}</TableCell>
                          <TableCell align="right">{lead.climateLeadDays != null ? `${lead.climateLeadDays} dias antes` : 'não avisou'}</TableCell>
                          <TableCell align="right">{lead.satelliteLeadDays != null ? `${lead.satelliteLeadDays} dias antes` : 'não marcou'}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </Stack>
            </Paper>
          </Stack>
        )}
      </QueryBoundary>
    </PageContainer>
  );
}
