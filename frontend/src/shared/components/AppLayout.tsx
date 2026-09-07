import { AppBar, Box, Container, Toolbar, Typography } from '@mui/material';
import GrassIcon from '@mui/icons-material/Grass';
import { Outlet } from 'react-router-dom';

/**
 * Moldura de todas as telas. O `Container` fica fora do `Outlet` para que cada pagina cuide so do
 * proprio conteudo — a tela do mapa, que precisa ocupar a altura inteira, sai dessa restricao
 * usando o proprio layout.
 */
export function AppLayout() {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <AppBar position="static" color="primary">
        <Toolbar>
          <GrassIcon sx={{ mr: 1.5 }} />
          <Typography variant="h6" component="h1">
            SmartGrão
          </Typography>
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
        <Outlet />
      </Box>
    </Box>
  );
}

/** Conteudo centralizado com respiro — para as telas que nao sao o mapa. */
export function PageContainer({ children }: { children: React.ReactNode }) {
  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {children}
    </Container>
  );
}
