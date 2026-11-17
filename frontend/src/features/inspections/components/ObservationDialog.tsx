import { useEffect, useRef, useState } from 'react';
import {
  Alert, Box, Button, Checkbox, Collapse, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, FormControlLabel, MenuItem, Stack, TextField, ToggleButton, ToggleButtonGroup, Typography,
} from '@mui/material';
import MyLocationIcon from '@mui/icons-material/MyLocation';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import { CountUnit } from '@/api/generated/model/countUnit';
import type { ProtocolItemViewModel } from '@/api/generated/model/protocolItemViewModel';
import type { ProtocolViewModel } from '@/api/generated/model/protocolViewModel';
import type { RecordObservationViewModel } from '@/api/generated/model/recordObservationViewModel';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';
import type { Crop } from '@/api/generated/model/crop';
import { useGrowthStages } from '@/features/cultivations/hooks/useGrowthStages';
import { plantOrganLabel } from '@/features/protocols/protocolLabels';
import { useGeolocation } from '../hooks/useGeolocation';
import { countFieldLabel } from '../inspectionLabels';

interface Props {
  crop: Crop;
  protocol: ProtocolViewModel;
  remainingPoints: SamplingPointViewModel[];
  isSaving: boolean;
  onClose: () => void;
  onSave: (data: RecordObservationViewModel) => Promise<boolean>;
}

/**
 * Plantas olhadas em cada parada. A regra 10 paradas x 10 plantas vem da extensao de South Dakota
 * State (ver docs/pesquisa-cercosporiose.md) e e a mesma que as instrucoes do protocolo semeado
 * repetem. Com ela, "% de plantas atacadas" vira uma pergunta que se responde contando nos dedos.
 */
const plantsPerStop = 10;

/** Item que nao foi olhado nesta parada: sem linha, e essa ausencia e visivel depois. */
const notLooked = 'skip';

/**
 * Registrar uma parada.
 *
 * Feita para quem esta de bota no barro, com o celular numa mao:
 *
 * - **O GPS liga sozinho ao abrir.** Nao ha campo de coordenada para digitar; se o aparelho nao
 *   pegar sinal, ai sim aparecem as alternativas.
 * - **Contagem por toque.** "Das 10 plantas, quantas tinham mancha?" com botoes de 0 a 10, em vez de
 *   um campo de porcentagem. O sistema faz a conta.
 * - **Toda pergunta precisa de resposta**, incluindo "Nao olhei". Item avaliado e que deu nada
 *   precisa virar registro, porque e isso que impede a visita de ser lida depois como "estava tudo
 *   limpo" quando ninguem olhou.
 * - **O resto e opcional e fica recolhido** — ocorrencia fora das paradas, estagio, observacoes.
 */
