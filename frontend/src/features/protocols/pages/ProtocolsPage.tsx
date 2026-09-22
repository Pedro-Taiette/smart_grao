import { useState } from 'react';
import { Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import { Crop } from '@/api/generated/model/crop';
import { cropLabels, cropOptions } from '@/features/fields/cropLabels';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { ProtocolDialog } from '../components/ProtocolDialogs';
import { protocolStatusColor, protocolStatusLabel } from '../protocolLabels';
import { useProtocolActions } from '../hooks/useProtocolActions';
import { useProtocols } from '../hooks/useProtocols';

/**
 * Os protocolos de vistoria, agrupados por familia de versoes.
 *
 * Mostrar as versoes juntas e o ponto: o valor do versionamento so aparece quando se ve que a v1
 * publicada continua ali depois de a v2 existir.
 */
export function ProtocolsPage() {
  const navigate = useNavigate();
  const { families, crop, setCrop, isLoading, error, refetch } = useProtocols();
  const actions = useProtocolActions();
  const [isCreating, setCreating] = useState(false);

  return (
    <PageContainer>
      <PageHeader
        title="Protocolos de vistoria"
        description="A receita da visita: o que observar em cada parada e em que unidade medir. Cada versão publicada fica registrada como foi usada."
        action={
          <Button variant="contained" startIcon={<AddIcon />} disabled={actions.isSaving} onClick={() => setCreating(true)}>
            Novo protocolo
          </Button>
        }
      />

      <TextField select size="small" label="Cultura" sx={{ mb: 3, minWidth: 180 }}
        value={crop} onChange={(event) => setCrop(event.target.value as Crop)}>
        {cropOptions.filter((c) => c !== Crop.Undefined).map((c) => (
          <MenuItem key={c} value={c}>{cropLabels[c]}</MenuItem>
        ))}
      </TextField>

      <QueryBoundary isLoading={isLoading} error={error} onRetry={refetch}>
        {families.length === 0 ? (
          <EmptyState
            title="Nenhum protocolo para esta cultura"
            description="O protocolo define o que a equipe coleta na vistoria. Crie o primeiro e inclua os alvos do catálogo."
            action={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>Criar protocolo</Button>}
          />
        ) : (
          <Stack spacing={2}>
            {families.map((family) => (
              <Paper key={family.code} variant="outlined" sx={{ p: 2.5 }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{family.name}</Typography>
                  {family.versions.map((version) => (
                    <Stack key={version.id} direction="row" spacing={1.5}
                      sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                      <Typography variant="body2" sx={{ minWidth: 80 }}>Versão {version.version}</Typography>
                      <Chip size="small" color={protocolStatusColor[version.status]} label={protocolStatusLabel[version.status]} />
                      <Typography variant="body2" color="text.secondary">
                        {version.targetCount === 1 ? '1 alvo' : `${version.targetCount} alvos`}
                      </Typography>
                      <Button size="small" onClick={() => navigate(`/ajustes/protocolos/${version.id}`)}>Abrir</Button>
                    </Stack>
                  ))}
                </Stack>
              </Paper>
            ))}
          </Stack>
        )}
      </QueryBoundary>

      {isCreating && <ProtocolDialog isSaving={actions.isSaving} crop={crop} onClose={() => setCreating(false)}
        onSave={async (values) => {
          const created = await actions.createProtocol(values);
          if (created) navigate(`/ajustes/protocolos/${created.id}`);
          return created !== null;
        }} />}
    </PageContainer>
  );
}
