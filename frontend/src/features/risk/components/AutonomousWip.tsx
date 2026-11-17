import { Box, Button, Chip, Stack, Typography } from '@mui/material';
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import { useNavigate } from 'react-router-dom';

/**
 * A entrada do modulo de coleta autonoma, no fim da aba Situacao.
 *
 * O rover ja tem uma simulacao (`/talhoes/:id/rover`); o drone segue so como plano. A tela diz qual
 * e qual: "Simulação" num, "Em desenvolvimento" no outro. Plano e limites em
 * `docs/modulo-coleta-autonoma.md`.
 *
 * O drone e o rover nao fazem a mesma coisa: a cercosporiose comeca nas folhas de baixo, sob o
 * dossel. Drone voando por cima ve a copa — refina o *onde* do satelite. Quem fotografa a folha de
 * baixo e o rover andando na entrelinha — alimenta o *o que e*.
 */
export function AutonomousWip({ fieldId }: { fieldId: string }) {
  const navigate = useNavigate();
  return (
    <Box sx={{ p: 2.5, border: 2, borderStyle: 'dashed', borderColor: 'divider', borderRadius: 2 }}>
      <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', mb: 1, flexWrap: 'wrap', rowGap: 1 }}>
        <PrecisionManufacturingIcon color="action" />
        <Typography variant="h6" component="h2">Coleta autônoma — sem ninguém ir a campo</Typography>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Hoje as fotos dependem de alguém caminhar até as paradas. A coleta autônoma vai até as áreas que o satélite
        marcou sozinha — e aí a IA vira o olho da máquina.
      </Typography>
      <Stack spacing={1.5}>
        <Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.25 }}>
            <Typography variant="body2"><strong>Rover</strong> · reforça o eixo 3 (o que é)</Typography>
            <Chip size="small" color="warning" label="Simulação" />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            Anda na entrelinha até as paradas e fotografa as folhas de baixo, onde a doença começa.
          </Typography>
          <Button variant="contained" size="small" startIcon={<PlayArrowIcon />} sx={{ mt: 1 }}
            onClick={() => navigate(`/talhoes/${fieldId}/rover`)}>
            Ver o rover em ação
          </Button>
        </Box>
        <Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.25 }}>
            <Typography variant="body2"><strong>Drone</strong> · reforça o eixo 2 (onde olhar)</Typography>
            <Chip size="small" label="Em desenvolvimento" />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            Sobrevoa as áreas marcadas com muito mais detalhe que o satélite. Vê a copa, não as folhas de baixo.
          </Typography>
        </Box>
      </Stack>
    </Box>
  );
}
