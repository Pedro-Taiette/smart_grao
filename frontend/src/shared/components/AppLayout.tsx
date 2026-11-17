import {
  AppBar, BottomNavigation, BottomNavigationAction, Box, Button, Container, Paper, Stack, Toolbar,
  Typography, useMediaQuery, useTheme,
} from '@mui/material';
import GrassIcon from '@mui/icons-material/Grass';
import TodayIcon from '@mui/icons-material/Today';
import MapIcon from '@mui/icons-material/Map';
import FactCheckIcon from '@mui/icons-material/FactCheck';
import SettingsIcon from '@mui/icons-material/Settings';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { FarmSelector } from './FarmSelector';

/**
 * Os destinos do sistema, na ordem em que se pensa neles.
 *
 * Antes o menu era `Fazendas · Catalogo · Protocolos` — a estrutura do modelo de dados virada para
 * fora, com dois itens de tres que so se usam na implantacao. Aqui sao tarefas: o que fazer hoje,
 * onde e o que ja foi feito. Cadastro de referencia desceu para Ajustes, que continua a um clique
 * mas deixa de disputar a barra com o trabalho do dia.
 *
 * O aferimento do modelo tambem desceu para Ajustes: e a tela que mede a IA, util para a equipe e
 * para a banca, e nao uma tarefa de quem cuida da lavoura.
 */
const destinations = [
  { to: '/', label: 'Hoje', icon: <TodayIcon /> },
  { to: '/talhoes', label: 'Talhões', icon: <MapIcon /> },
  { to: '/vistorias', label: 'Vistorias', icon: <FactCheckIcon /> },
] as const;

const settings = { to: '/ajustes', label: 'Ajustes', icon: <SettingsIcon /> } as const;

/** A raiz so casa consigo mesma; as demais casam com as telas internas da secao. */
function isActive(pathname: string, to: string) {
  if (to === '/') return pathname === '/';
  // O aferimento mora fora de /ajustes na URL (links antigos), mas e um item de Ajustes.
  if (to === '/ajustes' && pathname.startsWith('/aferimento')) return true;
  return pathname.startsWith(to);
}

/**
 * Moldura de todas as telas.
 *
 * O `Container` fica fora do `Outlet` para que cada pagina cuide so do proprio conteudo — a tela do
 * mapa, que precisa ocupar a altura inteira, sai dessa restricao usando o proprio layout.
 */
export function AppLayout() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));

  // A navegacao inferior e a do polegar: no celular o alcance e a base da tela, nao o topo. No
  // desktop ela some e os mesmos destinos voltam para a barra.
  const current = [...destinations, settings].find((item) => isActive(pathname, item.to))?.to ?? false;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <AppBar position="static" color="primary">
        <Toolbar sx={{ gap: 2 }}>
          <GrassIcon />
          <Typography variant="h6" component="h1" sx={{ display: { xs: 'none', sm: 'block' } }}>
            SmartGrão
          </Typography>

          {!isCompact && (
            <Stack direction="row" spacing={1}>
              {destinations.map((item) => (
                <Button
                  key={item.to}
                  component={NavLink}
                  to={item.to}
                  color="inherit"
                  startIcon={item.icon}
                  sx={{
                    textDecoration: isActive(pathname, item.to) ? 'underline' : 'none',
                    textUnderlineOffset: 6,
                  }}
                >
                  {item.label}
                </Button>
              ))}
            </Stack>
          )}

          <Box sx={{ flex: 1 }} />
          <FarmSelector />

          {!isCompact && (
            <Button
              component={NavLink}
              to={settings.to}
              color="inherit"
              startIcon={settings.icon}
              sx={{
                textDecoration: isActive(pathname, settings.to) ? 'underline' : 'none',
                textUnderlineOffset: 6,
              }}
            >
              {settings.label}
            </Button>
          )}
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
        <Outlet />
      </Box>

      {isCompact && (
        <Paper square elevation={3} sx={{ flexShrink: 0 }}>
          <BottomNavigation
            showLabels
            value={current}
            onChange={(_, to: string) => navigate(to)}
          >
            {[...destinations, settings].map((item) => (
              <BottomNavigationAction
                key={item.to}
                value={item.to}
                label={item.label}
                icon={item.icon}
              />
            ))}
          </BottomNavigation>
        </Paper>
      )}
    </Box>
  );
}

/** Conteudo centralizado com respiro — para as telas que nao sao o mapa. */
export function PageContainer({ children }: { children: React.ReactNode }) {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, md: 4 } }}>
      {children}
    </Container>
  );
}

/**
 * Cabecalho padrao de pagina: titulo, uma linha de explicacao e a acao principal.
 *
 * Existia copiado em cinco telas com espacamentos levemente diferentes. Centralizar isso e metade
 * da sensacao de "sistema unico" em vez de "telas construidas em semanas diferentes".
 */
export function PageHeader({ title, description, action, back }: {
  title: string;
  description?: string;
  action?: React.ReactNode;
  /** Tela de dentro de outra: o caminho de volta fica visivel, em vez de depender do navegador. */
  back?: { to: string; label: string };
}) {
  const navigate = useNavigate();
  return (
    <>
    {back && (
      <Button startIcon={<ArrowBackIcon />} sx={{ mb: 1, ml: -1 }} onClick={() => navigate(back.to)}>
        {back.label}
      </Button>
    )}
    <Stack
      direction="row"
      sx={{ mb: 3, gap: 2, justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap' }}
    >
      <Box>
        <Typography variant="h5" component="h1">{title}</Typography>
        {description && (
          <Typography variant="body2" color="text.secondary">{description}</Typography>
        )}
      </Box>
      {action}
    </Stack>
    </>
  );
}
