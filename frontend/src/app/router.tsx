import { createBrowserRouter, Navigate } from 'react-router-dom';
import { FarmsPage } from '@/features/farms/pages/FarmsPage';
import { FieldsPage } from '@/features/fields/pages/FieldsPage';
import { AppLayout } from '@/shared/components/AppLayout';

/**
 * Rotas da aplicacao.
 *
 * O id da fazenda esta na URL, e nao num estado global: assim a tela do mapa e compartilhavel por
 * link e sobrevive a um F5 — que e o que acontece o tempo todo enquanto se desenha um talhao.
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/farms" replace /> },
      { path: 'farms', element: <FarmsPage /> },
      { path: 'farms/:farmId/fields', element: <FieldsPage /> },
      { path: '*', element: <Navigate to="/farms" replace /> },
    ],
  },
]);
