import { Alert, Box, Stack, Typography } from '@mui/material';
import type { PhotoViewModel } from '@/api/generated/model/photoViewModel';
import { PhotoAnalysisDialog } from '@/features/analysis/components/PhotoAnalysisDialog';
import { PhotoThumbnail } from '@/features/inspections/components/ObservationPhotos';
import type { PhotoFacts } from '../axes';

/**
 * Eixo 3 — todas as fotos do plantio com o que a IA disse, as que esperam revisao primeiro.
 *
 * Antes as fotos ficavam espalhadas: as de campo dentro de cada parada de cada vistoria, as da
 * estacao num painel a parte. Para saber "a IA achou alguma coisa neste talhao?" era preciso abrir
 * vistoria por vistoria. Agora a resposta esta aqui, e o botao "Revisar fotos" da decisao abre a
 * primeira que espera revisao.
 */
export function PhotosPanel({ photos, facts, openedId, onOpen, onClose }: {
  photos: PhotoViewModel[];
  facts: PhotoFacts;
  openedId: string | null;
  onOpen: (id: string) => void;
  onClose: () => void;
}) {
  const awaiting = new Set(facts.awaitingReview.map(photo => photo.id));
  const newestFirst = [...photos].sort((a, b) => b.takenAt.localeCompare(a.takenAt));
  const pending = newestFirst.filter(photo => awaiting.has(photo.id));
  const rest = newestFirst.filter(photo => !awaiting.has(photo.id));
  const opened = photos.find(photo => photo.id === openedId);
  const simulated = photos.some(photo => photo.isSimulated);

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        Cada foto de folha passa pelo modelo de IA, que dá um palpite: cercosporiose, outra mancha ou folha
        sadia. O palpite nunca é diagnóstico — quem olhou a foto confirma ou corrige.
      </Typography>

      {photos.length === 0 && (
        <Alert severity="info">
          Nenhuma foto ainda. As fotos entram pela vistoria: em cada parada, fotografe as folhas pedidas.
        </Alert>
      )}

      {simulated && (
        <Alert severity="info">
          Parte das fotos é da estação sentinela ou do rover <strong>simulados</strong>: sem hardware nem lavoura
          neste semestre, elas vêm do conjunto público de teste e chegam pela mesma rota que o equipamento real
          usaria. Estão marcadas com a origem e "simulada".
        </Alert>
      )}

      {pending.length > 0 && <Group title={`Aguardando sua revisão (${pending.length})`} photos={pending} onOpen={onOpen} />}
      {rest.length > 0 && <Group title={pending.length > 0 ? 'As outras' : 'Fotos'} photos={rest} onOpen={onOpen} />}

      {opened && <PhotoAnalysisDialog photo={opened} onClose={onClose} />}
    </Stack>
  );
}

function Group({ title, photos, onOpen }: { title: string; photos: PhotoViewModel[]; onOpen: (id: string) => void }) {
  return (
    <Box>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>{title}</Typography>
      <Stack direction="row" useFlexGap spacing={1.5} sx={{ flexWrap: 'wrap' }}>
        {photos.map(photo => <PhotoThumbnail key={photo.id} photo={photo} onOpen={() => onOpen(photo.id)} />)}
      </Stack>
    </Box>
  );
}
