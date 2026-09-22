import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RedirectToField, RedirectToInspection, RedirectToProtocol } from './legacyRedirects';
import { TodayPage } from '@/features/today/pages/TodayPage';
import { FarmsPage } from '@/features/farms/pages/FarmsPage';
import { FieldsPage } from '@/features/fields/pages/FieldsPage';
import { FieldPage } from '@/features/fields/pages/FieldPage';
import { TargetCatalogPage } from '@/features/protocols/pages/TargetCatalogPage';
import { ProtocolsPage } from '@/features/protocols/pages/ProtocolsPage';
import { ProtocolDetailPage } from '@/features/protocols/pages/ProtocolDetailPage';
import { TeamPage } from '@/features/inspections/pages/TeamPage';
import { InspectionsPage } from '@/features/inspections/pages/InspectionsPage';
import { InspectionDetailPage } from '@/features/inspections/pages/InspectionDetailPage';
import { SettingsPage } from '@/features/settings/pages/SettingsPage';
import { AppLayout } from '@/shared/components/AppLayout';

/**
 * Rotas da aplicacao.
 *
 * Tres destinos de trabalho na raiz — Hoje, Talhoes, Vistorias — e um porao de cadastro em
 * `/ajustes`. A fazenda saiu da URL e virou contexto: ver `features/farms/FarmContext.tsx` para o
 * que se ganhou e o que se perdeu nessa troca.
 *
 * O que sobrou de id na rota e id de registro, nao de navegacao: `/talhoes/:fieldId` e
 * `/vistorias/:id` continuam compartilhaveis por link e sobrevivem a um F5 — que e o que acontece
 * o tempo todo enquanto se desenha um talhao, e o unico jeito de mandar uma visita para o celular
 * de quem vai a campo.
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <TodayPage /> },

      { path: 'talhoes', element: <FieldsPage /> },
      { path: 'talhoes/:fieldId', element: <FieldPage /> },

      { path: 'vistorias', element: <InspectionsPage /> },
      { path: 'vistorias/:inspectionId', element: <InspectionDetailPage /> },

      // Dados de referencia e cadastro de implantacao: usados uma vez, no comeco, e raramente
      // depois. Ficam a um clique, fora do caminho do trabalho do dia.
      { path: 'ajustes', element: <SettingsPage /> },
      { path: 'ajustes/fazendas', element: <FarmsPage /> },
      { path: 'ajustes/equipe', element: <TeamPage /> },
      { path: 'ajustes/catalogo', element: <TargetCatalogPage /> },
      { path: 'ajustes/protocolos', element: <ProtocolsPage /> },
      { path: 'ajustes/protocolos/:protocolId', element: <ProtocolDetailPage /> },

      // Rotas da organizacao anterior. Ficam como redirecionamento porque links ja foram abertos e
      // salvos — em especial o da execucao de vistoria, que circula por mensagem.
      { path: 'farms', element: <Navigate to="/ajustes/fazendas" replace /> },
      { path: 'farms/:farmId/team', element: <Navigate to="/ajustes/equipe" replace /> },
      { path: 'farms/:farmId/fields', element: <Navigate to="/talhoes" replace /> },
      { path: 'farms/:farmId/fields/:fieldId/cultivations', element: <RedirectToField tab="cultivo" /> },
      { path: 'farms/:farmId/fields/:fieldId/inspections', element: <RedirectToField tab="vistorias" /> },
      { path: 'inspections/:inspectionId', element: <RedirectToInspection /> },
      { path: 'targets', element: <Navigate to="/ajustes/catalogo" replace /> },
      { path: 'protocols', element: <Navigate to="/ajustes/protocolos" replace /> },
      { path: 'protocols/:protocolId', element: <RedirectToProtocol /> },

      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
]);
