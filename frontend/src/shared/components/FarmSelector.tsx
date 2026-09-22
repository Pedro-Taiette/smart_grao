import { MenuItem, TextField } from '@mui/material';
import { useFarmContext } from '@/features/farms/useFarmContext';

/**
 * A troca de propriedade, fixa no topo.
 *
 * Some com uma fazenda so — que e o caso comum — porque um seletor de uma opcao nao e escolha, e
 * so ocupa espaco pedindo atencao. Com nenhuma cadastrada tambem some: a tela Hoje ja conduz ao
 * cadastro, e oferecer um seletor vazio seria um beco.
 */
export function FarmSelector() {
  const { farms, farmId, selectFarm } = useFarmContext();

  if (farms.length < 2) return null;

  return (
    <TextField
      select
      size="small"
      value={farmId}
      onChange={(event) => selectFarm(event.target.value)}
      aria-label="Fazenda"
      sx={{
        minWidth: 160,
        maxWidth: 240,
        // Herdar a cor da barra evita um campo branco recortado sobre o verde.
        '& .MuiInputBase-root': { color: 'inherit' },
        '& .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.5)' },
        '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.8)' },
        '& .MuiSelect-icon': { color: 'inherit' },
      }}
    >
      {farms.map((farm) => (
        <MenuItem key={farm.id} value={farm.id}>
          {farm.name}
        </MenuItem>
      ))}
    </TextField>
  );
}
