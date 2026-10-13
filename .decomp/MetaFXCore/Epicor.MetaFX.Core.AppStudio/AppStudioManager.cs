using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Targets;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio;

public class AppStudioManager
{
	private readonly Services services;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private readonly ILogger logger;

	private readonly string rootPath;

	public AppStudioManager(string rootPath, ILogger logger, Services services, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.services = services;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
	}

	public void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		GetTarget(request).Save(request, currentCultureName, localizedStringsFunc);
	}

	public void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		GetTarget(request).Publish(request, currentCultureName, localizedStringsFunc);
	}

	public List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool validateBeforePublish = false)
	{
		IEnumerable<EpMetaFxLayerForApplication> source = layersToPublish.Where((EpMetaFxLayerForApplication layer) => layer.TypeCode?.Equals("KNTCCustLayer", StringComparison.OrdinalIgnoreCase) == true);
		if (source.Any())
		{
			new LayerTarget(rootPath, logger, services, contentLoader, cacheService).BulkPublishLayers(source.ToList(), currentCultureName, localizedStringsFunc);
		}
		IEnumerable<EpMetaFxLayerForApplication> source2 = layersToPublish.Where((EpMetaFxLayerForApplication layer) => layer.TypeCode == null);
		if (source2.Any())
		{
			new BaseTarget(rootPath, logger, services, contentLoader, cacheService).BulkPublishLayers(source2.ToList(), currentCultureName, localizedStringsFunc);
		}
		return layersToPublish;
	}

	public List<EpMetaFxLayerForApplication> BulkDeleteLayers(List<EpMetaFxLayerForApplication> layersToDelete, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		new LayerTarget(rootPath, logger, services, contentLoader, cacheService).BulkDeleteLayers(layersToDelete.ToList(), currentCultureName, localizedStringsFunc);
		return layersToDelete;
	}

	private IAppStudioTarget GetTarget(EpMetaFxSaveRequestEx request)
	{
		if (request.layerInfo == null)
		{
			return new BaseTarget(rootPath, logger, services, contentLoader, cacheService);
		}
		return new LayerTarget(rootPath, logger, services, contentLoader, cacheService);
	}
}
