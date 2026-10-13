using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata;
using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Applications;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.Targets;

public class BaseTarget : IAppStudioTarget
{
	private readonly List<BaseMetadataType> metadatas;

	private readonly string rootPath;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private ICacheService cacheService;

	public BaseTarget(string rootPath, ILogger logger, Services services, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		metadatas = new List<BaseMetadataType>
		{
			new LayoutMetadata(rootPath, logger, contentLoader, cacheService),
			new PageMetadata(rootPath, logger, contentLoader, cacheService),
			new EventMetadata(rootPath, logger, services, contentLoader, cacheService),
			new RowRuleMetadata(rootPath, logger, contentLoader, cacheService),
			new ToolMetadata(rootPath, logger, contentLoader, cacheService),
			new DataViewMetadata(rootPath, logger, contentLoader, cacheService)
		};
	}

	public void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		if (!Directory.Exists(rootPath + "/" + request.id))
		{
			new ApplicationManager(rootPath, logger, contentLoader, cacheService).CopyTemplateApplication(request);
		}
		metadatas.ForEach(delegate(BaseMetadataType designer)
		{
			designer.Save(request, currentCultureName, localizedStringsFunc);
		});
	}

	public void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		if (!Directory.Exists(rootPath + "/" + request.id))
		{
			new ApplicationManager(rootPath, logger, contentLoader, cacheService).CopyTemplateApplication(request);
		}
		metadatas.ForEach(delegate(BaseMetadataType designer)
		{
			designer.Publish(request, currentCultureName, localizedStringsFunc);
		});
	}

	public List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool _validateBeforePublish = false)
	{
		EpMetaFxSaveRequestEx request;
		foreach (EpMetaFxLayerForApplication item in layersToPublish)
		{
			try
			{
				if (contentLoader.DirectoryExists(Path.Combine(rootPath, item.Id)))
				{
					request = new EpMetaFxSaveRequestEx
					{
						id = item.Id,
						viewType = "view"
					};
					if (!metadatas.Any((BaseMetadataType designer) => designer.HasActiveDraft(request.id)))
					{
						item.PublishStatus = StatusValue.Failed;
						item.PublishError = "Publish failed for base layer for Application " + item.Id + ". Error: Layer does not contain an active draft.";
						continue;
					}
					metadatas.ForEach(delegate(BaseMetadataType designer)
					{
						designer.Publish(request, currentCultureName, localizedStringsFunc, inBulkPublish: true);
					});
					if (!item.SystemFlag)
					{
						contentLoader.CreateVersion(item);
					}
					item.PublishStatus = StatusValue.Success;
					item.PublishError = null;
					item.IsPublished = true;
					item.HasDraftContent = false;
					item.LastUpdated = DateTime.UtcNow;
				}
				else
				{
					item.PublishStatus = StatusValue.Failed;
					item.PublishError = "Publish failed for base layer for Application " + item.Id + ". Error: Base layer not found.";
				}
			}
			catch (Exception ex)
			{
				item.PublishStatus = StatusValue.Failed;
				item.PublishError = ex.Message;
			}
		}
		return layersToPublish;
	}
}
