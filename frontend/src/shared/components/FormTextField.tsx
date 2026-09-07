import { TextField, type TextFieldProps } from '@mui/material';
import { Controller, type Control, type FieldValues, type Path } from 'react-hook-form';

type FormTextFieldProps<T extends FieldValues> = {
  name: Path<T>;
  control: Control<T>;
  label: string;
  // `helperText` continua disponivel: serve como dica do campo, e a mensagem de validacao a
  // substitui quando existe.
} & Omit<TextFieldProps, 'name' | 'error' | 'value' | 'onChange' | 'ref'>;

/**
 * Campo de texto ligado ao react-hook-form.
 *
 * Existe para que o formulario nao repita, campo a campo, a mesma ligacao entre `Controller`,
 * `error` e `helperText` — a repeticao que faz um campo novo nascer sem exibir a mensagem de
 * validacao porque alguem esqueceu de copiar uma linha.
 */
export function FormTextField<T extends FieldValues>({
  name,
  control,
  label,
  ...textFieldProps
}: FormTextFieldProps<T>) {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...textFieldProps}
          {...field}
          value={field.value ?? ''}
          label={label}
          error={Boolean(fieldState.error)}
          helperText={fieldState.error?.message ?? textFieldProps.helperText}
        />
      )}
    />
  );
}
