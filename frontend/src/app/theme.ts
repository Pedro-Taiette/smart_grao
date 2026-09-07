import { createTheme } from '@mui/material/styles';
import { ptBR } from '@mui/material/locale';

/**
 * Tema unico do app. Cores, raio de borda e densidade vivem aqui — nenhum componente define
 * `sx={{ color: '#2e7d32' }}`, senao trocar a identidade visual vira uma caca ao tesouro.
 */
export const theme = createTheme(
  {
    palette: {
      primary: { main: '#2e7d32' },
      secondary: { main: '#6d4c41' },
      background: { default: '#f6f7f4' },
    },
    shape: { borderRadius: 10 },
    typography: {
      fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
      h5: { fontWeight: 600 },
      h6: { fontWeight: 600 },
    },
    components: {
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: { root: { textTransform: 'none', fontWeight: 600 } },
      },
      MuiPaper: { defaultProps: { elevation: 0 }, styleOverrides: { root: { border: '1px solid #e3e6df' } } },
      MuiTextField: { defaultProps: { size: 'small', fullWidth: true } },
    },
  },
  // Traduz os textos internos do MUI (paginacao, autocomplete, dialogs).
  ptBR,
);
