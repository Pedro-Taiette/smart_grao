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

export function photoUrl(id: string) {
  return `${apiBaseUrl}/api/photos/${id}/content`;
}

export function PhotoThumbnail({ photo }: { photo: PhotoViewModel }) {
  return (
    <Box component="a" href={photoUrl(photo.id)} target="_blank" rel="noreferrer"
      sx={{ display: 'block', width: 112, textDecoration: 'none', color: 'inherit' }}>
      <Box component="img" src={photoUrl(photo.id)} alt={plantOrganLabel[photo.organ]}
        sx={{ width: 112, height: 84, objectFit: 'cover', borderRadius: 1, display: 'block', bgcolor: 'grey.200' }} />
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.25 }}>
        {plantOrganLabel[photo.organ]}{photo.isSimulated ? ' · simulada' : ''}
      </Typography>
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
          {photos.map(photo => <PhotoThumbnail key={photo.id} photo={photo} />)}
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
    </Stack>
  );
}
