import { useRef, useState } from 'react';
import { Box, Button, Chip, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import type { IFormFile } from '@/api/generated/model/iFormFile';
import type { ObservationViewModel } from '@/api/generated/model/observationViewModel';
import type { PhotoViewModel } from '@/api/generated/model/photoViewModel';
import { PlantOrgan } from '@/api/generated/model/plantOrgan';
import type { ProtocolViewModel } from '@/api/generated/model/protocolViewModel';
import { getGetPhotosQueryKey, uploadObservationPhoto } from '@/api/generated/photos/photos';
import { apiBaseUrl } from '@/api/httpClient';
import { plantOrganLabel } from '@/features/protocols/protocolLabels';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { PhotoAnalysisDialog } from '@/features/analysis/components/PhotoAnalysisDialog';
import { isSimulated, latestModel, latestReview, leafLabelName, percent, verdictColor } from '@/features/analysis/analysisLabels';

export function photoUrl(id: string) {
  return `${apiBaseUrl}/api/photos/${id}/content`;
}

function AnalysisLine({ photo }: { photo: PhotoViewModel }) {
  const model = latestModel(photo.assessments);
  const human = latestReview(photo.assessments, model);
  if (human) return <Typography variant="caption" color="success.main" sx={{ display: 'block' }}>Revisada: {leafLabelName[human.label]}</Typography>;
  if (model) return <Chip size="small" color={verdictColor[model.label]} variant="outlined" sx={{ height: 'auto', '& .MuiChip-label': { whiteSpace: 'normal', py: 0.25 } }}
    label={`${leafLabelName[model.label]} ${percent(model.confidence)}${isSimulated(model) ? ' · simulado' : ''}`} />;
  if (photo.analysisStatus === 'Failed') return <Typography variant="caption" color="error">Análise falhou</Typography>;
  return <Typography variant="caption" color="text.secondary">Analisando…</Typography>;
}

export function PhotoThumbnail({ photo, onOpen }: { photo: PhotoViewModel; onOpen: () => void }) {
  return (
    <Box component="button" type="button" onClick={onOpen}
      sx={{ display: 'block', width: 128, p: 0, border: 0, bgcolor: 'transparent', textAlign: 'left', cursor: 'pointer', color: 'inherit' }}>
      <Box component="img" src={photoUrl(photo.id)} alt={plantOrganLabel[photo.organ]}
        sx={{ width: 128, height: 96, objectFit: 'cover', borderRadius: 1, display: 'block', bgcolor: 'grey.200' }} />
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.25 }}>
        {plantOrganLabel[photo.organ]}{photo.isSimulated ? ' · simulada' : ''}
      </Typography>
      <AnalysisLine photo={photo} />
    </Box>
  );
}

interface Pending { id: string; file: File; organ: PlantOrgan }

/** Fotos de uma parada. O id nasce no celular: reenviar depois de uma falha não duplica a foto. */
export function ObservationPhotos({ inspectionId, observation, protocol, photos, canAdd }: {
  inspectionId: string;
  observation: ObservationViewModel;
  protocol: ProtocolViewModel | undefined;
  photos: PhotoViewModel[];
  canAdd: boolean;
}) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const input = useRef<HTMLInputElement>(null);
  const requested = (protocol?.items ?? []).filter(item => item.photosRequested > 0 && item.organ);
  const organs = [...new Set(requested.map(item => item.organ!))];
  const [chosenOrgan, setOrgan] = useState<PlantOrgan | null>(null);
  const organ = chosenOrgan && organs.includes(chosenOrgan) ? chosenOrgan : organs[0] ?? PlantOrgan.Leaf;
  const [failed, setFailed] = useState<Pending[]>([]);
  const [sending, setSending] = useState(false);
  const [opened, setOpened] = useState<string | null>(null);
  const openedPhoto = photos.find(photo => photo.id === opened);
  const expected = requested.reduce((sum, item) => sum + item.photosRequested, 0);

  async function send(pending: Pending[]) {
    setSending(true);
    const stillFailing: Pending[] = [];
    for (const item of pending) {
      try {
        await uploadObservationPhoto({
          id: item.id, inspectionId, observationId: observation.id, organ: item.organ,
          targetId: requested.find(r => r.organ === item.organ)?.targetId,
          takenAt: new Date(item.file.lastModified || Date.now()).toISOString(),
          file: item.file as unknown as IFormFile,
        });
      } catch (failure) {
        stillFailing.push(item);
        notifier.notifyError(failure);
      }
    }
    setFailed(stillFailing);
    setSending(false);
    await cache.invalidateQueries({ queryKey: getGetPhotosQueryKey({ inspectionId }) });
    if (stillFailing.length === 0 && pending.length > 0) notifier.notifySuccess('Foto enviada.');
  }

  function pick(files: FileList | null) {
    if (!files || files.length === 0) return;
    void send([...files].map(file => ({ id: crypto.randomUUID(), file, organ })));
    if (input.current) input.current.value = '';
  }

  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">
        Fotos {expected > 0 ? `(${photos.length} de ${expected} pedidas)` : `(${photos.length})`}
      </Typography>
      {photos.length > 0 && (
        <Stack direction="row" useFlexGap spacing={1.5} sx={{ flexWrap: 'wrap' }}>
          {photos.map(photo => <PhotoThumbnail key={photo.id} photo={photo} onOpen={() => setOpened(photo.id)} />)}
        </Stack>
      )}
      {canAdd && (
        <Stack direction="row" useFlexGap spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          {organs.length > 1 && (
            <TextField select size="small" label="Qual folha" value={organ} sx={{ width: 280 }}
              onChange={event => setOrgan(event.target.value as PlantOrgan)}>
              {organs.map(o => <MenuItem key={o} value={o}>{plantOrganLabel[o]}</MenuItem>)}
            </TextField>
          )}
          <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" capture="environment" multiple hidden
            onChange={event => pick(event.target.files)} />
          <Button variant="outlined" disabled={sending} onClick={() => input.current?.click()}>
            {sending ? 'Enviando…' : 'Tirar foto'}
          </Button>
          {failed.length > 0 && <>
            <Chip color="warning" label={`${failed.length} foto(s) não enviada(s)`} />
            <Button disabled={sending} onClick={() => void send(failed)}>Tentar de novo</Button>
          </>}
        </Stack>
      )}
      {openedPhoto && <PhotoAnalysisDialog photo={openedPhoto} onClose={() => setOpened(null)} />}
    </Stack>
  );
}
