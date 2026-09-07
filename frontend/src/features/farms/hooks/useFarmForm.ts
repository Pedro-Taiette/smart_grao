import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';
import { emptyFarmForm, farmSchema, type FarmFormValues } from '../farmSchema';

/**
 * Toda a logica do formulario de fazenda: validacao, valores iniciais e recarga ao trocar de
 * registro.
 *
 * Fica separado do componente para que o `FarmFormDialog` seja apenas marcacao. O `reset` no
 * `useEffect` e o detalhe que costuma faltar: sem ele, abrir o dialogo para editar a segunda
 * fazenda mostraria os dados da primeira, porque o formulario so le os valores iniciais na
 * montagem.
 */
export function useFarmForm(farm: FarmViewModel | null, isOpen: boolean) {
  const form = useForm<FarmFormValues>({
    resolver: zodResolver(farmSchema),
    defaultValues: emptyFarmForm,
    mode: 'onBlur',
  });

  const { reset } = form;

  useEffect(() => {
    if (!isOpen) return;

    reset(
      farm
        ? {
            name: farm.name,
            city: farm.city,
            state: farm.state,
            latitude: farm.headquarters?.latitude ?? '',
            longitude: farm.headquarters?.longitude ?? '',
          }
        : emptyFarmForm,
    );
  }, [farm, isOpen, reset]);

  return form;
}
