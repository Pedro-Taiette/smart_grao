import { useQueryClient } from '@tanstack/react-query';
import { getGetFieldsByFarmQueryKey } from '@/api/generated/farms/farms';
import { getGetFieldByIdQueryKey, useCreateField, useUpdateField } from '@/api/generated/fields/fields';
import type { Crop } from '@/api/generated/model/crop';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface SaveFieldInput {
  /** Ausente cadastra; presente atualiza. */
  fieldId?: string;
  farmId: string;
  name: string;
  crop: Crop;
  boundary: GeoJsonPolygon;
}

export interface UseSaveFieldResult {
  saveField: (input: SaveFieldInput) => Promise<FieldViewModel | null>;
  isSaving: boolean;
}

/**
 * Cadastra ou atualiza um talhao.
 *
 * O contorno sempre viaja junto, mesmo quando so o nome mudou: o endpoint de atualizacao recebe o
 * talhao inteiro, e mandar o contorno atual e mais barato do que manter dois caminhos de escrita
 * que podem divergir.
 *
 * Nao ha tratamento especial para sobreposicao ou nome repetido — o backend responde 409 com um
 * codigo, o catalogo traduz, e o produtor le uma frase que diz o que houve.
 */
export function useSaveField(): UseSaveFieldResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();

  const createMutation = useCreateField();
  const updateMutation = useUpdateField();

  const saveField = async ({ fieldId, farmId, name, crop, boundary }: SaveFieldInput) => {
    try {
      const saved = fieldId
        ? await updateMutation.mutateAsync({ id: fieldId, data: { name, crop, boundary } })
        : await createMutation.mutateAsync({ data: { farmId, name, crop, boundary } });

      await queryClient.invalidateQueries({ queryKey: getGetFieldsByFarmQueryKey(farmId) });
      if (fieldId) {
        await queryClient.invalidateQueries({ queryKey: getGetFieldByIdQueryKey(fieldId) });
      }

      notifySuccess(fieldId ? 'Talhão atualizado.' : 'Talhão cadastrado.');
      return saved as FieldViewModel;
    } catch (error) {
      notifyError(error);
      return null;
    }
  };

  return {
    saveField,
    isSaving: createMutation.isPending || updateMutation.isPending,
  };
}
