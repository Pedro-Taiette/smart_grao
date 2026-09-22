import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AutomationCapability } from '@/api/generated/model/automationCapability';
import { CountUnit } from '@/api/generated/model/countUnit';
import { Crop } from '@/api/generated/model/crop';
import { TargetKind } from '@/api/generated/model/targetKind';
import {
  targetSchema, automationSchema, protocolSchema, protocolNameSchema, protocolItemSchema,
  type TargetValues, type AutomationValues, type ProtocolValues, type ProtocolNameValues,
  type ProtocolItemValues,
} from '../protocolSchema';

export function useTargetForm(crop: Crop) {
  return useForm<TargetValues>({
    resolver: zodResolver(targetSchema),
    defaultValues: { code: '', commonName: '', scientificName: '', kind: TargetKind.Pest, crop },
  });
}

export function useAutomationForm(current: AutomationCapability) {
  return useForm<AutomationValues>({
    resolver: zodResolver(automationSchema), defaultValues: { automation: current },
  });
}

export function useProtocolForm(crop: Crop) {
  return useForm<ProtocolValues>({
    resolver: zodResolver(protocolSchema), defaultValues: { code: '', crop, name: '' },
  });
}

export function useProtocolNameForm(name: string) {
  return useForm<ProtocolNameValues>({
    resolver: zodResolver(protocolNameSchema), defaultValues: { name },
  });
}

export function useProtocolItemForm(targetId: string) {
  return useForm<ProtocolItemValues>({
    resolver: zodResolver(protocolItemSchema),
    // Uma foto ja vem marcada: a foto e a matéria-prima das fases seguintes, e deixar o padrao em
    // zero faria o protocolo nascer sem imagem nenhuma por omissao.
    defaultValues: {
      targetId, unit: CountUnit.AttackedPlantPercentage, organ: '', photosRequested: 1,
      instructions: '', referenceThreshold: '', referenceSource: '',
    },
  });
}
