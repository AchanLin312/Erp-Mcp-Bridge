using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.LayerManager;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Events;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.Targets;

public class LayerTarget : IAppStudioTarget
{
	private readonly ILayerService layerService;

	private readonly IEventDesignerService eventDesignerService;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private readonly string rootPath;

	public LayerTarget(string rootPath, ILogger logger, Services services, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		layerService = services.LayerService;
		eventDesignerService = services.EventDesignerService;
	}

	public void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		Epicor.MetaFX.Core.AppStudio.LayerManager.LayerManager layerManager = new Epicor.MetaFX.Core.AppStudio.LayerManager.LayerManager(rootPath, logger, layerService, contentLoader, cacheService);
		EpMetaFxLayer epMetaFxLayer = new EpMetaFxLayer
		{
			CommentText = request.commentText,
			Company = request.layerInfo.company,
			Content = new Dictionary<string, JToken>(),
			DeviceType = request.deviceType,
			IsNew = request.layerInfo.IsNew,
			LayerDescription = request.layerInfo.layerDescription,
			LayerName = request.layerInfo.layerName,
			ParentLayers = request.layerInfo.parentLayers,
			TypeCode = request.layerInfo.typeCode,
			UserName = request.userName,
			ViewId = request.id,
			WIP = request.layerInfo.wip,
			CGCCode = request.layerInfo.CGCCode
		};
		epMetaFxLayer.Content.Add("layout", request.layout);
		if (request.pages != null)
		{
			dynamic pages = request.pages;
			if (pages != null)
			{
				foreach (dynamic item in pages)
				{
					epMetaFxLayer.Content.Add((string)item.name, item);
				}
			}
		}
		OrphansManager.PreProcessSaveRequest(request, epMetaFxLayer);
		epMetaFxLayer.Content.Add("events", request.events);
		epMetaFxLayer.Content.Add("rules", request.rules);
		epMetaFxLayer.Content.Add("tools", request.tools);
		epMetaFxLayer.Content.Add("dataviews", request.dataviews);
		layerManager.SaveLayer(new SaveLayerRequest
		{
			Layer = epMetaFxLayer,
			CountryGroupCode = request.countryGroupCode,
			CountryCode = request.countryCode,
			PublishParentLayers = request.layerInfo.PublishParentLayers
		}, currentCultureName, localizedStringsFunc);
		new EventDesignerManager(rootPath, logger, eventDesignerService, contentLoader).Update(request);
	}

	public void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		throw new NotImplementedException("Not sure what we need to do for publish???");
	}

	public List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool validateBeforePublish = false)
	{
		try
		{
			layerService.BulkPublishLayers(layersToPublish.Where((EpMetaFxLayerForApplication layer) => layer.TypeCode.Equals("KNTCCustLayer", StringComparison.OrdinalIgnoreCase)).ToList());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			logger.Debug("Bulk publish of customization layers failed. Error: " + ex3.Message);
			layersToPublish.ForEach(delegate(EpMetaFxLayerForApplication layer)
			{
				layer.PublishStatus = StatusValue.Failed;
				layer.PublishError = ex3.Message;
			});
		}
		return layersToPublish;
	}

	public List<EpMetaFxLayerForApplication> BulkDeleteLayers(List<EpMetaFxLayerForApplication> layersToDelete, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		try
		{
			layerService.BulkDeleteLayers(layersToDelete.ToList());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			logger.Debug("Bulk delete of layers failed. Error: " + ex3.Message);
			layersToDelete.ForEach(delegate(EpMetaFxLayerForApplication layer)
			{
				layer.DeleteError = ex3.Message;
			});
		}
		return layersToDelete;
	}
}