export function ObservationDialog(props: Props) {
  const { locate, isLocating } = useGeolocation();
  const { stages, hasScale } = useGrowthStages(props.crop);

  const [pointId, setPointId] = useState<string>(props.remainingPoints[0]?.id ?? '');
  const [offPlan, setOffPlan] = useState(props.remainingPoints.length === 0);
  const [latitude, setLatitude] = useState('');
  const [longitude, setLongitude] = useState('');
  const [accuracy, setAccuracy] = useState('');
  const [gpsFailed, setGpsFailed] = useState(false);
  const [growthStage, setGrowthStage] = useState('');
  const [notes, setNotes] = useState('');
  const [showMore, setShowMore] = useState(props.remainingPoints.length === 0);
  const [error, setError] = useState<string | null>(null);
  const [answers, setAnswers] = useState<Record<string, string | null>>(() =>
    Object.fromEntries(props.protocol.items.map(item => [item.id, null])));

  const selected = props.remainingPoints.find(point => point.id === pointId);
  const hasPosition = latitude !== '' && longitude !== '';

  async function readDeviceLocation() {
    const position = await locate();
    if (!position) {
      // O GPS falhar nao pode custar a coleta: quem esta no talhao nao volta depois so porque o
      // aparelho nao pegou sinal.
      setGpsFailed(true);
      return;
    }
    setLatitude(String(position.latitude));
    setLongitude(String(position.longitude));
    setAccuracy(String(Math.round(position.accuracyMeters)));
    setGpsFailed(false);
    setError(null);
  }

  // Liga o GPS uma vez, ao abrir: e o que todo mundo faria primeiro.
  const located = useRef(false);
  useEffect(() => {
    if (located.current) return;
    located.current = true;
    void readDeviceLocation();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /**
   * Cai para a coordenada que o plano mandava. Pior do que o GPS efetivo — some quanto a caminhada
   * desviou da malha — e muito melhor do que perder a coleta. Sem precisao: nao houve leitura.
   */
  function applyPlannedLocation() {
    if (!selected) return;
    const [longitudeValue, latitudeValue] = selected.location.coordinates;
    setLatitude(String(latitudeValue));
    setLongitude(String(longitudeValue));
    setAccuracy('');
    setError(null);
  }

  function answer(itemId: string, value: string | null) {
    setAnswers(current => ({ ...current, [itemId]: value }));
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    const lat = Number(latitude.replace(',', '.'));
    const lon = Number(longitude.replace(',', '.'));
    if (!hasPosition || !Number.isFinite(lat) || !Number.isFinite(lon)) {
      setError(selected
        ? 'Falta a localização. Espere o GPS ou toque em "Usar a posição planejada".'
        : 'Falta a localização. Espere o GPS ou toque em "Tentar o GPS de novo".');
      return;
    }
    if (!offPlan && !pointId) {
      setError('Escolha qual parada é esta.');
      return;
    }

    const payload: RecordObservationViewModel['counts'] = [];
    for (const item of props.protocol.items) {
      const value = answers[item.id];
      if (value === null || value.trim() === '') {
        setError(`Responda "${questionFor(item)}" — ou marque "Não olhei".`);
        return;
      }
      if (value === notLooked) continue;

      if (item.unit === CountUnit.Presence) {
        payload.push({ protocolItemId: item.id, value: null, detected: value === 'yes' });
        continue;
      }
      if (item.unit === CountUnit.AttackedPlantPercentage) {
        const plants = Number(value);
        const percentage = (plants / plantsPerStop) * 100;
        payload.push({ protocolItemId: item.id, value: percentage, detected: plants > 0 });
        continue;
      }
      const number = Number(value.replace(',', '.'));
      if (!Number.isFinite(number) || number < 0) {
        setError(`Informe um número em "${questionFor(item)}" — use 0 se não encontrou nada.`);
        return;
      }
      payload.push({ protocolItemId: item.id, value: number, detected: number > 0 });
    }

    if (payload.length === 0) {
      setError('Responda pelo menos uma pergunta com o que você viu.');
      return;
    }

    const accuracyMeters = accuracy.trim() === '' ? null : Number(accuracy.replace(',', '.'));
    const saved = await props.onSave({
      samplingPointId: offPlan ? null : pointId,
      recordedAt: new Date().toISOString(),
      location: { latitude: lat, longitude: lon },
      accuracyMeters: Number.isFinite(accuracyMeters) ? accuracyMeters : null,
      growthStage: growthStage || null,
      notes: notes.trim() || null,
      counts: payload,
    });

    if (saved) props.onClose();
  }

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={submit}>
      <DialogTitle>{offPlan ? 'Registrar algo fora das paradas' : `Registrar parada ${selected?.sequence ?? ''}`}</DialogTitle>
      <DialogContent><Stack spacing={2.5} sx={{ pt: 1 }}>
        {!offPlan && props.remainingPoints.length > 1 && (
          <TextField select label="Qual parada?" value={pointId} onChange={event => setPointId(event.target.value)}
            helperText="O número que aparece no mapa da vistoria.">
            {props.remainingPoints.map(point => (
              <MenuItem key={point.id} value={point.id}>Parada {point.sequence}</MenuItem>
            ))}
          </TextField>
        )}

        <Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
            <MyLocationIcon color={hasPosition ? 'success' : 'disabled'} />
            <Typography variant="body2">
              {isLocating
                ? 'Pegando a localização…'
                : hasPosition
                  ? accuracy ? `Localização ok (precisão de ${accuracy} m)` : 'Localização informada à mão'
                  : 'Sem localização ainda'}
            </Typography>
            {!isLocating && <Button size="small" onClick={() => void readDeviceLocation()}>
              Tentar o GPS de novo
            </Button>}
          </Stack>

          {/* Sem posicao, a saida fica sempre a vista — inclusive enquanto o GPS ainda procura:
              um aparelho que nunca responde nao pode travar a parada. */}
          {!hasPosition && <Stack spacing={1} sx={{ mt: 1 }}>
            {gpsFailed && <Alert severity="warning">
              O celular não pegou o GPS. Confira se a localização está ligada e tente de novo
              {selected ? ', ou use a posição planejada da parada' : ''}.
            </Alert>}
            {selected && <Button variant="outlined" size="small" sx={{ alignSelf: 'flex-start' }} onClick={applyPlannedLocation}>
              Usar a posição planejada (parada {selected.sequence})
            </Button>}
            {gpsFailed && !selected && <Stack direction="row" spacing={1}>
              <TextField size="small" label="Latitude" value={latitude}
                onChange={event => setLatitude(event.target.value)} />
              <TextField size="small" label="Longitude" value={longitude}
                onChange={event => setLongitude(event.target.value)} />
            </Stack>}
          </Stack>}
        </Box>

        <Divider />

        {props.protocol.items.map(item => (
          <CountQuestion key={item.id} item={item} value={answers[item.id]} onChange={value => answer(item.id, value)} />
        ))}

        <Divider />

        <Button size="small" sx={{ alignSelf: 'flex-start' }}
          endIcon={showMore ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          onClick={() => setShowMore(!showMore)}>
          Mais opções (estágio, observações)
        </Button>
        <Collapse in={showMore} unmountOnExit>
          <Stack spacing={2}>
            <FormControlLabel
              control={<Checkbox checked={offPlan} disabled={props.remainingPoints.length === 0}
                onChange={event => setOffPlan(event.target.checked)} />}
              label="Vi alguma coisa fora das paradas planejadas"
            />
            {offPlan && <Typography variant="body2" color="text.secondary" sx={{ mt: '-8px !important' }}>
              Fica registrado onde você está, e não na parada mais próxima.
            </Typography>}

            {hasScale && <TextField select fullWidth label="Estágio da lavoura (opcional)"
              value={growthStage} onChange={event => setGrowthStage(event.target.value)}>
              <MenuItem value="">Não informar</MenuItem>
              {stages.map(stage => (
                <MenuItem key={stage.code} value={stage.code}>{stage.code} — {stage.description}</MenuItem>
              ))}
            </TextField>}

            <TextField label="Observações (opcional)" multiline minRows={2}
              value={notes} onChange={event => setNotes(event.target.value)} />
          </Stack>
        </Collapse>

        {error && <Alert severity="error">{error}</Alert>}
      </Stack></DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={props.onClose} disabled={props.isSaving}>Cancelar</Button>
        <Button type="submit" variant="contained" size="large" disabled={props.isSaving}>
          {props.isSaving ? 'Salvando…' : 'Salvar parada'}
        </Button>
      </DialogActions>
    </form>
  </Dialog>;
}

function questionFor(item: ProtocolItemViewModel): string {
  const where = item.organ ? ` (${plantOrganLabel[item.organ].toLowerCase()})` : '';
  if (item.unit === CountUnit.AttackedPlantPercentage)
    return `${item.targetCommonName}${where}: das ${plantsPerStop} plantas, quantas tinham mancha?`;
  if (item.unit === CountUnit.Presence) return `${item.targetCommonName}${where}: encontrou?`;
  return `${item.targetCommonName}${where}: ${countFieldLabel(item.unit).toLowerCase()}`;
}

/** Uma pergunta do protocolo, respondida por toque sempre que a unidade permite. */
function CountQuestion({ item, value, onChange }: {
  item: ProtocolItemViewModel;
  value: string | null;
  onChange: (value: string | null) => void;
}) {
  const [showHow, setShowHow] = useState(false);
  const options = item.unit === CountUnit.AttackedPlantPercentage
    ? Array.from({ length: plantsPerStop + 1 }, (_, n) => ({ value: String(n), label: String(n) }))
    : item.unit === CountUnit.Presence
      ? [{ value: 'no', label: 'Não' }, { value: 'yes', label: 'Sim' }]
      : null;

  return <Stack spacing={1}>
    <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1, flexWrap: 'wrap' }}>
      <Typography variant="subtitle2">{questionFor(item)}</Typography>
      {item.instructions && <Button size="small" sx={{ p: 0, minWidth: 0 }} onClick={() => setShowHow(!showHow)}>
        {showHow ? 'Fechar' : 'Como contar?'}
      </Button>}
    </Stack>
    {item.instructions && <Collapse in={showHow}>
      <Typography variant="body2" color="text.secondary">{item.instructions}</Typography>
    </Collapse>}

    {options
      ? <ToggleButtonGroup exclusive color="primary" value={value} onChange={(_, next: string | null) => onChange(next)}
          sx={{ flexWrap: 'wrap', gap: 0.5, '& .MuiToggleButton-root': { minWidth: 44, py: 0.75, borderRadius: '8px !important', border: 1, borderColor: 'divider' },
            '& .MuiToggleButton-root.Mui-selected': { bgcolor: 'primary.main', color: 'primary.contrastText', fontWeight: 700 },
            '& .MuiToggleButton-root.Mui-selected:hover': { bgcolor: 'primary.dark' } }}>
          {options.map(option => <ToggleButton key={option.value} value={option.value}>{option.label}</ToggleButton>)}
          <ToggleButton value={notLooked} sx={{ px: 1.5 }}>Não olhei</ToggleButton>
        </ToggleButtonGroup>
      : <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <TextField size="small" label={countFieldLabel(item.unit)} disabled={value === notLooked}
            value={value === notLooked ? '' : value ?? ''} onChange={event => onChange(event.target.value)}
            helperText="Use 0 se olhou e não encontrou nada." />
          <ToggleButton size="small" value={notLooked} selected={value === notLooked}
            onChange={() => onChange(value === notLooked ? null : notLooked)}>Não olhei</ToggleButton>
        </Stack>}
  </Stack>;
}
