import { Box, Chip, ListItemButton, Stack, Typography } from '@mui/material';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import { formatHectares } from '@/shared/format';
import { cropLabels } from '../cropLabels';

interface FieldListItemProps {
  field: FieldViewModel;
  isSelected: boolean;
  onSelect: (fieldId: string) => void;
}

/** Uma linha da lista lateral de talhoes. */
export function FieldListItem({ field, isSelected, onSelect }: FieldListItemProps) {
  return (
    <ListItemButton
      selected={isSelected}
      onClick={() => onSelect(field.id)}
      sx={{ borderRadius: 1, mb: 0.5, alignItems: 'flex-start', flexDirection: 'column', gap: 0.5 }}
    >
      <Stack
        direction="row"
        sx={{ width: '100%', justifyContent: 'space-between', alignItems: 'center' }}
      >
        <Typography variant="subtitle2" noWrap>
          {field.name}
        </Typography>
        {!field.active && <Chip label="Inativo" size="small" />}
      </Stack>

      <Box>
        <Typography variant="body2" color="text.secondary">
          {cropLabels[field.crop]} — {formatHectares(field.areaHectares)}
        </Typography>
      </Box>
    </ListItemButton>
  );
}
