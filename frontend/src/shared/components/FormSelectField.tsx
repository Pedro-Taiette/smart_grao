import { MenuItem, TextField } from '@mui/material';
import { Controller, type Control, type FieldValues, type Path } from 'react-hook-form';

interface Option<TValue extends string> {
  value: TValue;
  label: string;
}

interface FormSelectFieldProps<T extends FieldValues, TValue extends string> {
  name: Path<T>;
  control: Control<T>;
  label: string;
  options: readonly Option<TValue>[];
}

/** Seletor ligado ao react-hook-form, com a mesma ligacao de erro do {@link FormTextField}. */
export function FormSelectField<T extends FieldValues, TValue extends string>({
  name,
  control,
  label,
  options,
}: FormSelectFieldProps<T, TValue>) {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          value={field.value ?? ''}
          select
          label={label}
          error={Boolean(fieldState.error)}
          helperText={fieldState.error?.message}
        >
          {options.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
      )}
    />
  );
}
