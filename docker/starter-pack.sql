-- Starter pack da demonstração do SmartGrão: fazendas, talhões, pessoas e plantios de milho.
--
-- Roda sozinho na subida do Docker (serviço `seed` do docker-compose.yml), depois que a API aplicou
-- as migrations — que já trazem o catálogo de pragas e doenças e o protocolo de cercosporiose.
-- Pode rodar de novo sem medo: cada linha tem id fixo e `on conflict do nothing`.
--
-- O que NÃO está aqui, de propósito: clima e satélite. O sistema busca os dois sozinho (pilares 1 e
-- 2): o clima na subida da API, o satélite ao abrir o talhão. Fotos também não — o rover gera na
-- demo. Pessoas e fazendas são fictícias; os contornos são de lavouras reais, desenhados sobre a
-- imagem do Sentinel-2 (estradas e carreadores como divisa):
--
-- - Talhão Sul (Castro-PR, Campos Gerais): cobertura de inverno verde até agosto (NDVI 0,92),
--   dessecada no fim de agosto, solo exposto em setembro (0,20) e dossel fechado em novembro (0,88).
--   É o ciclo do milho de verão da região; a cultura é inferida pela curva, não confirmada em campo.
--   Por isso o plantio tem data fixa (12/09/2026), tirada da curva.
-- - Talhão da Sede (Sorriso-MT): o talhão inteiro entre as quatro estradas. Na safrinha 2026, solo
--   exposto em fevereiro, NDVI 0,91 em abril e senescência em junho — a safrinha de Sorriso é milho.
--
-- As datas do plantio atual de Sorriso e dos estágios são relativas a hoje (`current_date`).

begin;

-- Safra no formato do produtor ("2026/2027"): o ano agrícola vai de julho a junho. Mesma regra do
-- frontend (`seasonNameFor`).
create or replace function pg_temp.season_name(planted date) returns text language sql as $$
  select case when extract(month from planted) >= 7
    then extract(year from planted)::int || '/' || extract(year from planted)::int + 1
    else extract(year from planted)::int - 1 || '/' || extract(year from planted)::int end
$$;

-- Fazendas -----------------------------------------------------------------------------------------

insert into farms (id, name, city, state, headquarters, created_at) values
  ('5a000000-0000-4000-8000-000000000001', 'Sítio Santa Rita', 'Castro', 'PR',
   ST_SetSRID(ST_MakePoint(-49.8853, -24.7745), 4326), now()),
  ('5a000000-0000-4000-8000-000000000002', 'Fazenda Boa Esperança', 'Sorriso', 'MT',
   ST_SetSRID(ST_MakePoint(-55.8349, -12.5245), 4326), now())
on conflict (id) do nothing;

-- Pessoas (quem pode ir a campo) -------------------------------------------------------------------

insert into people (id, farm_id, name, role, active, created_at) values
  ('5a000000-0000-4000-8000-000000000101', '5a000000-0000-4000-8000-000000000001', 'Ana Souza', 'Technician', true, now()),
  ('5a000000-0000-4000-8000-000000000102', '5a000000-0000-4000-8000-000000000001', 'Carlos Lima', 'Agronomist', true, now()),
  ('5a000000-0000-4000-8000-000000000103', '5a000000-0000-4000-8000-000000000002', 'João Pereira', 'Producer', true, now())
on conflict (id) do nothing;

-- Talhões ------------------------------------------------------------------------------------------
-- Área e perímetro são os que o sistema calcula (geodésia WGS84 do domínio), não os do PostGIS.

