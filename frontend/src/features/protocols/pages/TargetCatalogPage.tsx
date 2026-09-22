import { useState } from 'react';
import { Box, Button, Chip, MenuItem, Paper, Stack, TextField, Tooltip, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import type { MonitoringTargetViewModel } from '@/api/generated/model/monitoringTargetViewModel';
import { Crop } from '@/api/generated/model/crop';
import { TargetKind } from '@/api/generated/model/targetKind';
import { cropLabels, cropOptions } from '@/features/fields/cropLabels';
import { PageContainer } from '@/shared/components/AppLayout';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { AutomationDialog, TargetDialog } from '../components/TargetDialogs';
import {
  automationColor, automationExplanation, automationLabel, targetKindLabel, targetKindOptions,
} from '../protocolLabels';
import { useTargetActions } from '../hooks/useTargetActions';
import { useTargetCatalog } from '../hooks/useTargetCatalog';

/**
 * O catalogo de alvos.
 *
 * A coluna que importa e a situacao da automacao: o catalogo e amplo de proposito, e sem esse
 * rotulo a lista poderia ser lida como "o sistema reconhece tudo isso automaticamente". Ver
 * `docs/fase-2-catalogo-alvos.md`.
 */
export function TargetCatalogPage() {
  const catalog = useTargetCatalog();
  const actions = useTargetActions();
  const [isCreating, setCreating] = useState(false);
  const [editing, setEditing] = useState<MonitoringTargetViewModel | null>(null);

  const pests = catalog.targets.filter((t) => t.kind === TargetKind.Pest);
  const diseases = catalog.targets.filter((t) => t.kind === TargetKind.FoliarDisease);

  return (
    <PageContainer>
      <Stack direction="row" sx={{ mb: 3, justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="h5" component="h1">Catálogo de alvos</Typography>
          <Typography variant="body2" color="text.secondary">
            As pragas e doenças foliares que a equipe pode monitorar. Vale para qualquer lavoura da cultura.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} disabled={actions.isSaving} onClick={() => setCreating(true)}>
          Novo alvo
        </Button>
      </Stack>

      <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap', gap: 2 }}>
        <TextField select size="small" label="Cultura" sx={{ minWidth: 180 }}
          value={catalog.crop} onChange={(event) => catalog.setCrop(event.target.value as Crop)}>
          {cropOptions.filter((c) => c !== Crop.Undefined).map((c) => (
            <MenuItem key={c} value={c}>{cropLabels[c]}</MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="Tipo" sx={{ minWidth: 180 }}
          value={catalog.kind} onChange={(event) => catalog.setKind(event.target.value as TargetKind | '')}>
          <MenuItem value="">Todos</MenuItem>
          {targetKindOptions.map((k) => <MenuItem key={k} value={k}>{targetKindLabel[k]}</MenuItem>)}
        </TextField>
      </Stack>

      <QueryBoundary isLoading={catalog.isLoading} error={catalog.error} onRetry={catalog.refetch}>
        {catalog.targets.length === 0 ? (
          <EmptyState
            title="Nenhum alvo neste filtro"
            description="Cadastre o primeiro alvo desta cultura para montar um protocolo de vistoria."
            action={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>Cadastrar alvo</Button>}
          />
        ) : (
          <Stack spacing={3}>
            {pests.length > 0 && <TargetGroup title="Pragas" targets={pests} onEdit={setEditing} isSaving={actions.isSaving} />}
            {diseases.length > 0 && <TargetGroup title="Doenças foliares" targets={diseases} onEdit={setEditing} isSaving={actions.isSaving} />}
          </Stack>
        )}
      </QueryBoundary>

      {isCreating && <TargetDialog isSaving={actions.isSaving} crop={catalog.crop}
        onClose={() => setCreating(false)} onSave={actions.createTarget} />}
      {editing && <AutomationDialog isSaving={actions.isSaving} target={editing}
        onClose={() => setEditing(null)} onSave={(value) => actions.changeAutomation(editing.id, value)} />}
    </PageContainer>
  );
}

function TargetGroup({ title, targets, onEdit, isSaving }: {
  title: string;
  targets: MonitoringTargetViewModel[];
  onEdit: (target: MonitoringTargetViewModel) => void;
  isSaving: boolean;
}) {
  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 1.5 }}>{title}</Typography>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' } }}>
        {targets.map((target) => (
          <Paper key={target.id} variant="outlined" sx={{ p: 2 }}>
            <Stack spacing={1}>
              <Typography variant="subtitle1">{target.commonName}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                {target.scientificName}
              </Typography>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                <Tooltip title={automationExplanation[target.automation]}>
                  <Chip size="small" color={automationColor[target.automation]} label={automationLabel[target.automation]} />
                </Tooltip>
                <Button size="small" disabled={isSaving} onClick={() => onEdit(target)}>Alterar situação</Button>
              </Stack>
            </Stack>
          </Paper>
        ))}
      </Box>
    </Box>
  );
}
