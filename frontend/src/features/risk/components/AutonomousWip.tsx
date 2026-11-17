import { Box, Chip, Stack, Typography } from '@mui/material';
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing';

/**
 * O modulo de coleta autonoma (drone e rover), ainda em desenvolvimento.
 *
 * Aparece para dizer onde ele vai entrar — nos eixos 2 e 3 — sem fingir que existe: nao tem botao,
 * nao tem dado, e o rotulo "Em desenvolvimento" fica a vista. Plano e limites em
 * `docs/modulo-coleta-autonoma.md`.
 *
 * O drone e o rover nao fazem a mesma coisa, e a tela diz isso: a cercosporiose comeca nas folhas
 * de baixo, sob o dossel. Drone voando por cima ve a copa — refina o *onde* do satelite. Quem
 * fotografa a folha de baixo e o rover andando na entrelinha — alimenta o *o que e*.
 */
export function AutonomousWip() {
  return (
    <Box sx={{ p: 2.5, border: 2, borderStyle: 'dashed', borderColor: 'divider', borderRadius: 2 }}>
      <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', mb: 1, flexWrap: 'wrap', rowGap: 1 }}>
        <PrecisionManufacturingIcon color="action" />
        <Typography variant="h6" component="h2">Coleta autônoma — drone e rover</Typography>
        <Chip size="small" label="Em desenvolvimento" />
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Hoje as fotos dependem de alguém ir a campo. O próximo passo é ir até as áreas que o satélite marcou sem
        precisar de ninguém:
      </Typography>
      <Stack spacing={0.75}>
        <Typography variant="body2">
          <strong>Drone (reforça o eixo 2 — onde olhar).</strong> Sobrevoa as áreas marcadas com muito mais detalhe
          que o satélite. Vê a copa, não as folhas de baixo, onde a doença começa.
        </Typography>
        <Typography variant="body2">
          <strong>Rover (reforça o eixo 3 — o que é).</strong> Anda na entrelinha até as paradas e fotografa as
          folhas de baixo. As fotos entram na mesma fila da IA, marcadas com a origem.
        </Typography>
      </Stack>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.5 }}>
        Ainda não coleta nada. A primeira versão será uma simulação, marcada como simulada na tela.
      </Typography>
    </Box>
  );
}
