# Fase 1 — Safras e cultivos

Implementação do contexto agrícola, sem autenticação. A validação funcional e a aplicação da
migration no banco ficaram para execução posterior, conforme solicitado.

## Modelo e regras

- **Fazenda → safra:** uma safra é um nome compartilhado pelos talhões de uma fazenda, por exemplo
  `2026/2027`. O nome, após remover espaços externos, é único dentro da fazenda. Não existe um
  calendário obrigatório da safra: as datas reais pertencem aos cultivos.
- **Talhão → cultivo:** o talhão representa a área física. Cultura, cultivar/híbrido, safra e data
  de plantio pertencem ao cultivo, cujo contexto permanece imutável depois do cadastro. Nesta
  fase não há edição, reabertura ou exclusão de cultivos.
- Um talhão pode ter vários cultivos sucessivos, inclusive na mesma safra. Não pode ter períodos
  sobrepostos. As datas são inclusivas: o próximo plantio deve ocorrer depois do encerramento
  anterior. Consórcios e cultivos simultâneos no mesmo talhão não estão contemplados.
- O encerramento é explícito e não pode anteceder o plantio nem qualquer estágio registrado.
  Repetir a mesma data de encerramento é idempotente; substituir a data é recusado.
- Estágios são observações datadas, com código/descrição de até 32 caracteres e notas opcionais
  de até 1.000 caracteres. Há um registro por data por cultivo. O sistema não infere o estágio
  pela idade. **O catálogo agronômico e a validação por cultura, que esta fase deixou pendentes,
  foram fechados na fase 2** — ver [Escala fenológica](fase-2-protocolos.md#escala-fenológica).
  Os registros gravados antes disso permanecem como foram digitados.
- Pode-se inserir um registro retrospectivo em cultivo encerrado, desde que a data esteja dentro
  do seu período. As datas são informadas pela equipe; esta etapa não restringe datas futuras.
- Safra e talhão devem pertencer à mesma fazenda. Talhão inativo não aceita novos cultivos ou
  novos planos de amostragem. Talhões com histórico e fazendas com safras não podem ser excluídos.
- Novos planos exigem `cultivationId`, pertencente ao talhão informado e ainda não encerrado.
  O modo `Monitoring` usa MIP-Soja e só aceita soja. Milho utiliza `Mapping`, com espaçamento
  informado: essa malha não representa um protocolo agronômico validado para milho.

## Banco e migração

Migration: `20260922144912_AgriculturalCycles`.

- Cria `seasons`, `cultivations` e `growth_stage_records`.
- Remove `fields.crop`. A coluna antiga não contém cultivar, safra nem plantio, portanto não
  são inventados cultivos ou datas para os registros existentes. Recadastre o contexto de cultivo.
- Acrescenta `sampling_plans.cultivation_id`, anulável somente para acomodar planos antigos.
  Eles permanecem acessíveis como **Histórico sem cultivo vinculado**. A API não cria novos planos
  sem esse vínculo.
- A chave estrangeira composta do plano garante que cultivo e talhão correspondam.
- A extensão PostgreSQL `btree_gist` permite uma restrição de exclusão sobre os períodos. Ela
  impede ciclos sobrepostos mesmo sob requisições concorrentes. Essa restrição é SQL explícito
  na migration; deve ser preservada em alterações futuras do esquema.
- A versão de linha do cultivo e o índice único das observações protegem alterações concorrentes.
- O rollback recria `fields.crop` como `Undefined`; não recupera a informação removida nem o
  histórico de cultivos que foi excluído pelo rollback.

Aplicar no ambiente configurado, a partir da raiz:

```powershell
dotnet run --project backend/src/SmartGrao.WebApi -- migrate
```

Não foi executado nesta entrega.

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/seasons?farmId={id}` | Listar safras da fazenda |
| POST | `/api/seasons` | Criar safra com `farmId` e `name` |
| GET | `/api/cultivations?fieldId={id}` | Listar cultivos e estágios do talhão |
| GET | `/api/cultivations/{id}` | Consultar um cultivo e seu histórico |
| POST | `/api/cultivations` | Criar com `fieldId`, `seasonId`, `crop`, `cultivar`, `plantedOn` |
| PUT | `/api/cultivations/{id}/closure` | Encerrar com `endedOn` |
| POST | `/api/cultivations/{id}/stages` | Registrar `observedOn`, `stage`, `notes` |

Datas de calendário usam `YYYY-MM-DD`, sem conversão de fuso. `notes` pode ser `null`.
`crop` usa o enum existente; milho é `Corn` e `Undefined` não é aceito no cultivo.
Os endpoints de talhão deixam de receber/devolver `crop`. A geração de planos passa a exigir
`cultivationId` junto ao `fieldId`; as consultas de planos devolvem esse vínculo.

Falhas de validação retornam 422, referências inexistentes 404 e conflitos de ciclo/histórico 409.
Os códigos `cultivation.*` estão no catálogo central do backend e traduzidos no frontend.

## Uso e verificação posterior

Na tela de talhões, selecione um talhão e abra **Safras e cultivos**. Cadastre uma safra e um cultivo
(milho é o padrão do formulário). Registre estágios, encerre o cultivo e cadastre o ciclo seguinte.
No mapa, o seletor **Cultivo da amostragem** separa os planos de cada ciclo e os planos antigos.

Roteiro funcional:

1. Criar uma safra e um cultivo de milho com cultivar/híbrido e data de plantio.
2. Registrar um estágio e gerar um plano de mapeamento nesse cultivo.
3. Verificar recusa de cultivo sobreposto, safra de outra fazenda, observação fora do período,
   data de observação duplicada e monitoramento MIP-Soja para milho.
4. Encerrar após a última observação; repetir o encerramento com a mesma data.
5. Criar outro cultivo no mesmo talhão com plantio posterior ao encerramento.
6. Selecionar cada ciclo no mapa: conferir que somente seus próprios planos aparecem.
7. Conferir preservação do primeiro cultivo, suas observações e planos após recarregar a página.
8. Conferir recusa da exclusão do talhão com histórico e ausência de novos planos em cultivo encerrado.

Comandos de validação:

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

Os testes de domínio da fase estão em `backend/tests/SmartGrao.Domain.Tests/Cultivations`.
Não há processamento de IA, upload, coleta offline ou protocolos de milho nesta fase.
