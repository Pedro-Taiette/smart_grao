import { useQueryClient } from '@tanstack/react-query';
import { getGetSamplingPlansByFieldQueryKey } from '@/api/generated/fields/fields';
import { useGenerateSamplingPlan as useGenerateSamplingPlanMutation } from '@/api/generated/sampling-plans/sampling-plans';
import type { SamplingMode } from '@/api/generated/model/samplingMode';
import type { SamplingPlanViewModel } from '@/api/generated/model/samplingPlanViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface GeneratePlanInput {
  fieldId: string;
  mode: SamplingMode;
  /** So no Mapeamento. No Monitoramento a densidade vem da tabela do MIP-Soja. */
  spacingMeters?: number;
}

export interface UseGenerateSamplingPlanResult {
  generatePlan: (input: GeneratePlanInput) => Promise<SamplingPlanViewModel | null>;
  isGenerating: boolean;
}

/**
 * Gera a malha de amostragem de um talhao.
 *
 * O espacamento e enviado como `null` no Monitoramento porque a API o recusa nesse modo — la a
 * densidade vem da tabela do MIP-Soja, e aceitar um valor sem efeito faria parecer que o produtor
 * escolheu algo que nao escolheu.
 */
export function useGenerateSamplingPlan(): UseGenerateSamplingPlanResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();
  const mutation = useGenerateSamplingPlanMutation();

  const generatePlan = async ({ fieldId, mode, spacingMeters }: GeneratePlanInput) => {
    try {
      const plan = await mutation.mutateAsync({
        data: { fieldId, mode, spacingMeters: mode === 'Mapping' ? (spacingMeters ?? null) : null },
      });

      await queryClient.invalidateQueries({
        queryKey: getGetSamplingPlansByFieldQueryKey(fieldId),
      });

      notifySuccess('Pontos de amostragem prontos.');
      return plan as SamplingPlanViewModel;
    } catch (error) {
      notifyError(error);
      return null;
    }
  };

  return { generatePlan, isGenerating: mutation.isPending };
}