insert into fields (id, farm_id, name, boundary, area_hectares, perimeter_meters, active, created_at) values
  ('5a000000-0000-4000-8000-000000000201', '5a000000-0000-4000-8000-000000000001', 'Talhão Sul',
   ST_GeomFromText('POLYGON((-49.889391 -24.767891,-49.888774 -24.767774,-49.885043 -24.770274,-49.881929 -24.77287,-49.880933 -24.773698,-49.885507 -24.773732,-49.887485 -24.773747,-49.888602 -24.773303,-49.888861 -24.771951,-49.889621 -24.769925,-49.889384 -24.768681,-49.889391 -24.767891))', 4326),
   29.9592, 2498.41, true, now()),
  ('5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000002', 'Talhão da Sede',
   ST_GeomFromText('POLYGON((-55.836772 -12.516373,-55.827073 -12.519518,-55.831336 -12.533302,-55.841682 -12.529775,-55.836772 -12.516373))', 4326),
   182.9059, 5482.65, true, now())
on conflict (id) do nothing;

-- Safras -------------------------------------------------------------------------------------------

insert into seasons (id, farm_id, name, created_at) values
  ('5a000000-0000-4000-8000-000000000301', '5a000000-0000-4000-8000-000000000001', pg_temp.season_name(date '2026-09-12'), now()),
  ('5a000000-0000-4000-8000-000000000302', '5a000000-0000-4000-8000-000000000002', pg_temp.season_name(current_date - 60), now()),
  ('5a000000-0000-4000-8000-000000000303', '5a000000-0000-4000-8000-000000000002', '2025/2026', now())
on conflict (id) do nothing;

-- Plantios de milho --------------------------------------------------------------------------------
-- Talhão Sul: plantado em 12/09/2026 (data tirada da curva do satélite), sem os dados do plantio
--   respondidos ("Não sei").
-- Talhão da Sede: plantado há ~2 meses, híbrido suscetível, milho sobre milho, plantio direto — os
--   três fatores que agravam o risco.
-- Talhão da Sede, safrinha 2026 (encerrada): a safra passada com clima e satélite reais, para mostrar
--   a curva do NDRE de uma safra inteira.

insert into cultivations (id, field_id, season_id, crop, cultivar, planted_on, ended_on, created_at,
                          risk_hybrid_reaction, risk_crop_sequence, risk_tillage) values
  ('5a000000-0000-4000-8000-000000000401', '5a000000-0000-4000-8000-000000000201', '5a000000-0000-4000-8000-000000000301',
   'Corn', 'AG 8700 PRO4', date '2026-09-12', null, now(), 'Unknown', 'Unknown', 'Unknown'),
  ('5a000000-0000-4000-8000-000000000402', '5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000303',
   'Corn', 'DKB 255 PRO3 (safrinha)', date '2026-02-10', date '2026-07-01', now(), 'Susceptible', 'Rotation', 'NoTill'),
  ('5a000000-0000-4000-8000-000000000403', '5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000302',
   'Corn', 'P3707VYH', current_date - 60, null, now(), 'Susceptible', 'CornAfterCorn', 'NoTill')
on conflict (id) do nothing;

-- Estágios observados --------------------------------------------------------------------------------
-- Os atuais ficam dentro da janela de risco (V4 a R2) e com menos de 14 dias, para o sistema não
-- pedir o estágio logo de cara.

insert into growth_stage_records (id, cultivation_id, observed_on, stage, notes) values
  ('5a000000-0000-4000-8000-000000000501', '5a000000-0000-4000-8000-000000000401', current_date - 7, 'V12', null),
  ('5a000000-0000-4000-8000-000000000502', '5a000000-0000-4000-8000-000000000403', current_date - 5, 'V8', null),
  ('5a000000-0000-4000-8000-000000000503', '5a000000-0000-4000-8000-000000000402', date '2026-03-02', 'V4', null),
  ('5a000000-0000-4000-8000-000000000504', '5a000000-0000-4000-8000-000000000402', date '2026-03-20', 'V8', null),
  ('5a000000-0000-4000-8000-000000000505', '5a000000-0000-4000-8000-000000000402', date '2026-04-14', 'VT', null),
  ('5a000000-0000-4000-8000-000000000506', '5a000000-0000-4000-8000-000000000402', date '2026-05-04', 'R2', null),
  ('5a000000-0000-4000-8000-000000000507', '5a000000-0000-4000-8000-000000000402', date '2026-06-01', 'R5', null)
on conflict (id) do nothing;

commit;
