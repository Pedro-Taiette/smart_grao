import {
  Button,
  Card,
  CardActions,
  CardContent,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import MapOutlinedIcon from '@mui/icons-material/MapOutlined';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';

interface FarmCardProps {
  farm: FarmViewModel;
  onOpenFields: (farm: FarmViewModel) => void;
  onEdit: (farm: FarmViewModel) => void;
  onDelete: (farm: FarmViewModel) => void;
}

/** Uma fazenda na lista. Recebe tudo por prop e nao consulta nada — puramente apresentacional. */
export function FarmCard({ farm, onOpenFields, onEdit, onDelete }: FarmCardProps) {
  return (
    <Card>
      <CardContent>
        <Typography variant="h6" component="h2" gutterBottom>
          {farm.name}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {farm.city} — {farm.state}
        </Typography>
      </CardContent>

      <CardActions sx={{ px: 2, pb: 2, pt: 0, justifyContent: 'space-between' }}>
        <Button
          size="small"
          variant="contained"
          startIcon={<MapOutlinedIcon />}
          onClick={() => onOpenFields(farm)}
        >
          Talhões
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
