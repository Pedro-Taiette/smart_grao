import { useState } from 'react';
import {
  Alert, Autocomplete, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, FormControlLabel, Stack, TextField, Typography,
} from '@mui/material';
import MyLocationIcon from '@mui/icons-material/MyLocation';
import { CountUnit } from '@/api/generated/model/countUnit';
import type { ProtocolViewModel } from '@/api/generated/model/protocolViewModel';
import type { RecordObservationViewModel } from '@/api/generated/model/recordObservationViewModel';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';
import type { Crop } from '@/api/generated/model/crop';
import { useGrowthStages } from '@/features/cultivations/hooks/useGrowthStages';
import { describeCollection } from '@/features/protocols/protocolLabels';
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

interface CountEntry {
  value: string;
  detected: boolean;
  /** Alvo não avaliado nesta parada: sem linha, e essa ausência é visível depois. */
  skipped: boolean;
}

/**
 * Registrar uma parada.
 *
 * As duas decisoes que fazem esta tela funcionar de bota no barro:
 *
 * - **O GPS e um botao, nunca um campo para digitar.** Digitar coordenada em campo e a forma mais
 *   rapida de gravar a parada no lugar errado, e uma troca de sinal poe o ponto no outro hemisferio.
 * - **Todo alvo do protocolo ja vem na lista, marcado como avaliado.** A pessoa desmarca o que nao
 *   olhou. E o contrario do que seria natural — e de proposito: alvo avaliado e que deu nada precisa
 *   virar registro, porque e isso que impede a visita de ser lida depois como "estava tudo limpo".
 */
