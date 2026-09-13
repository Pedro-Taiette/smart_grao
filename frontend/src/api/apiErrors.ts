import { ApiError } from './ApiError';

/**
 * Catalogo de traducao dos codigos de erro do backend.
 *
 * O backend nunca manda texto para o usuario: ele manda um codigo estavel. Este arquivo e o outro
 * lado desse contrato — a unica lista de mensagens em portugues do app. Quando o backend acrescenta
 * um codigo, e aqui que ele ganha voz.
 *
 * As mensagens falam do que o produtor fez, nao do que o sistema achou. "O contorno se cruza" e
 * acionavel; "geometria topologicamente invalida" nao e.
 */
const messages: Record<string, string> = {
  // Geometria
  'geo.invalid_polygon':
    'O contorno se cruza em algum ponto. Refaça o desenho sem sobrepor as linhas.',
  'geo.polygon_has_holes': 'O talhão não pode ter uma área vazada por dentro.',
  'geo.malformed_geojson': 'O desenho chegou incompleto. Tente desenhar novamente.',
  'geo.unsupported_geojson': 'Só é possível salvar áreas fechadas (polígonos).',
  'geo.empty_boundary': 'Desenhe o contorno do talhão antes de salvar.',
  'geo.insufficient_vertices': 'O contorno precisa de pelo menos três pontos.',
  'geo.too_many_vertices': 'O contorno tem pontos demais. Simplifique o desenho.',
  'geo.latitude_out_of_range': 'Latitude fora da faixa válida.',
  'geo.longitude_out_of_range': 'Longitude fora da faixa válida.',
  'geo.coordinate_not_finite': 'As coordenadas do desenho são inválidas.',

  // Fazenda
  'farm.not_found': 'Fazenda não encontrada.',
  'farm.name_required': 'Informe o nome da fazenda.',
  'farm.name_too_long': 'O nome da fazenda é longo demais.',
  'farm.city_required': 'Informe o município.',
  'farm.city_too_long': 'O nome do município é longo demais.',
  'farm.invalid_state': 'A UF deve ter duas letras.',
  'farm.has_fields':
    'Esta fazenda ainda tem talhões cadastrados. Remova os talhões antes de excluí-la.',

  // Talhão
  'field.not_found': 'Talhão não encontrado.',
  'field.name_required': 'Informe o nome do talhão.',
  'field.name_too_long': 'O nome do talhão é longo demais.',
  'field.duplicate_name': 'Já existe um talhão com esse nome nesta fazenda.',
  'field.area_below_minimum': 'A área desenhada é pequena demais para ser um talhão.',
  'field.area_above_maximum':
    'A área desenhada é grande demais. Verifique se o contorno está no lugar certo.',
  'field.overlaps_another': 'Este contorno invade a área de outro talhão da mesma fazenda.',
  'field.unknown_crop': 'Cultura não reconhecida.',

  // Amostragem
  'sampling.not_found': 'Marcação de pontos não encontrada.',
  'sampling.field_is_inactive':
    'Este talhão está desativado. Reative-o para marcar pontos de amostragem.',
  'sampling.field_too_narrow_for_edge_buffer':
    'Este talhão é estreito demais. A amostragem descarta uma faixa junto à divisa, porque ali a praga se concentra e a contagem sairia alta demais — e nesse talhão não sobra área no meio.',
  'sampling.no_points_fit_the_field':
    'Nenhum ponto coube dentro deste talhão. Tente um detalhamento maior.',
  'sampling.grid_too_dense':
    'Esse detalhamento geraria pontos demais para um talhão deste tamanho. Escolha menos detalhe.',
  'sampling.spacing_out_of_range': 'O detalhamento escolhido está fora do que o sistema aceita.',
  'sampling.spacing_not_finite': 'O detalhamento escolhido é inválido.',
  'sampling.unknown_mode': 'Tipo de amostragem não reconhecido.',

  // Transversais
  'validation.failed': 'Há campos preenchidos de forma inválida.',
  'network.unreachable':
    'Não foi possível falar com o servidor. Verifique sua conexão e tente de novo.',
  internal_error: 'Algo deu errado do nosso lado. Tente novamente em instantes.',
};

const fallback = messages.internal_error;

/** Mensagem em portugues para qualquer falha, inclusive as que nao vieram da API. */
export function getApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) return fallback;
  return messages[error.code] ?? fallback;
}

/** Verdadeiro quando o erro e exatamente o codigo indicado — util para reagir a um caso especifico. */
export function isApiErrorCode(error: unknown, code: string): boolean {
  return error instanceof ApiError && error.code === code;
}
