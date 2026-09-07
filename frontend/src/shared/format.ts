const areaFormatter = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const distanceFormatter = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 });

/** Area em hectares, como aparece em relatorio: `1.234,56 ha`. */
export function formatHectares(hectares: number): string {
  return `${areaFormatter.format(hectares)} ha`;
}

/** Perimetro legivel: metros ate 1 km, quilometros a partir dai. */
export function formatDistance(meters: number): string {
  if (meters < 1_000) return `${distanceFormatter.format(meters)} m`;
  return `${areaFormatter.format(meters / 1_000)} km`;
}

export function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString('pt-BR');
}
