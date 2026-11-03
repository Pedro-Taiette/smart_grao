import { useState } from 'react';
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Stack, Switch, TextField, Typography,
} from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useReviewAssessment } from '@/api/generated/analysis/analysis';
import type { LeafLabel } from '@/api/generated/model/leafLabel';
import type { PhotoViewModel } from '@/api/generated/model/photoViewModel';
import { getGetPhotosQueryKey } from '@/api/generated/photos/photos';
import { apiBaseUrl } from '@/api/httpClient';
import { plantOrganLabel } from '@/features/protocols/protocolLabels';
import { useNotifier } from '@/shared/notifications/useNotifier';
import {
  isSimulated, latestModel, latestReview, leafLabelName, leafLabelOrder, modelVerdict, percent,
} from '../analysisLabels';

const reviewerKey = 'smartgrao.reviewer';

function storedReviewer() {
  try {
    return window.localStorage.getItem(reviewerKey) ?? '';
  } catch {
    return '';
  }
}

export function PhotoAnalysisDialog({ photo, onClose }: { photo: PhotoViewModel; onClose: () => void }) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const review = useReviewAssessment();
  const model = latestModel(photo.assessments);
  const human = latestReview(photo.assessments, model);
  const [showHeatmap, setShowHeatmap] = useState(false);
  const [reviewer, setReviewer] = useState(storedReviewer);
  const [notes, setNotes] = useState('');

  async function confirm(label: LeafLabel) {
    if (!model || !reviewer.trim()) return;
    try {
      await review.mutateAsync({ id: model.id, data: { label, reviewer: reviewer.trim(), notes: notes.trim() || null } });
      try { window.localStorage.setItem(reviewerKey, reviewer.trim()); } catch { /* sem storage, segue */ }
      await cache.invalidateQueries({ queryKey: getGetPhotosQueryKey() });
      notifier.notifySuccess('Revisão registrada.');
      onClose();
    } catch (failure) {
      notifier.notifyError(failure);
    }
  }

  const image = showHeatmap && model?.hasHeatmap
    ? `${apiBaseUrl}/api/assessments/${model.id}/heatmap`
    : `${apiBaseUrl}/api/photos/${photo.id}/content`;

  return (
    <Dialog open onClose={review.isPending ? undefined : onClose} maxWidth="md" fullWidth>
      <DialogTitle>{plantOrganLabel[photo.organ]}{photo.growthStage ? ` · ${photo.growthStage}` : ''}</DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <Box component="img" src={image} alt={plantOrganLabel[photo.organ]}
            sx={{ width: '100%', maxHeight: 420, objectFit: 'contain', bgcolor: 'grey.100', borderRadius: 1 }} />
          {model?.hasHeatmap && (
            <FormControlLabel control={<Switch checked={showHeatmap} onChange={(_, v) => setShowHeatmap(v)} />}
              label="Mostrar onde o modelo olhou" />
          )}
          {photo.isSimulated && <Alert severity="info">Foto de estação simulada, não de campo.</Alert>}
          {!model && <Typography color="text.secondary">A análise desta foto ainda não terminou.</Typography>}
          {model && (
            <Stack spacing={0.5}>
              <Typography variant="subtitle1">{modelVerdict[model.label]}</Typography>
              <Typography variant="body2" color="text.secondary">
                Cercosporiose {percent(model.cercosporioseProbability)} · outra mancha {percent(model.otherSpotsProbability)} ·
                {' '}sadia {percent(model.healthyProbability)} · modelo {model.modelVersion}
              </Typography>
              {isSimulated(model) && (
                <Alert severity="warning">
                  Resultado do processador simulado: serve para testar a fila, não diz nada sobre a folha.
                </Alert>
              )}
              <Typography variant="caption" color="text.secondary">
                "Sem sinal" vale para esta folha, não para a lavoura.
              </Typography>
            </Stack>
          )}
          {human && (
            <Alert severity="success">
              {human.reviewer} revisou: {leafLabelName[human.label]}{human.notes ? ` — ${human.notes}` : ''}
            </Alert>
          )}
          {model && (
            <Stack spacing={1.5}>
              <Typography variant="subtitle2">{human ? 'Revisar de novo' : 'O que é, olhando a foto?'}</Typography>
              <TextField size="small" label="Quem está revisando" value={reviewer} onChange={e => setReviewer(e.target.value)}
                sx={{ maxWidth: 320 }} />
              <TextField size="small" label="Observação (opcional)" value={notes} onChange={e => setNotes(e.target.value)} />
            </Stack>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2, flexWrap: 'wrap', gap: 1 }}>
        <Button onClick={onClose} disabled={review.isPending}>Fechar</Button>
        {model && leafLabelOrder.map(label => (
          <Button key={label} variant={label === model.label ? 'contained' : 'outlined'}
            disabled={review.isPending || !reviewer.trim()} onClick={() => void confirm(label)}>
            {leafLabelName[label]}
          </Button>
        ))}
      </DialogActions>
    </Dialog>
  );
}
