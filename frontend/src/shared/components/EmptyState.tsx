import type { ReactNode } from 'react';
import { Box, Typography } from '@mui/material';

interface EmptyStateProps {
  title: string;
  description: string;
  action?: ReactNode;
}

/** Lista vazia com uma saida. Uma tela em branco nao diz ao usuario qual e o proximo passo. */
export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Box sx={{ textAlign: 'center', py: 6, px: 2 }}>
      <Typography variant="h6" gutterBottom>
        {title}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        {description}
      </Typography>
      {action}
    </Box>
  );
}
