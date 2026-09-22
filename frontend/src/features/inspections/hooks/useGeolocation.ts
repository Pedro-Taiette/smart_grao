import { useState } from 'react';

/**
 * Pega a posicao do aparelho.
 *
 * Existe para que ninguem digite coordenada em campo: a pessoa aperta um botao e o GPS responde.
 * Digitar latitude e longitude a mao, de bota no barro, e a forma mais rapida de gravar uma parada
 * no lugar errado — e uma troca de sinal poe o ponto no outro hemisferio.
 *
 * A precisao vem junto porque e ela que distingue um desvio real da malha de um erro do aparelho.
 */
export interface DevicePosition {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
}

export function useGeolocation() {
  const [isLocating, setLocating] = useState(false);

  function locate(): Promise<DevicePosition | null> {
    if (!('geolocation' in navigator)) return Promise.resolve(null);

    setLocating(true);
    return new Promise(resolve => {
      navigator.geolocation.getCurrentPosition(
        position => {
          setLocating(false);
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracyMeters: position.coords.accuracy,
          });
        },
        () => { setLocating(false); resolve(null); },
        // Sem cache de posicao: a parada anterior foi noutro lugar, e uma leitura guardada poria
        // esta observacao no ponto errado.
        { enableHighAccuracy: true, timeout: 15_000, maximumAge: 0 },
      );
    });
  }

  return { locate, isLocating };
}
