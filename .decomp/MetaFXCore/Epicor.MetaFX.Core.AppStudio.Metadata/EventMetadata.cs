using System;
using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Events;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.Metadata;

internal class EventMetadata : BaseMetadataType
{
	private readonly IEventDesignerService eventDesignerService;

	private readonly IContentLoader contentLoader;

	public EventMetadata(string rootPath, ILogger logger, Services services, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "events", contentLoader, cacheService)
	{
		eventDesignerService = services?.EventDesignerService;
		this.contentLoader = contentLoader;
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		base.Save(request, currentCultureName, localizedStringsFunc);
		new EventDesignerManager(rootPath, logger, eventDesignerService, contentLoader).Update(request);
	}
}
