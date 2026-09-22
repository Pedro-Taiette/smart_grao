import { createBrowserRouter, Navigate } from 'react-router-dom';
import { FarmsPage } from '@/features/farms/pages/FarmsPage';
import { FieldsPage } from '@/features/fields/pages/FieldsPage';
import { CultivationsPage } from '@/features/cultivations/pages/CultivationsPage';
import { TargetCatalogPage } from '@/features/protocols/pages/TargetCatalogPage';
import { ProtocolsPage } from '@/features/protocols/pages/ProtocolsPage';
import { ProtocolDetailPage } from '@/features/protocols/pages/ProtocolDetailPage';
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
      { path: 'farms/:farmId/fields/:fieldId/cultivations', element: <CultivationsPage /> },
      // Catalogo e protocolos sao dados de referencia do produto, nao de uma fazenda: por isso
      // ficam na raiz, e nao debaixo de `/farms/:farmId`.
      { path: 'targets', element: <TargetCatalogPage /> },
      { path: 'protocols', element: <ProtocolsPage /> },
      { path: 'protocols/:protocolId', element: <ProtocolDetailPage /> },
      { path: '*', element: <Navigate to="/farms" replace /> },
    ],
  },
]);
