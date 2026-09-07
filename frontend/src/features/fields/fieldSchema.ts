import { z } from 'zod';
import { Crop } from '@/api/generated/model/crop';

/**
 * O formulario de talhao cobre apenas nome e cultura.
 *
 * O contorno nao esta aqui de proposito: ele nao e digitado, e desenhado no mapa. Trata-lo como
 * campo de formulario levaria a validar geometria com zod — trabalho que o backend ja faz melhor,
 * e que aqui so produziria uma segunda versao das regras para divergir da primeira.
 */
export const fieldSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do talhão.').max(80, 'Nome longo demais.'),
  crop: z.enum(Crop),
});

export type FieldFormValues = z.infer<typeof fieldSchema>;

export const emptyFieldForm: FieldFormValues = {
  name: '',
  crop: Crop.Soybean,
};
