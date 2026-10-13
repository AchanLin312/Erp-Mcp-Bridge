using System;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Metadata;

internal class DataViewMetadata : BaseMetadataType
{
	private readonly IContentLoader contentLoader;

	public DataViewMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "dataviews", contentLoader, cacheService)
	{
		this.contentLoader = contentLoader;
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool inBulkPublish = false)
	{
		if (!inBulkPublish)
		{
			CreateTabPageDataViews(request, currentCultureName);
		}
		base.Publish(request, currentCultureName, localizedStringsFunc, inBulkPublish);
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		CreateTabPageDataViews(request, currentCultureName);
		base.Save(request, currentCultureName, localizedStringsFunc);
	}

	private bool CreateTabPageDataViews(EpMetaFxSaveRequestEx request, Func<string> currentCultureName)
	{
		if (request.pages == null || !request.pages.Any())
		{
			return false;
		}
		bool flag = false;
		JObject jObject = LoadFileContent(request.id, request.viewType, currentCultureName, isDraft: true);
		JArray jArray = jObject["dataviews"] as JArray;
		foreach (JToken page in request.pages.Where((JToken pg) => pg["viewType"]?.ToString() == "TabPage"))
		{
			if (jArray.FirstOrDefault((JToken dv) => dv["id"].ToString() == page["name"].ToString()) == null)
			{
				jArray.Add(JToken.FromObject(new DataView
				{
					Id = page["name"].ToString(),
					Table = page["name"].ToString(),
					Schema = "Erp"
				}));
				flag = true;
			}
		}
		if (flag)
		{
			string path = Path.Combine(rootPath, request.id, "dataviews.jsonc");
			contentLoader.WriteAllText(path, jObject.ToString());
		}
		return flag;
	}
}
