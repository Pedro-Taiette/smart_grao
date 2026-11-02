import { useState } from 'react';
import { Alert, Paper, Stack, Typography } from '@mui/material';
import { useGetPhotos } from '@/api/generated/photos/photos';
import { PhotoAnalysisDialog } from '@/features/analysis/components/PhotoAnalysisDialog';
import { PhotoThumbnail } from '@/features/inspections/components/ObservationPhotos';

/** Fotos de dispositivos fixos (estação sentinela, drone). Só aparece quando há alguma. */
export function StationPanel({ cultivationId }: { cultivationId: string }) {
  const query = useGetPhotos({ cultivationId }, {
    query: {
      refetchInterval: q => (q.state.data ?? []).some(p => p.analysisStatus === 'Pending' || p.analysisStatus === 'Running') ? 4000 : false,
    },
  });
  const photos = (query.data ?? []).filter(photo => photo.source !== 'Phone').reverse();
  const [opened, setOpened] = useState<string | null>(null);
  const openedPhoto = photos.find(photo => photo.id === opened);
  if (photos.length === 0) return null;
  const simulated = photos.some(photo => photo.isSimulated);

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={1.5}>
        <Typography variant="subtitle2">Estação sentinela{simulated ? ' (simulada)' : ''}</Typography>
        <Typography variant="body2" color="text.secondary">
          Fotos automáticas das folhas de baixo de uma planta fixa, analisadas pelo mesmo modelo das fotos de campo.
        </Typography>
        {simulated && (
          <Alert severity="info">
            Sem hardware nem lavoura neste semestre: as fotos vêm do conjunto de teste e chegam pela mesma rota que a
            estação real usaria.
          </Alert>
        )}
        <Stack direction="row" useFlexGap spacing={1.5} sx={{ flexWrap: 'wrap' }}>
          {photos.map(photo => <PhotoThumbnail key={photo.id} photo={photo} onOpen={() => setOpened(photo.id)} />)}
        </Stack>
      </Stack>
      {openedPhoto && <PhotoAnalysisDialog photo={openedPhoto} onClose={() => setOpened(null)} />}
    </Paper>
  );
}
