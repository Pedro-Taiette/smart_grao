import { z } from 'zod';

/**
 * O formulario de talhao cobre apenas nome.
 *
 * O contorno nao esta aqui de proposito: ele nao e digitado, e desenhado no mapa. Trata-lo como
 * campo de formulario levaria a validar geometria com zod — trabalho que o backend ja faz melhor,
 * e que aqui so produziria uma segunda versao das regras para divergir da primeira.
 */
export const fieldSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do talhão.').max(80, 'Nome longo demais.'),
});

export type FieldFormValues = z.infer<typeof fieldSchema>;

export const emptyFieldForm: FieldFormValues = {
  name: '',
};
