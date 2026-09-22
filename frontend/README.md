# Frontend

React + TypeScript + MUI. O cliente da API é gerado pelo Orval a partir do contrato OpenAPI do
backend.

## Rodando

```powershell
npm install
npm run dev
```

O backend precisa estar no ar em `http://localhost:5148` (ajuste em `.env.development`). A porta do
Vite é fixa em 5173 porque é a origem liberada no CORS da API.

| Comando | O que faz |
|---|---|
| `npm run dev` | servidor de desenvolvimento |
| `npm run build` | typecheck + build de produção |
| `npm run typecheck` | só o typecheck |
| `npm run api` | regera o cliente a partir de `../openapi/SmartGrao.WebApi.json` |

## Organização

```
src/
  api/
    generated/      cliente do Orval — NUNCA editar à mão
    httpClient.ts   instância axios; o único lugar que fala HTTP
    ApiError.ts     ProblemDetails → erro tipado com código estável
    apiErrors.ts    código → mensagem em português
  app/              tema, providers, router, política de cache
  features/
    today/          a tela de abertura: pendências e agenda do dia
    farms/          cadastro das propriedades, mais FarmContext.tsx (a fazenda em uso)
    fields/         mapa e página do talhão, mais geo/ (GeoJSON ↔ Leaflet)
    cultivations/   safras, ciclos e estágios de um talhão
    sampling/       marcação de pontos, mais samplingLabels.ts (vocabulário)
    protocols/      pragas, doenças e protocolos, mais protocolLabels.ts
    inspections/    equipe, vistorias e coleta em campo, mais inspectionLabels.ts
    settings/       o hub dos cadastros de referência
  shared/           componentes e utilidades sem dono
```

## Navegação

Três destinos de trabalho — **Hoje**, **Talhões**, **Vistorias** — e um porão de cadastro em
**Ajustes**. A fazenda não é uma tela: é um seletor fixo na barra do topo, lembrado entre sessões.

As razões de cada decisão, o que se perdeu ao tirar a fazenda da URL e o mapa completo de rotas
estão em [`docs/navegacao.md`](../docs/navegacao.md).

## As regras que mantêm isso desacoplado

**Nenhum componente importa de `api/generated`.** Ele importa um hook da própria feature. Os hooks
gerados são um detalhe de implementação — trocar o Orval por outra coisa mexe nos hooks de feature e
em mais nada. Os *tipos* gerados, esses sim, são usados direto: eles são o contrato, e duplicá-los à
mão só criaria uma segunda verdade para divergir da primeira.

**Os componentes não veem TanStack Query.** Os hooks devolvem `{ farms, isLoading, error }` e
`{ saveFarm, isSaving }` — não `data`, `mutateAsync` nem `queryKey`. Invalidação de cache e aviso de
erro moram no hook, então nenhuma tela repete os dois.

**Formulários são só renderização.** Validação no schema zod, estado no `useXForm`, persistência no
`useSaveX`. O `FarmFormDialog` não tem `useState`, `try/catch` nem chamada de API — só a decisão de
qual campo aparece e onde.

**Criar e editar são a mesma função.** `useSaveFarm` recebe o id quando está editando e o omite
quando está criando. Sem isso, todo diálogo carregaria um `if (isEditing)` que não é lógica de tela.

**Mensagens de erro vêm de um catálogo único.** O backend manda um código estável
(`field.overlaps_another`); `apiErrors.ts` é o outro lado desse contrato e é o único arquivo com
frases em português para falhas.

**Estado de interação complexo sai da página.** `FieldsPage` é layout; o `useFieldsWorkspace` carrega
o que está selecionado, o que está sendo desenhado, o que está sendo redesenhado e o que vai ser
excluído.

## O fluxo do Pilar 1

1. Cadastre a fazenda em **Fazendas**. A sede é opcional e serve para o mapa abrir sobre a sua terra.
2. Clique em **Talhões**. A ferramenta de polígono fica no canto superior direito do mapa.
3. Feche o contorno — o diálogo pede o nome. A área em hectares é calculada pelo backend,
   nunca digitada.
4. Selecione um talhão para editar, redesenhar o contorno, desativar ou excluir.
5. Em **Safras e cultivos**, cadastre a safra e o cultivo (milho é o padrão), registre estágios e
   encerre o ciclo antes de iniciar outro. No mapa, selecione o cultivo para consultar seus planos.

O fluxo, a migration necessária e o roteiro de validação estão em
[Fase 1 — Safras e cultivos](../docs/fase-1-cultivos.md).

O Geoman está com `allowSelfIntersection: false`, então o "nó de gravata" é barrado ainda durante o
traçado. O backend recusa de novo (`geo.invalid_polygon`) — a checagem do navegador é só a que chega
antes.

## Quando o contrato da API mudar

```powershell
dotnet build backend/SmartGrao.slnx   # emite ../openapi/SmartGrao.WebApi.json
cd frontend && npm run api && npm run typecheck
```

Uma mudança incompatível aparece como erro de compilação nos hooks de feature — que é exatamente onde
ela deve doer.

---

Decisões que valem para o projeto todo estão em [`docs/arquitetura.md`](../docs/arquitetura.md).
