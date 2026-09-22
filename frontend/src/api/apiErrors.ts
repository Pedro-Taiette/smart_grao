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
  'cultivation.season_not_found': 'Safra não encontrada.',
  'cultivation.invalid_season_name': 'Informe um nome de safra de até 80 caracteres.',
  'cultivation.duplicate_season': 'Já existe uma safra com esse nome nesta fazenda.',
  'cultivation.not_found': 'Cultivo não encontrado.',
  'cultivation.wrong_farm': 'A safra e o talhão precisam pertencer à mesma fazenda.',
  'cultivation.invalid_cultivar': 'Informe uma cultivar ou híbrido de até 120 caracteres.',
  'cultivation.invalid_date': 'Confira as datas: o encerramento e os estágios precisam respeitar o período do cultivo e o histórico registrado.',
  'cultivation.invalid_stage': 'Informe um estágio de até 32 caracteres e observações de até 1.000 caracteres.',
  'cultivation.stage_not_in_scale':
    'Este estágio não existe na escala desta cultura. Escolha um da lista.',
  'cultivation.duplicate_stage_date': 'Já existe um estágio registrado nesta data.',
  'cultivation.overlapping_cycle': 'Os períodos dos cultivos não podem se sobrepor. Encerre o ciclo anterior antes de iniciar outro.',
  'cultivation.closed': 'Este cultivo já foi encerrado.',
  'cultivation.has_history': 'Este talhão tem histórico de cultivos ou amostragem. Use Desativar para preservá-lo.',
  'cultivation.wrong_field': 'O cultivo selecionado não pertence a este talhão.',
  'cultivation.unsupported_monitoring': 'O monitoramento MIP-Soja é exclusivo para soja. Para milho, utilize o mapeamento por espaçamento.',
  'cultivation.concurrent_change': 'O cultivo foi alterado durante esta operação. Atualize os dados e tente novamente.',
  'cultivation.farm_has_seasons': 'Esta fazenda tem safras registradas e não pode ser excluída.',
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

  // Alvos e protocolos
  'protocol.target_not_found': 'Alvo não encontrado no catálogo.',
  'protocol.invalid_target_code':
    'O código do alvo aceita letras minúsculas, números e sublinhado, com até 60 caracteres.',
  'protocol.invalid_target_name': 'Informe o nome comum e o nome científico, com até 120 caracteres.',
  'protocol.unknown_target_kind': 'Um alvo é uma praga ou uma doença foliar.',
  'protocol.duplicate_target_code': 'Já existe um alvo com esse código no catálogo.',
  'protocol.unknown_automation': 'Situação de automação não reconhecida.',
  'protocol.automation_skips_validation':
    'Este alvo ainda não passou pela validação com revisão humana. Coloque-o em validação antes de liberar a análise automática.',
  'protocol.not_found': 'Protocolo não encontrado.',
  'protocol.invalid_code':
    'O código do protocolo aceita letras minúsculas, números e sublinhado, com até 60 caracteres.',
  'protocol.invalid_name': 'Informe um nome de protocolo de até 120 caracteres.',
  'protocol.duplicate_version': 'Já existe essa versão do protocolo.',
  'protocol.not_draft':
    'Este protocolo já foi publicado e não muda mais. Abra a próxima versão para alterá-lo — as vistorias já feitas continuam apontando para a versão em que foram realizadas.',
  'protocol.not_published': 'Este protocolo ainda não foi publicado.',
  'protocol.empty': 'Inclua pelo menos um alvo antes de publicar o protocolo.',
  'protocol.target_from_another_crop':
    'Este alvo é de outra cultura. Um protocolo só observa alvos da cultura que ele declara.',
  'protocol.duplicate_item': 'Este protocolo já observa esse alvo nessa parte da planta.',
  'protocol.item_not_found': 'Alvo não encontrado neste protocolo.',
  'protocol.unknown_unit': 'Unidade de contagem não reconhecida.',
  'protocol.organ_required': 'Informe a parte da planta que será observada.',
  'protocol.organ_not_applicable': 'A contagem por armadilha não observa uma parte da planta.',
  'protocol.reference_level_without_source':
    'Todo nível de referência precisa da fonte de onde veio, com até 300 caracteres.',
  'protocol.reference_level_not_applicable':
    'O registro de presença não tem nível de referência: a presença já justifica a ação.',
  'protocol.reference_level_out_of_range':
    'O nível informado está fora da faixa possível para essa unidade de contagem.',
  'protocol.invalid_instructions': 'Informe as instruções de coleta, com até 2.000 caracteres.',
  'protocol.too_many_photos': 'Escolha de 0 a 5 fotos por alvo.',
  'protocol.concurrent_change':
    'O protocolo foi alterado durante esta operação. Atualize os dados e tente novamente.',

  // Equipe e vistorias
  'inspection.person_not_found': 'Pessoa não encontrada.',
  'inspection.invalid_person_name': 'Informe um nome de até 120 caracteres.',
  'inspection.unknown_role': 'Função não reconhecida.',
  'inspection.person_inactive': 'Esta pessoa não está mais na equipe. Traga-a de volta ou escolha outra.',
  'inspection.person_from_another_farm': 'Esta pessoa é de outra fazenda.',
  'inspection.person_has_history':
    'Esta pessoa tem vistorias registradas. Use “Tirar da equipe” para preservar o histórico.',
  'inspection.not_found': 'Vistoria não encontrada.',
  'inspection.invalid_date': 'A data da vistoria precisa cair dentro do ciclo do cultivo.',
  'inspection.plan_from_another_cultivation':
    'Essa marcação de pontos é de outro cultivo. Escolha uma do cultivo em andamento.',
  'inspection.protocol_not_published':
    'Só um protocolo publicado vai a campo. Publique a versão antes de agendar.',
  'inspection.protocol_from_another_crop': 'Este protocolo é de outra cultura.',
  'inspection.not_scheduled': 'Esta vistoria já começou e não pode mais ser remarcada.',
  'inspection.not_in_progress': 'Inicie a vistoria antes de registrar paradas.',
  'inspection.already_finished': 'Esta vistoria já foi encerrada.',
  'inspection.invalid_timing': 'O horário de conclusão não pode ser anterior ao de início.',
  'inspection.no_observations':
    'Registre ao menos uma parada antes de concluir — ou marque a vistoria como não realizada.',
  'inspection.cancellation_needs_reason': 'Diga por que a vistoria não aconteceu.',
  'inspection.observation_not_found': 'Parada não encontrada.',
  'inspection.point_from_another_plan': 'Essa parada não pertence à caminhada desta vistoria.',
  'inspection.duplicate_point_observation': 'Esta parada já foi registrada nesta vistoria.',
  'inspection.invalid_accuracy': 'A precisão do GPS precisa ser um número de metros não negativo.',
  'inspection.invalid_notes': 'As observações aceitam até 1.000 caracteres.',
  'inspection.unknown_count_target': 'Cada contagem precisa apontar para um alvo do protocolo.',
  'inspection.count_from_another_protocol': 'Esse alvo não está no protocolo desta vistoria.',
  'inspection.duplicate_count': 'O mesmo alvo foi contado duas vezes nesta parada.',
  'inspection.presence_takes_no_value': 'Este alvo é só presença: não tem contagem.',
  'inspection.count_value_required': 'Informe a contagem — use 0 se avaliou e não encontrou nada.',
  'inspection.count_value_out_of_range': 'O valor está fora da faixa possível para essa unidade.',
  'inspection.concurrent_change':
    'A vistoria foi alterada durante esta operação. Atualize os dados e tente novamente.',

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