export function ObservationDialog(props: Props) {
  const { locate, isLocating } = useGeolocation();
  const { stages, hasScale } = useGrowthStages(props.crop);

  const [pointId, setPointId] = useState<string | null>(props.remainingPoints[0]?.id ?? null);
  const [offPlan, setOffPlan] = useState(props.remainingPoints.length === 0);
  const [latitude, setLatitude] = useState('');
  const [longitude, setLongitude] = useState('');
  const [accuracy, setAccuracy] = useState('');
  const [growthStage, setGrowthStage] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [counts, setCounts] = useState<Record<string, CountEntry>>(() =>
    Object.fromEntries(props.protocol.items.map(item =>
      [item.id, { value: '', detected: false, skipped: false }])));

  const [manual, setManual] = useState(false);
  const selected = props.remainingPoints.find(point => point.id === pointId);

  async function useDeviceLocation() {
    const position = await locate();
    if (!position) {
      // O GPS falhar nao pode custar a coleta: quem esta no talhao nao volta depois so porque o
      // aparelho nao pegou sinal. Abre-se o caminho manual, com o aviso de que ali nao ha leitura.
      setManual(true);
      setError('Não foi possível ler o GPS. Informe a posição abaixo ou use a coordenada planejada.');
      return;
    }
    setLatitude(String(position.latitude));
    setLongitude(String(position.longitude));
    setAccuracy(String(Math.round(position.accuracyMeters)));
    setError(null);
  }

  /**
   * Cai para a coordenada que o plano mandava. Registrar a parada com ela e pior do que com o GPS
   * efetivo — some a informacao de quanto a caminhada desviou da malha — e muito melhor do que
   * perder a coleta. A precisao fica vazia de proposito: nao houve leitura de aparelho.
   */
  function usePlannedLocation() {
    if (!selected) return;
    const [longitudeValue, latitudeValue] = selected.location.coordinates;
    setLatitude(String(latitudeValue));
    setLongitude(String(longitudeValue));
    setAccuracy('');
    setError(null);
  }

  function updateCount(itemId: string, patch: Partial<CountEntry>) {
    setCounts(current => ({ ...current, [itemId]: { ...current[itemId], ...patch } }));
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    const lat = Number(latitude.replace(',', '.'));
    const lon = Number(longitude.replace(',', '.'));
    if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
      setError('Informe onde você está — use o botão do GPS.');
      return;
    }
    if (!offPlan && !pointId) {
      setError('Escolha a parada da malha ou marque que é uma ocorrência fora dela.');
      return;
    }

    const evaluated = props.protocol.items.filter(item => !counts[item.id].skipped);
    if (evaluated.length === 0) {
      setError('Marque pelo menos um alvo como avaliado.');
      return;
    }

    const payload: RecordObservationViewModel['counts'] = [];
    for (const item of evaluated) {
      const entry = counts[item.id];
      if (item.unit === CountUnit.Presence) {
        payload.push({ protocolItemId: item.id, value: null, detected: entry.detected });
        continue;
      }
      const value = Number(entry.value.replace(',', '.'));
      if (entry.value.trim() === '' || !Number.isFinite(value) || value < 0) {
        setError(`Informe a contagem de ${item.targetCommonName} — use 0 se não encontrou nada.`);
        return;
      }
      payload.push({ protocolItemId: item.id, value, detected: value > 0 });
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
      <DialogTitle>Registrar parada</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        {error && <Alert severity="error">{error}</Alert>}

        <FormControlLabel
          control={<Checkbox checked={offPlan} onChange={event => setOffPlan(event.target.checked)} />}
          label="Vi alguma coisa fora das paradas planejadas"
        />

        {offPlan
          ? <Alert severity="info">
              A ocorrência fica registrada onde você está, e não na parada mais próxima.
            </Alert>
          : <Autocomplete
              options={props.remainingPoints}
              value={selected ?? null}
              onChange={(_, point) => setPointId(point?.id ?? null)}
              getOptionLabel={point => `Parada ${point.sequence}`}
              renderInput={params => <TextField {...params} label="Qual parada?"
                helperText="As paradas já registradas nesta visita não aparecem." />}
            />}

        <Stack spacing={1}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
            <Button variant="contained" startIcon={<MyLocationIcon />} disabled={isLocating}
              onClick={useDeviceLocation}>
              {isLocating ? 'Localizando…' : 'Usar minha localização'}
            </Button>
            <Typography variant="body2" color="text.secondary">
              {latitude && longitude
                ? `${Number(latitude).toFixed(5)}, ${Number(longitude).toFixed(5)}${accuracy ? ` · ${accuracy} m` : ' · sem leitura de GPS'}`
                : 'Nenhuma leitura ainda'}
            </Typography>
          </Stack>

          {!manual && <Button size="small" sx={{ alignSelf: 'flex-start' }}
            onClick={() => setManual(true)}>GPS não pegou? Informar de outro jeito</Button>}

          {manual && <Stack spacing={1}>
            {selected && <Button size="small" sx={{ alignSelf: 'flex-start' }} onClick={usePlannedLocation}>
              Usar a coordenada planejada da parada {selected.sequence}
            </Button>}
            <Stack direction="row" spacing={1}>
              <TextField size="small" label="Latitude" value={latitude}
                onChange={event => setLatitude(event.target.value)} />
              <TextField size="small" label="Longitude" value={longitude}
                onChange={event => setLongitude(event.target.value)} />
            </Stack>
            <Typography variant="caption" color="text.secondary">
              Sem leitura do aparelho não há precisão para registrar, e a parada fica marcada como
              posição informada à mão.
            </Typography>
          </Stack>}
        </Stack>

        {hasScale && <TextField select fullWidth label="Estágio da lavoura (opcional)"
          value={growthStage} onChange={event => setGrowthStage(event.target.value)}
          slotProps={{ select: { native: true }, inputLabel: { shrink: true } }}>
          <option value="">Não informar</option>
          {stages.map(stage => (
            <option key={stage.code} value={stage.code}>{stage.code} — {stage.description}</option>
          ))}
        </TextField>}

        <Divider />
        <Typography variant="subtitle2">O que o protocolo pede nesta parada</Typography>

        {props.protocol.items.map(item => {
          const entry = counts[item.id];
          return <Stack key={item.id} spacing={0.5}>
            <FormControlLabel
              control={<Checkbox checked={!entry.skipped}
                onChange={event => updateCount(item.id, { skipped: !event.target.checked })} />}
              label={<Typography variant="body2"><strong>{item.targetCommonName}</strong>{' — '}
                {describeCollection(item.organ, item.unit)}</Typography>}
            />
            {!entry.skipped && (item.unit === CountUnit.Presence
              ? <FormControlLabel sx={{ ml: 3 }}
                  control={<Checkbox checked={entry.detected}
                    onChange={event => updateCount(item.id, { detected: event.target.checked })} />}
                  label={countFieldLabel(item.unit)} />
              : <TextField size="small" sx={{ ml: 4 }} label={countFieldLabel(item.unit)}
                  value={entry.value} onChange={event => updateCount(item.id, { value: event.target.value })}
                  helperText="Use 0 se avaliou e não encontrou nada." />)}
          </Stack>;
        })}

        <Divider />
        <TextField label="Observações (opcional)" multiline minRows={2}
          value={notes} onChange={event => setNotes(event.target.value)} />
      </Stack></DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={props.onClose} disabled={props.isSaving}>Cancelar</Button>
        <Button type="submit" variant="contained" disabled={props.isSaving}>
          {props.isSaving ? 'Salvando…' : 'Salvar parada'}
        </Button>
      </DialogActions>
    </form>
  </Dialog>;
}
