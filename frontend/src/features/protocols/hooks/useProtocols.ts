import { useState } from 'react';
import { useGetProtocols } from '@/api/generated/protocols/protocols';
import { Crop } from '@/api/generated/model/crop';
import type { ProtocolSummaryViewModel } from '@/api/generated/model/protocolSummaryViewModel';

/** Uma familia de versoes do mesmo `code`, da mais recente para a mais antiga. */
export interface ProtocolFamily {
  code: string;
  name: string;
  versions: ProtocolSummaryViewModel[];
}

export function useProtocols() {
  const [crop, setCrop] = useState<Crop>(Crop.Corn);
  const query = useGetProtocols({ crop });
  const protocols = query.data ?? [];

  // O backend ja devolve ordenado por codigo e versao decrescente; aqui so se agrupa, para que a
  // tela mostre um protocolo com o seu historico de versoes em vez de uma lista solta de linhas.
  const families: ProtocolFamily[] = [];
  for (const protocol of protocols) {
    const family = families.find((candidate) => candidate.code === protocol.code);
    if (family) family.versions.push(protocol);
    else families.push({ code: protocol.code, name: protocol.name, versions: [protocol] });
  }

  return {
    families, crop, setCrop,
    isLoading: query.isPending,
    error: query.error,
    refetch: () => { void query.refetch(); },
  };
}
