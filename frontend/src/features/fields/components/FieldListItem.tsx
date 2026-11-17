import { Box, Button, Chip, ListItem, ListItemButton, Stack, Typography } from '@mui/material';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import { formatHectares } from '@/shared/format';

interface FieldListItemProps {
  field: FieldViewModel;
  isSelected: boolean;
  /**
   * O que está plantado agora, já em português, ou `null` em pousio.
   *
   * A lista dizia só nome e área — o mesmo talhão em qualquer safra. Com o cultivo na linha ela
   * responde "o que está acontecendo na fazenda" sem abrir um talhão de cada vez.
   */
  cultivation: string | null;
  onSelect: (fieldId: string) => void;
  onOpen: (fieldId: string) => void;
}

/**
 * Uma linha da lista lateral de talhoes.
 *
 * Clicar na linha mostra o talhao no mapa (e as opcoes do contorno); o botao "Abrir" leva direto a
 * pagina dele. Antes so havia a selecao, e "abrir" era um segundo botao que aparecia longe, no pe
 * da lista.
 */
export function FieldListItem({ field, isSelected, cultivation, onSelect, onOpen }: FieldListItemProps) {
  return (
    <ListItem
      disablePadding
      sx={{ mb: 0.5 }}
      secondaryAction={
        <Button size="small" onClick={() => onOpen(field.id)}>Abrir</Button>
      }
    >
      <ListItemButton
        selected={isSelected}
        onClick={() => onSelect(field.id)}
        sx={{ borderRadius: 1, alignItems: 'flex-start', flexDirection: 'column', gap: 0.5, pr: 10 }}
      >
        <Stack direction="row" sx={{ width: '100%', gap: 1, alignItems: 'center' }}>
          <Typography variant="subtitle2" noWrap>
            {field.name}
          </Typography>
          {!field.active && <Chip label="Inativo" size="small" />}
        </Stack>

        <Box>
          <Typography variant="body2" color="text.secondary">
            {formatHectares(field.areaHectares)}
            {cultivation ? ` · ${cultivation}` : ' · nada plantado'}
          </Typography>
        </Box>
      </ListItemButton>
    </ListItem>
  );
}
