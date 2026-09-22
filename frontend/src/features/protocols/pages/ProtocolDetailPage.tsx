import { Alert, Box, Button, Chip, Divider, Paper, Stack, Tooltip, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate, useParams } from 'react-router-dom';
import type { AddProtocolItemViewModel } from '@/api/generated/model/addProtocolItemViewModel';
import { cropLabels } from '@/features/fields/cropLabels';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { PageContainer } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { ProtocolItemDialog, RenameProtocolDialog } from '../components/ProtocolDialogs';
import {
  automationColor, automationExplanation, automationLabel, describeCollection, describePhotos,
  describeReferenceLevel, protocolStatusColor, protocolStatusExplanation, protocolStatusLabel,
} from '../protocolLabels';
import type { ProtocolItemValues } from '../protocolSchema';
import { useProtocolWorkspace } from '../hooks/useProtocolWorkspace';

export function ProtocolDetailPage() {
  const { protocolId = '' } = useParams();
  // Uma URL diferente monta o estado de formulario do zero, para que um dialogo aberto nao acabe
  // gravando na versao errada.
  return <ProtocolWorkspace key={protocolId} protocolId={protocolId} />;
}

function ProtocolWorkspace({ protocolId }: { protocolId: string }) {
  const navigate = useNavigate();
  const w = useProtocolWorkspace(protocolId);
  const protocol = w.protocol;

  return (
    <PageContainer>
      <Stack spacing={2.5}>
        <Button sx={{ alignSelf: 'flex-start' }} onClick={() => navigate('/ajustes/protocolos')}>Voltar aos protocolos</Button>

        <QueryBoundary isLoading={w.isLoading} error={w.error} onRetry={w.refetch}>
          {protocol === undefined ? null : <>
            <Box>
              <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                <Typography variant="h5" component="h1">{protocol.name}</Typography>
                <Chip size="small" color={protocolStatusColor[protocol.status]} label={protocolStatusLabel[protocol.status]} />
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {cropLabels[protocol.crop]} · versão {protocol.version} · código {protocol.code}
              </Typography>
            </Box>

            <Alert severity={w.isDraft ? 'info' : 'success'}>{protocolStatusExplanation[protocol.status]}</Alert>

            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              {w.isDraft && <>
                <Button variant="contained" startIcon={<AddIcon />} disabled={w.isSaving || w.availableTargets.length === 0}
                  onClick={() => w.openDialog({ kind: 'item' })}>Incluir alvo</Button>
                <Button disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'rename' })}>Renomear</Button>
                <Button disabled={w.isSaving || protocol.items.length === 0}
                  onClick={() => w.publishProtocol(protocol.id)}>Publicar</Button>
              </>}
              {w.isPublished && <>
                <Button variant="contained" disabled={w.isSaving} onClick={async () => {
                  const next = await w.createVersion(protocol.id);
                  if (next) navigate(`/ajustes/protocolos/${next.id}`);
                }}>Abrir próxima versão</Button>
                <Button color="error" disabled={w.isSaving}
                  onClick={() => w.retireProtocol(protocol.id)}>Aposentar</Button>
              </>}
            </Stack>

            {w.isDraft && w.availableTargets.length === 0 && protocol.items.length > 0 && <Typography variant="body2" color="text.secondary">
              Todos os alvos de {cropLabels[protocol.crop].toLowerCase()} do catálogo já estão neste protocolo.
            </Typography>}

            <Divider />
            <Typography variant="h6">Alvos desta versão</Typography>

            {protocol.items.length === 0
              ? <Alert severity="info">
                  Nenhum alvo ainda. Inclua pelo menos um antes de publicar — é ele que diz o que a
                  equipe vai olhar e anotar no campo.
                </Alert>
              : protocol.items.map((item) => (
                <Paper key={item.id} variant="outlined" sx={{ p: 2.5 }}>
                  <Stack spacing={1}>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                      <Typography variant="subtitle1">{item.targetCommonName}</Typography>
                      <Tooltip title={automationExplanation[item.targetAutomation]}>
                        <Chip size="small" color={automationColor[item.targetAutomation]}
                          label={automationLabel[item.targetAutomation]} />
                      </Tooltip>
                    </Stack>
                    <Typography variant="body2">{describeCollection(item.organ, item.unit)}</Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
                      {item.instructions}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">{describePhotos(item.photosRequested)}</Typography>
                    {item.referenceLevel
                      ? <Typography variant="body2">
                          <strong>{describeReferenceLevel(item.referenceLevel.threshold, item.unit)}</strong>
                          {' · '}{item.referenceLevel.source}
                        </Typography>
                      : <Typography variant="body2" color="text.secondary">
                          Sem nível de referência publicado — a decisão fica com quem avalia.
                        </Typography>}
                    {w.isDraft && <Button size="small" color="error" sx={{ alignSelf: 'flex-start' }} disabled={w.isSaving}
                      onClick={() => w.openDialog({ kind: 'remove', item })}>Remover</Button>}
                  </Stack>
                </Paper>
              ))}
          </>}
        </QueryBoundary>
      </Stack>

      {protocol && w.dialog?.kind === 'item' && <ProtocolItemDialog isSaving={w.isSaving}
        targets={w.availableTargets} onClose={w.closeDialog}
        onSave={(values) => submitItem(protocol.id, values, w.addItem)} />}

      {protocol && w.dialog?.kind === 'rename' && <RenameProtocolDialog isSaving={w.isSaving} name={protocol.name}
        onClose={w.closeDialog} onSave={async (name) => await w.renameProtocol(protocol.id, name) !== null} />}

      <ConfirmDialog
        open={w.dialog?.kind === 'remove'}
        title="Remover alvo do protocolo"
        message={w.dialog?.kind === 'remove'
          ? `Remover "${w.dialog.item.targetCommonName}" desta versão? O alvo continua no catálogo.`
          : ''}
        confirmLabel="Remover"
        isWorking={w.isSaving}
        onConfirm={async () => {
          if (w.dialog?.kind !== 'remove' || !protocol) return;
          if (await w.removeItem(protocol.id, w.dialog.item.id) !== null) w.closeDialog();
        }}
        onCancel={w.closeDialog}
      />
    </PageContainer>
  );
}

/**
 * Traduz o formulario para o contrato: o nivel de referencia so existe quando tem numero **e**
 * fonte, e o orgao vazio vira `null` — que e o que a contagem por armadilha espera.
 */
async function submitItem(
  protocolId: string,
  values: ProtocolItemValues,
  addItem: (id: string, data: AddProtocolItemViewModel) => Promise<unknown>,
) {
  const threshold = values.referenceThreshold.replace(',', '.');
  const result = await addItem(protocolId, {
    targetId: values.targetId,
    organ: values.organ === '' ? null : values.organ,
    unit: values.unit,
    photosRequested: values.photosRequested,
    instructions: values.instructions,
    referenceLevel: threshold === ''
      ? null
      : { threshold: Number(threshold), source: values.referenceSource },
  });
  return result !== null;
}
