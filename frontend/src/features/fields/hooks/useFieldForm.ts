import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import { emptyFieldForm, fieldSchema, type FieldFormValues } from '../fieldSchema';

/**
 * Estado e validacao do formulario de talhao.
 *
 * Recarrega ao abrir, para que editar o segundo talhao nao mostre os dados do primeiro.
 */
export function useFieldForm(field: FieldViewModel | null, isOpen: boolean) {
  const form = useForm<FieldFormValues>({
    resolver: zodResolver(fieldSchema),
    defaultValues: emptyFieldForm,
    mode: 'onBlur',
  });

  const { reset } = form;

  useEffect(() => {
    if (!isOpen) return;
    reset(field ? { name: field.name, crop: field.crop } : emptyFieldForm);
  }, [field, isOpen, reset]);

  return form;
}
