import {
  Button,
  Card,
  CardActions,
  CardContent,
  Chip,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';

interface FarmCardProps {
  farm: FarmViewModel;
  /** A que o resto do sistema esta mostrando agora. */
  isSelected: boolean;
  onSelect: (farm: FarmViewModel) => void;
  onEdit: (farm: FarmViewModel) => void;
  onDelete: (farm: FarmViewModel) => void;
}

/**
 * Uma fazenda na lista. Recebe tudo por prop e nao consulta nada — puramente apresentacional.
 *
 * Perdeu os atalhos para Talhoes e Equipe: esses destinos agora estao na barra de navegacao e
 * valem para a fazenda selecionada. O cartao faz uma coisa — escolher qual e essa fazenda.
 */
export function FarmCard({ farm, isSelected, onSelect, onEdit, onDelete }: FarmCardProps) {
  return (
    <Card variant={isSelected ? 'elevation' : 'outlined'}>
      <CardContent>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
          <Typography variant="h6" component="h2">{farm.name}</Typography>
          {isSelected && <Chip size="small" color="primary" label="Em uso" />}
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {farm.city} — {farm.state}
        </Typography>
      </CardContent>

      <CardActions sx={{ px: 2, pb: 2, pt: 0, justifyContent: 'space-between' }}>
        <Button
          size="small"
          variant={isSelected ? 'text' : 'contained'}
          disabled={isSelected}
          onClick={() => onSelect(farm)}
        >
          {isSelected ? 'Em uso' : 'Usar esta'}
        </Button>

        <Stack direction="row" spacing={0.5}>
          <IconButton aria-label={`Editar ${farm.name}`} onClick={() => onEdit(farm)}>
            <EditOutlinedIcon fontSize="small" />
          </IconButton>
          <IconButton aria-label={`Excluir ${farm.name}`} onClick={() => onDelete(farm)}>
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Stack>
      </CardActions>
    </Card>
  );
}
