-- Curadoria da copia de demonstracao. Roda so no Postgres local temporario, nunca no Neon.
\set ON_ERROR_STOP on
begin;

-- Talhoes e fazendas que saem da demo.
create temp table doomed_fields as
select f.id from fields f join farms fa on fa.id = f.farm_id
where fa.name in ('FarmLab', 'Fazenda Unimar')
   or (fa.city = 'Sorriso' and f.name in ('Talhao Leste', 'Talhao Norte', 'Talhao norte 2', 'Talhão Rec'));
create temp table doomed_farms as select id from farms where name in ('FarmLab', 'Fazenda Unimar');

-- Vistorias de teste.
create temp table doomed_inspections as
select id from inspections
where id in ('01a0caa1-887c-7844-a59d-ffa0721da00d', '01a19e78-ab50-7361-9a92-e7f358a3d5b5',
             '01a1a3c4-8ed3-7ec2-af48-7585518f5051', '01a19e90-3ac4-7f3b-8ab1-32562ac41c67')
   or field_id in (select id from doomed_fields);

-- Fotos que saem: as do rover (a demo roda o rover do zero) e as dos talhoes/vistorias que saem.
create temp table doomed_photos as
select id from photos
where source = 'Rover' or field_id in (select id from doomed_fields)
   or inspection_id in (select id from doomed_inspections);

\copy (select storage_key from photos where id in (select id from doomed_photos)) to '/tmp/doomed_photos.txt'
\copy (select heatmap_key from assessments where photo_id in (select id from doomed_photos) and heatmap_key is not null) to '/tmp/doomed_heatmaps.txt'

delete from assessments where photo_id in (select id from doomed_photos) and review_of is not null;
delete from assessments where photo_id in (select id from doomed_photos);
delete from analysis_jobs where photo_id in (select id from doomed_photos);
delete from photos where id in (select id from doomed_photos);

delete from inspections where id in (select id from doomed_inspections);
delete from sampling_plans where field_id in (select id from doomed_fields);
delete from cultivations where field_id in (select id from doomed_fields);
delete from fields where id in (select id from doomed_fields);

delete from people where farm_id in (select id from doomed_farms)
  and id not in (select responsible_id from inspections);
delete from seasons where farm_id in (select id from doomed_farms);
delete from farms where id in (select id from doomed_farms);

-- Duas fazendas com o mesmo nome confundem o seletor; a cidade desfaz.
update farms set name = 'Fazenda Santa Rita (Sorriso)' where name = 'Fazenda Santa Rita' and city = 'Sorriso';
update farms set name = 'Fazenda Santa Rita (Marília)', city = 'Marília' where name = 'Fazenda Santa Rita' and lower(city) = 'marilia';

commit;

select fa.name, f.name, (select count(*) from photos p where p.field_id = f.id) photos,
       (select count(*) from inspections i where i.field_id = f.id) inspections
from fields f join farms fa on fa.id = f.farm_id order by 1, 2;
