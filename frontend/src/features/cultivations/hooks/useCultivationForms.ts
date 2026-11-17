import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { CercosporaRiskFactorsViewModel } from '@/api/generated/model/cercosporaRiskFactorsViewModel';
import { focusCrop } from '@/features/fields/cropLabels';
import {
  cultivationSchema, stageSchema, closureSchema, riskFactorsSchema, today,
  type CultivationValues, type StageValues, type ClosureValues, type RiskFactorsValues,
} from '../cultivationSchema';

export function useCultivationForm(riskFactors: CercosporaRiskFactorsViewModel) {
  return useForm<CultivationValues>({
    resolver: zodResolver(cultivationSchema),
    defaultValues: { crop: focusCrop, cultivar: '', plantedOn: today(), riskFactors },
  });
}
export function useRiskFactorsForm(riskFactors: CercosporaRiskFactorsViewModel) {
  return useForm<RiskFactorsValues>({ resolver: zodResolver(riskFactorsSchema), defaultValues: riskFactors });
}
export function useStageForm() {
  return useForm<StageValues>({
    resolver: zodResolver(stageSchema), defaultValues: { observedOn: today(), stage: '', notes: '' },
  });
}
export function useClosureForm() {
  return useForm<ClosureValues>({ resolver: zodResolver(closureSchema), defaultValues: { endedOn: today() } });
}
