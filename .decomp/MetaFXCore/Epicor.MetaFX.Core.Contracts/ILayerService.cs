using System;
using System.Collections.Generic;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Contracts;

public interface ILayerService
{
	ILayerService WithNewSession(SessionData newSession);

	bool SaveLayer(EpMetaFxLayer request, bool publishParentLayer);

	bool DeleteLayer(EpMetaFxLayerRequest request);

	EpMetaFxLayer GetLayer(EpMetaFxLayerRequest request);

	bool LayerExists(EpMetaFxLayerRequest request);

	List<EpMetaFxLayer> GetLayers(EpMetaFxGetLayersRequest request);

	List<EpMetaFxLayer> GetLayersPaged(EpMetaFxGetLayersRequest request);

	List<EpMetaFxLayer> GetAllLayers(EpMetaFxGetLayersRequest request);

	bool UpgradeLayer(EpMetaFxLayer request, List<PageInfo> pages);

	List<EpMetaFXAuditLog> GetAuditLogs(EpMetaFXAuditLogRequest request);

	List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish);

	List<EpMetaFxLayerForApplication> BulkDeleteLayers(List<EpMetaFxLayerForApplication> layersToDelete);

	void DeleteBackupTypeLayers();

	bool PropertyLevelDiffsEnabled();

	List<EpMetaFxLayer> UpdateLayersInDatabase(List<EpMetaFxLayer> layers);

	void ProcessAllLayersForDataFix(string[] applicationsFilter, Func<string, string, Dictionary<string, JToken>, bool> process);

	bool ProcessLayersForPropDiffUpdate(List<EpMetaFxLayerForConversion> layersToUpdate, Func<List<EpMetaFxLayerForConversion>, List<EpMetaFxLayerForConversion>> preprocessLayers, Func<EpMetaFxLayerForConversion, bool> processLayer);

	void ClearLayerDraft(EpMetaFxRequest request);
}
