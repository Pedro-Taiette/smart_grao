import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  seasonSchema, cultivationSchema, stageSchema, closureSchema, today,
  type SeasonValues, type CultivationValues, type StageValues, type ClosureValues,
} from '../cultivationSchema';

export function useSeasonForm() {
  return useForm<SeasonValues>({ resolver: zodResolver(seasonSchema), defaultValues: { name: '' } });
}
export function useCultivationForm(seasonId: string) {
  return useForm<CultivationValues>({
    resolver: zodResolver(cultivationSchema),
    defaultValues: { seasonId, crop: 'Corn', cultivar: '', plantedOn: today() },
  });
}
export function useStageForm() {
  return useForm<StageValues>({
    resolver: zodResolver(stageSchema), defaultValues: { observedOn: today(), stage: '', notes: '' },
  });
}
export function useClosureForm() {
  return useForm<ClosureValues>({ resolver: zodResolver(closureSchema), defaultValues: { endedOn: today() } });
}
