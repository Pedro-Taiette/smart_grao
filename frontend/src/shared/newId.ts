/**
 * Um UUID v4 gerado no aparelho — o id da foto, que torna o reenvio seguro (a mesma foto nao
 * duplica).
 *
 * `crypto.randomUUID` so existe em contexto seguro (HTTPS ou localhost). Aberto pelo IP da rede,
 * como num celular apontado para o computador da demo, ele nao existe, e a foto nao saia. O
 * `getRandomValues` existe em qualquer contexto e da a mesma garantia de aleatoriedade.
 */
export function newId(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = [...bytes].map(b => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
