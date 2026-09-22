import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from '@mui/material';
import GrassIcon from '@mui/icons-material/Grass';
import { NavLink, Outlet, useLocation } from 'react-router-dom';

/**
 * As tres portas de entrada. Fazendas leva ao trabalho de cada propriedade; catalogo e protocolos
 * sao dados de referencia do produto, iguais para todas elas — por isso estao lado a lado aqui, e
 * nao dentro de uma fazenda.
 */
const sections = [
  { to: '/farms', label: 'Fazendas' },
  { to: '/targets', label: 'Catálogo' },
  { to: '/protocols', label: 'Protocolos' },
];

/**
 * Moldura de todas as telas. O `Container` fica fora do `Outlet` para que cada pagina cuide so do
 * proprio conteudo — a tela do mapa, que precisa ocupar a altura inteira, sai dessa restricao
 * usando o proprio layout.
 */
export function AppLayout() {
  const { pathname } = useLocation();

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <AppBar position="static" color="primary">
        <Toolbar>
          <GrassIcon sx={{ mr: 1.5 }} />
          <Typography variant="h6" component="h1" sx={{ mr: 3 }}>
            SmartGrão
          </Typography>

          <Stack direction="row" spacing={1}>
            {sections.map((section) => (
              <Button
                key={section.to}
                component={NavLink}
                to={section.to}
                color="inherit"
                // Sublinha a secao aberta, inclusive nas telas internas dela — a de um protocolo
                // continua sendo "Protocolos".
                sx={{
                  textDecoration: pathname.startsWith(section.to) ? 'underline' : 'none',
                  textUnderlineOffset: 6,
                }}
              >
                {section.label}
              </Button>
            ))}
          </Stack>
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
