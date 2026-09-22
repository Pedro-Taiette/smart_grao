import { Box, Card, CardActionArea, Stack, Typography } from '@mui/material';
import AgricultureIcon from '@mui/icons-material/Agriculture';
import GroupsIcon from '@mui/icons-material/Groups';
import PestControlIcon from '@mui/icons-material/PestControl';
import ChecklistIcon from '@mui/icons-material/Checklist';
import { useNavigate } from 'react-router-dom';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';

/**
 * O porao do sistema: o que se cadastra na implantacao e quase nao se toca depois.
 *
 * Reunir isso numa tela so tem um motivo pratico — cada item aqui estava competindo com o trabalho
 * do dia na barra de navegacao. E tem um motivo de leitura: a descricao de cada cartao diz para que
 * serve o cadastro, que e a pergunta que ninguem conseguia responder olhando so o nome no menu.
 */
const entries = [
  {
    to: '/ajustes/fazendas',
    label: 'Fazendas',
    description: 'As propriedades. A sede posiciona o mapa sobre a sua terra.',
    icon: <AgricultureIcon fontSize="large" color="primary" />,
  },
  {
    to: '/ajustes/equipe',
    label: 'Equipe',
    description: 'Quem pode ser responsável por uma vistoria nesta fazenda.',
    icon: <GroupsIcon fontSize="large" color="primary" />,
  },
  {
    to: '/ajustes/protocolos',
    label: 'Protocolos',
    description: 'A receita da vistoria: o que observar em cada parada e em que unidade medir.',
    icon: <ChecklistIcon fontSize="large" color="primary" />,
  },
  {
    to: '/ajustes/catalogo',
    label: 'Pragas e doenças',
    description: 'O catálogo que acompanha o produto. Vale para qualquer lavoura de milho do país.',
    icon: <PestControlIcon fontSize="large" color="primary" />,
  },
] as const;

export function SettingsPage() {
  const navigate = useNavigate();

  return (
    <PageContainer>
      <PageHeader
        title="Ajustes"
        description="Cadastros de referência. Normalmente feitos uma vez, no começo."
      />

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' } }}>
        {entries.map((entry) => (
          <Card key={entry.to} variant="outlined">
            <CardActionArea sx={{ p: 2.5, height: '100%' }} onClick={() => navigate(entry.to)}>
              <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
                {entry.icon}
                <Box>
                  <Typography variant="h6">{entry.label}</Typography>
                  <Typography variant="body2" color="text.secondary">{entry.description}</Typography>
                </Box>
              </Stack>
            </CardActionArea>
          </Card>
        ))}
      </Box>
    </PageContainer>
  );
}
