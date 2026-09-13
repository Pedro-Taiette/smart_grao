import { useCallback, useState } from 'react';
import type { SamplingMode } from '@/api/generated/model/samplingMode';
import type { SamplingPlanSummaryViewModel } from '@/api/generated/model/samplingPlanSummaryViewModel';
import { useGenerateSamplingPlan } from './useGenerateSamplingPlan';
import { useRemoveSamplingPlan } from './useRemoveSamplingPlan';
import { useSamplingPlan } from './useSamplingPlan';
import { useSamplingPlans } from './useSamplingPlans';

/**
 * Toda a interacao da amostragem de um talhao num lugar so, na mesma linha do
 * {@link import('@/features/fields/hooks/useFieldsWorkspace').useFieldsWorkspace}: qual plano esta
 * visivel no mapa, o dialogo de marcacao e a confirmacao de exclusao.
 *
 * Existe para que a pagina de talhoes continue sendo layout depois de ganhar a amostragem.
 */
export function useSamplingWorkspace(fieldId: string | null) {
  const { plans, latestPlan, refetch } = useSamplingPlans(fieldId);
  const { generatePlan, isGenerating } = useGenerateSamplingPlan();
  const { removePlan, isRemoving } = useRemoveSamplingPlan();

  // Guarda apenas a escolha explicita — o produtor clicando numa marcacao anterior. O que o mapa
  // mostra e derivado logo abaixo, e nao copiado para dentro de um estado: copiar exigiria um efeito
  // para manter a copia em dia, e e assim que o mapa acaba mostrando os pontos do talhao anterior.
  const [chosenPlanId, setChosenPlanId] = useState<string | null>(null);
  const [isDialogOpen, setDialogOpen] = useState(false);
  const [pendingDeletion, setPendingDeletion] = useState<SamplingPlanSummaryViewModel | null>(null);

  // Trocar de talhao descarta a escolha, que era sobre outro talhao. Ajuste durante a renderizacao,
  // e nao num efeito: o React reexecuta o componente antes de pintar a tela, entao nao chega a
  // existir um quadro mostrando a marcacao errada.
  const [lastFieldId, setLastFieldId] = useState(fieldId);
  if (fieldId !== lastFieldId) {
    setLastFieldId(fieldId);
    setChosenPlanId(null);
  }

  // Sem escolha explicita, vale a marcacao mais recente — que e o que o produtor espera ver ao abrir
  // um talhao, e tambem logo depois de marcar pontos novos.
  const visiblePlanId = chosenPlanId ?? latestPlan?.id ?? null;

  const { points } = useSamplingPlan(visiblePlanId);
  const visiblePlan = plans.find((plan) => plan.id === visiblePlanId) ?? null;

  const openDialog = useCallback(() => setDialogOpen(true), []);
  const closeDialog = useCallback(() => setDialogOpen(false), []);

  const confirmGeneration = useCallback(
    async (mode: SamplingMode, spacingMeters: number) => {
      if (!fieldId) return;

      const plan = await generatePlan({ fieldId, mode, spacingMeters });
      if (plan) {
        // Fixa a marcacao recem-criada em vez de esperar a lista recarregar: ate a consulta voltar,
        // "a mais recente" ainda seria a anterior, e o mapa piscaria a malha antiga.
        setChosenPlanId(plan.plan.id);
        setDialogOpen(false);
      }
    },
    [fieldId, generatePlan],
  );

  const confirmDeletion = useCallback(async () => {
    if (!pendingDeletion || !fieldId) return;

    const removed = await removePlan(pendingDeletion.id, fieldId);
    if (removed) {
      setPendingDeletion(null);
      // Solta a escolha: a visivel volta a ser a mais recente das que sobraram.
      setChosenPlanId(null);
      refetch();
    }
  }, [fieldId, pendingDeletion, refetch, removePlan]);

  return {
    plans,
    points,
    visiblePlanId,
    isVisiblePlanOutdated: visiblePlan?.isOutdated ?? false,
    selectPlan: setChosenPlanId,

    isDialogOpen,
    openDialog,
    closeDialog,
    confirmGeneration,

    pendingDeletion,
    requestDeletion: setPendingDeletion,
    confirmDeletion,

    isBusy: isGenerating || isRemoving,
    isGenerating,
  };
}
