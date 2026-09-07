import { z } from 'zod';
import type { SaveFarmViewModel } from '@/api/generated/model/saveFarmViewModel';

/**
 * Regras de preenchimento do formulario de fazenda.
 *
 * Duplicam de proposito o que o backend valida: aqui elas existem para dar resposta imediata, sem
 * ida ao servidor. O backend continua sendo a autoridade — este schema nunca e a unica barreira.
 */
export const farmSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome da fazenda.').max(120, 'Nome longo demais.'),
  city: z.string().trim().min(1, 'Informe o município.').max(120, 'Nome longo demais.'),
  state: z
    .string()
    .trim()
    .length(2, 'A UF deve ter duas letras.')
    .transform((value) => value.toUpperCase()),
  // Sede opcional: aceita os dois campos vazios ou os dois preenchidos, nunca metade.
  latitude: z
    .union([z.coerce.number().min(-90).max(90), z.literal('')])
    .optional(),
  longitude: z
    .union([z.coerce.number().min(-180).max(180), z.literal('')])
    .optional(),
});

export type FarmFormValues = z.input<typeof farmSchema>;

export const emptyFarmForm: FarmFormValues = {
  name: '',
  city: '',
  state: '',
  latitude: '',
  longitude: '',
};

/** Converte o que o formulario carrega para o corpo que a API espera. */
export function toSaveFarmPayload(values: z.output<typeof farmSchema>): SaveFarmViewModel {
  const hasHeadquarters =
    typeof values.latitude === 'number' && typeof values.longitude === 'number';

  return {
    name: values.name,
    city: values.city,
    state: values.state,
    headquarters: hasHeadquarters
      ? { latitude: values.latitude as number, longitude: values.longitude as number }
      : null,
  };
}
