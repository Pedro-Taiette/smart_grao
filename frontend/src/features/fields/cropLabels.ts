import { Crop } from '@/api/generated/model/crop';

/**
 * Nome de cada cultura em portugues.
 *
 * O `Record<Crop, string>` e proposital: se o backend acrescentar uma cultura ao enum, a proxima
 * geracao do cliente quebra a compilacao aqui em vez de mostrar "Sorghum" ao produtor.
 */
export const cropLabels: Record<Crop, string> = {
  [Crop.Undefined]: 'Não definida',
  [Crop.Soybean]: 'Soja',
  [Crop.Corn]: 'Milho',
  [Crop.Cotton]: 'Algodão',
  [Crop.Coffee]: 'Café',
  [Crop.Sugarcane]: 'Cana-de-açúcar',
  [Crop.Wheat]: 'Trigo',
  [Crop.Beans]: 'Feijão',
  [Crop.Rice]: 'Arroz',
  [Crop.Sorghum]: 'Sorgo',
  [Crop.Other]: 'Outra',
};

/** Opcoes na ordem em que fazem sentido no seletor: as culturas mais comuns primeiro. */
export const cropOptions = [
  Crop.Soybean,
  Crop.Corn,
  Crop.Cotton,
  Crop.Coffee,
  Crop.Sugarcane,
  Crop.Wheat,
  Crop.Beans,
  Crop.Rice,
  Crop.Sorghum,
  Crop.Other,
  Crop.Undefined,
] as const;
