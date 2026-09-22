import { z } from 'zod';
import { AutomationCapability } from '@/api/generated/model/automationCapability';
import { CountUnit } from '@/api/generated/model/countUnit';
import { Crop } from '@/api/generated/model/crop';
import { PlantOrgan } from '@/api/generated/model/plantOrgan';
import { TargetKind } from '@/api/generated/model/targetKind';

/**
 * O codigo e a chave estavel que vai rotular imagem e resultado de modelo mais adiante, entao a
 * validacao e a mesma do dominio: minusculas, numeros e sublinhado interno.
 */
const stableCode = z
  .string()
  .trim()
  .min(1, 'Informe o código.')
  .max(60, 'O código aceita até 60 caracteres.')
  .regex(/^[a-z0-9]+(_[a-z0-9]+)*$/, 'Use letras minúsculas, números e sublinhado. Ex.: cercospora_zeina.');

export const targetSchema = z.object({
  code: stableCode,
  commonName: z.string().trim().min(1, 'Informe o nome comum.').max(120),
  scientificName: z.string().trim().min(1, 'Informe o nome científico.').max(120),
  kind: z.enum(TargetKind).refine((value): boolean => value !== TargetKind.Undefined, 'Escolha praga ou doença foliar.'),
  crop: z.enum(Crop).refine((value): boolean => value !== Crop.Undefined, 'Selecione uma cultura.'),
});

export const automationSchema = z.object({ automation: z.enum(AutomationCapability) });

export const protocolSchema = z.object({
  code: stableCode,
  crop: z.enum(Crop).refine((value): boolean => value !== Crop.Undefined, 'Selecione uma cultura.'),
  name: z.string().trim().min(1, 'Informe o nome do protocolo.').max(120),
});

export const protocolNameSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do protocolo.').max(120),
});

/**
 * O alvo dentro do protocolo.
 *
 * Orgao e nivel de referencia sao condicionais, e a condicao e a unidade: a armadilha nao observa
 * parte de planta, e o registro de presenca nao tem limiar. O formulario esconde o campo, e o
 * `superRefine` garante que uma troca de unidade com o campo ja preenchido nao passe adiante.
 *
 * `referenceThreshold` e `referenceSource` andam juntos de proposito — um numero agronomico sem
 * procedencia nao e melhor que numero nenhum.
 */
export const protocolItemSchema = z
  .object({
    targetId: z.uuid('Selecione um alvo do catálogo.'),
    unit: z.enum(CountUnit).refine((value): boolean => value !== CountUnit.Undefined, 'Escolha o que será contado.'),
    organ: z.union([z.enum(PlantOrgan), z.literal('')]),
    photosRequested: z.coerce.number<number>().int().min(0).max(5),
    instructions: z.string().trim().min(1, 'Descreva como coletar.').max(2000),
    referenceThreshold: z.string().trim(),
    referenceSource: z.string().trim().max(300),
  })
  .superRefine((values, context) => {
    const needsOrgan = values.unit !== CountUnit.InsectsPerTrap;
    if (needsOrgan && (values.organ === '' || values.organ === PlantOrgan.Undefined)) {
      context.addIssue({ code: 'custom', path: ['organ'], message: 'Informe a parte da planta observada.' });
    }

    if (values.referenceThreshold === '') {
      if (values.referenceSource !== '') {
        context.addIssue({ code: 'custom', path: ['referenceThreshold'], message: 'Informe o nível ou apague a fonte.' });
      }
      return;
    }

    if (values.unit === CountUnit.Presence) {
      context.addIssue({
        code: 'custom', path: ['referenceThreshold'],
        message: 'O registro de presença não tem nível: a presença já justifica a ação.',
      });
      return;
    }

    const threshold = Number(values.referenceThreshold.replace(',', '.'));
    if (!Number.isFinite(threshold) || threshold <= 0) {
      context.addIssue({ code: 'custom', path: ['referenceThreshold'], message: 'Informe um número maior que zero.' });
      return;
    }

    const isPercentage =
      values.unit === CountUnit.AttackedPlantPercentage ||
      values.unit === CountUnit.LesionedLeafAreaPercentage;
    if (isPercentage && threshold > 100) {
      context.addIssue({ code: 'custom', path: ['referenceThreshold'], message: 'Um percentual vai até 100.' });
    }
    if (values.unit === CountUnit.SeverityScore1To9 && (threshold < 1 || threshold > 9)) {
      context.addIssue({ code: 'custom', path: ['referenceThreshold'], message: 'A nota vai de 1 a 9.' });
    }

    if (values.referenceSource === '') {
      context.addIssue({
        code: 'custom', path: ['referenceSource'],
        message: 'Todo nível precisa da fonte de onde veio.',
      });
    }
  });

export type TargetValues = z.infer<typeof targetSchema>;
export type AutomationValues = z.infer<typeof automationSchema>;
export type ProtocolValues = z.infer<typeof protocolSchema>;
export type ProtocolNameValues = z.infer<typeof protocolNameSchema>;
export type ProtocolItemValues = z.infer<typeof protocolItemSchema>;
