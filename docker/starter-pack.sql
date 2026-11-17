-- Starter pack da demonstração do SmartGrão: fazendas, talhões, pessoas e plantios de milho.
--
-- Roda sozinho na subida do Docker (serviço `seed` do docker-compose.yml), depois que a API aplicou
-- as migrations — que já trazem o catálogo de pragas e doenças e o protocolo de cercosporiose.
-- Pode rodar de novo sem medo: cada linha tem id fixo e `on conflict do nothing`.
--
-- O que NÃO está aqui, de propósito: clima e satélite. O sistema busca os dois sozinho (pilares 1 e
-- 2): o clima na subida da API, o satélite ao abrir o talhão. Fotos também não — o rover gera na
-- demo. Pessoas e fazendas são fictícias; os contornos são de lavouras reais vistas na imagem de
-- satélite, para o Sentinel-2 ter o que ler.
--
-- As datas dos plantios atuais são relativas a hoje (`current_date`): o pacote não envelhece.

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
  ('5a000000-0000-4000-8000-000000000001', 'Sítio Santa Rita', 'Marília', 'SP',
   ST_SetSRID(ST_MakePoint(-49.9551, -22.3185), 4326), now()),
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
   ST_GeomFromText('POLYGON((-49.958037 -22.317401,-49.954598 -22.316746,-49.952581 -22.317416,-49.9533 -22.318905,-49.9536 -22.320731,-49.956079 -22.320215,-49.958037 -22.317401))', 4326),
   15.3422, 1601.83, true, now()),
  ('5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000002', 'Talhão da Sede',
   ST_GeomFromText('POLYGON((-55.8386 -12.5195,-55.8311 -12.5195,-55.8311 -12.5295,-55.8386 -12.5295,-55.8386 -12.5195))', 4326),
   90.5258, 3852.14, true, now())
on conflict (id) do nothing;

-- Safras -------------------------------------------------------------------------------------------

insert into seasons (id, farm_id, name, created_at) values
  ('5a000000-0000-4000-8000-000000000301', '5a000000-0000-4000-8000-000000000001', pg_temp.season_name(current_date - 65), now()),
  ('5a000000-0000-4000-8000-000000000302', '5a000000-0000-4000-8000-000000000002', pg_temp.season_name(current_date - 60), now()),
  ('5a000000-0000-4000-8000-000000000303', '5a000000-0000-4000-8000-000000000002', '2025/2026', now())
on conflict (id) do nothing;

-- Plantios de milho --------------------------------------------------------------------------------
-- Talhão Sul: plantado há ~2 meses, sem os dados do plantio respondidos ("Não sei").
-- Talhão da Sede: plantado há ~2 meses, híbrido suscetível, milho sobre milho, plantio direto — os
--   três fatores que agravam o risco.
-- Talhão da Sede, safrinha 2026 (encerrada): a safra passada com clima e satélite reais, para mostrar
--   a curva do NDRE de uma safra inteira.

insert into cultivations (id, field_id, season_id, crop, cultivar, planted_on, ended_on, created_at,
                          risk_hybrid_reaction, risk_crop_sequence, risk_tillage) values
  ('5a000000-0000-4000-8000-000000000401', '5a000000-0000-4000-8000-000000000201', '5a000000-0000-4000-8000-000000000301',
   'Corn', 'AG 8700 PRO4', current_date - 65, null, now(), 'Unknown', 'Unknown', 'Unknown'),
  ('5a000000-0000-4000-8000-000000000402', '5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000303',
   'Corn', 'DKB 255 PRO3 (safrinha)', date '2026-02-10', date '2026-07-01', now(), 'Susceptible', 'Rotation', 'NoTill'),
  ('5a000000-0000-4000-8000-000000000403', '5a000000-0000-4000-8000-000000000202', '5a000000-0000-4000-8000-000000000302',
   'Corn', 'P3707VYH', current_date - 60, null, now(), 'Susceptible', 'CornAfterCorn', 'NoTill')
on conflict (id) do nothing;

-- Estágios observados --------------------------------------------------------------------------------
-- Os atuais ficam dentro da janela de risco (V4 a R2) e com menos de 14 dias, para o sistema não
-- pedir o estágio logo de cara.

insert into growth_stage_records (id, cultivation_id, observed_on, stage, notes) values
  ('5a000000-0000-4000-8000-000000000501', '5a000000-0000-4000-8000-000000000401', current_date - 7, 'V8', null),
  ('5a000000-0000-4000-8000-000000000502', '5a000000-0000-4000-8000-000000000403', current_date - 5, 'V8', null),
  ('5a000000-0000-4000-8000-000000000503', '5a000000-0000-4000-8000-000000000402', date '2026-03-02', 'V4', null),
  ('5a000000-0000-4000-8000-000000000504', '5a000000-0000-4000-8000-000000000402', date '2026-03-20', 'V8', null),
  ('5a000000-0000-4000-8000-000000000505', '5a000000-0000-4000-8000-000000000402', date '2026-04-14', 'VT', null),
  ('5a000000-0000-4000-8000-000000000506', '5a000000-0000-4000-8000-000000000402', date '2026-05-04', 'R2', null),
  ('5a000000-0000-4000-8000-000000000507', '5a000000-0000-4000-8000-000000000402', date '2026-06-01', 'R5', null)
on conflict (id) do nothing;

commit;
