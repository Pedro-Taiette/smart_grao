import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Radio,
  Stack,
  Typography,
} from '@mui/material';
import type { SamplingMode } from '@/api/generated/model/samplingMode';
import { formatHectares } from '@/shared/format';
import {
  defaultDetailLevel,
  detailLevels,
  modeExplanation,
  modeQuestion,
} from '../samplingLabels';

interface SamplingPlanDialogProps {
  fieldName: string;
  fieldAreaHectares: number;
  isGenerating: boolean;
  onConfirm: (mode: SamplingMode, spacingMeters: number) => void;
  onClose: () => void;
}

const modes: SamplingMode[] = ['Monitoring', 'Mapping'];

/**
 * Escolha do tipo de amostragem.
 *
 * O dialogo pergunta a **duvida do produtor**, nunca o modo tecnico: nao aparece "Monitoring",
 * "Mapping" nem campo de espacamento em metros. Sao dois cartoes com as duas perguntas que a
 * literatura trata de forma separada, e a escolha entre elas e que define a densidade da malha.
 *
 * Um clique basta: "Preciso aplicar?" ja vem selecionado, e o nivel de detalhe do outro caminho ja
 * vem no padrao recomendado. Quem nao quiser decidir nada so aperta "Marcar os pontos".
 *
 * Quem controla a abertura e a pagina, montando e desmontando este componente. E o que faz a escolha
 * voltar ao padrao a cada vez sem um efeito de reinicializacao: o estado nasce novo junto com o
 * componente. A escolha anterior nao deve influenciar a proxima, que costuma ser sobre outro talhao
 * ou outro momento da safra.
 */
export function SamplingPlanDialog({
  fieldName,
  fieldAreaHectares,
  isGenerating,
  onConfirm,
  onClose,
}: SamplingPlanDialogProps) {
  const [mode, setMode] = useState<SamplingMode>('Monitoring');
  const [spacingMeters, setSpacingMeters] = useState(defaultDetailLevel.spacingMeters);

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Marcar pontos de amostragem</DialogTitle>

      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {fieldName} — {formatHectares(fieldAreaHectares)}
        </Typography>

        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          O que você quer saber?
        </Typography>

        <Stack spacing={1.5}>
          {modes.map((option) => (
            <Card key={option} variant="outlined" sx={{ borderColor: mode === option ? 'primary.main' : undefined }}>
              <CardActionArea onClick={() => setMode(option)} sx={{ p: 2 }}>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                  <Radio checked={mode === option} size="small" sx={{ p: 0, mt: 0.25 }} />
                  <Box>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                      {modeQuestion[option]}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {modeExplanation[option]}
                    </Typography>
                  </Box>
                </Stack>
              </CardActionArea>
            </Card>
          ))}
        </Stack>

        {mode === 'Mapping' && (
          <Box sx={{ mt: 2.5 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Quanto detalhe?
            </Typography>

            <Stack spacing={1}>
              {detailLevels.map((level) => (
                <Card
                  key={level.spacingMeters}
                  variant="outlined"
                  sx={{
                    borderColor:
                      spacingMeters === level.spacingMeters ? 'primary.main' : undefined,
                  }}
                >
                  <CardActionArea
                    onClick={() => setSpacingMeters(level.spacingMeters)}
                    sx={{ px: 2, py: 1.5 }}
                  >
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                      <Radio
                        checked={spacingMeters === level.spacingMeters}
                        size="small"
                        sx={{ p: 0, mt: 0.25 }}
                      />
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {level.label}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {level.description}
                        </Typography>
                      </Box>
                    </Stack>
                  </CardActionArea>
                </Card>
              ))}
            </Stack>

            <Alert severity="info" sx={{ mt: 1.5 }}>
              Nesta opção os pontos vão até a divisa do talhão. É de propósito: a praga costuma
              entrar pela borda, e é justamente lá que ela aparece primeiro.
            </Alert>
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={isGenerating}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          onClick={() => onConfirm(mode, spacingMeters)}
          disabled={isGenerating}
        >
          {isGenerating ? 'Marcando…' : 'Marcar os pontos'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
