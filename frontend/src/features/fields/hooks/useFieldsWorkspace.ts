import { useCallback, useMemo, useState } from 'react';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import type { LatLngTuple } from '../geo/geoJson';
import { useFarm } from './useFarm';
import { useFields } from './useFields';
import { useRemoveField } from './useRemoveField';
import { useSaveField } from './useSaveField';
import { useToggleFieldStatus } from './useToggleFieldStatus';

/**
 * Toda a interacao da tela de talhoes num lugar so: o que esta selecionado, o que esta sendo
 * desenhado, o que esta sendo redesenhado e o que esta prestes a ser excluido.
 *
 * Existe para que `FieldsPage` seja layout. Sem isto, a pagina acumularia seis `useState`, cinco
 * handlers e as regras de transicao entre eles — e viraria o arquivo de quinhentas linhas que
 * ninguem quer abrir.
 */
export function useFieldsWorkspace(farmId: string) {
  const { farm, isLoading: isLoadingFarm, error: farmError } = useFarm(farmId);
  const { fields, isLoading: isLoadingFields, error: fieldsError, refetch } = useFields(farmId);

  const { saveField, isSaving } = useSaveField();
  const { removeField, isRemoving } = useRemoveField();
  const { toggleFieldStatus, isToggling } = useToggleFieldStatus();

  const [selectedFieldId, setSelectedFieldId] = useState<string | null>(null);

  // Cadastro: o contorno vem do mapa e espera o nome e a cultura.
  const [drawnBoundary, setDrawnBoundary] = useState<GeoJsonPolygon | null>(null);
  const [editingField, setEditingField] = useState<FieldViewModel | null>(null);
  const [isFormOpen, setFormOpen] = useState(false);

  // Redesenho: o contorno muda no mapa e so vai para a API quando o usuario confirma.
  const [redrawingFieldId, setRedrawingFieldId] = useState<string | null>(null);
  const [pendingGeometry, setPendingGeometry] = useState<GeoJsonPolygon | null>(null);

  const [pendingDeletion, setPendingDeletion] = useState<FieldViewModel | null>(null);

  const selectedField = useMemo(
    () => fields.find((field) => field.id === selectedFieldId) ?? null,
    [fields, selectedFieldId],
  );

  const farmCenter = useMemo<LatLngTuple | null>(
    () =>
      farm?.headquarters
        ? [farm.headquarters.latitude, farm.headquarters.longitude]
        : null,
    [farm],
  );

  const handlePolygonDrawn = useCallback((boundary: GeoJsonPolygon) => {
    setEditingField(null);
    setDrawnBoundary(boundary);
    setFormOpen(true);
  }, []);

  const openEdit = useCallback((field: FieldViewModel) => {
    setEditingField(field);
    setDrawnBoundary(field.boundary);
    setFormOpen(true);
  }, []);

  const closeForm = useCallback(() => {
    setFormOpen(false);
    setDrawnBoundary(null);
    setEditingField(null);
  }, []);

  const startRedraw = useCallback((fieldId: string) => {
    setRedrawingFieldId(fieldId);
    setPendingGeometry(null);
  }, []);

  const cancelRedraw = useCallback(() => {
    setRedrawingFieldId(null);
    setPendingGeometry(null);
    // Recarrega para devolver ao mapa o contorno original: a camada foi alterada em memoria
    // durante o arrasto e, sem isso, o desenho descartado continuaria na tela.
    refetch();
  }, [refetch]);

  const saveRedraw = useCallback(async () => {
    if (!selectedField || !pendingGeometry) return;

    const saved = await saveField({
      fieldId: selectedField.id,
      farmId,
      name: selectedField.name,
      crop: selectedField.crop,
      boundary: pendingGeometry,
    });

    if (saved) {
      setRedrawingFieldId(null);
      setPendingGeometry(null);
    } else {
      // A API recusou (contorno invalido ou sobreposto). O modo de redesenho continua aberto para
      // que o usuario corrija o traçado em vez de perder o que ja ajustou.
      setPendingGeometry(null);
    }
  }, [farmId, pendingGeometry, saveField, selectedField]);

  const toggleStatus = useCallback(async () => {
    if (!selectedField) return;
    await toggleFieldStatus(selectedField.id, farmId, !selectedField.active);
  }, [farmId, selectedField, toggleFieldStatus]);

  const confirmDeletion = useCallback(async () => {
    if (!pendingDeletion) return;

    const removed = await removeField(pendingDeletion.id, farmId);
    if (removed) {
      setPendingDeletion(null);
      setSelectedFieldId(null);
    }
  }, [farmId, pendingDeletion, removeField]);

  return {
    farm,
    fields,
    farmCenter,
    isLoading: isLoadingFarm || isLoadingFields,
    error: farmError ?? fieldsError,
    refetch,

    selectedField,
    selectedFieldId,
    selectField: setSelectedFieldId,

    isFormOpen,
    editingField,
    drawnBoundary,
    handlePolygonDrawn,
    openEdit,
    closeForm,

    redrawingFieldId,
    pendingGeometry,
    setPendingGeometry,
    startRedraw,
    cancelRedraw,
    saveRedraw,

    pendingDeletion,
    requestDeletion: setPendingDeletion,
    confirmDeletion,

    isBusy: isSaving || isRemoving || isToggling,
    toggleStatus,
  };
}
