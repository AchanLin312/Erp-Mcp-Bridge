using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Metadata;

internal class ConfiguratorRowRuleMetadata : RowRuleMetadata
{
	private ConfiguratorHelper configuratorHelper;

	public ConfiguratorRowRuleMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, contentLoader, cacheService)
	{
		configuratorHelper = new ConfiguratorHelper();
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		CreateTabPageRowRules(request, currentCultureName);
		base.Save(request, currentCultureName, localizedStringsFunc);
	}

	private void CreateTabPageRowRules(EpMetaFxSaveRequestEx request, Func<string> currentCultureName)
	{
		if (request.pages == null || !request.pages.Any())
		{
			return;
		}
		JArray source = LoadFileContent(request.id, request.viewType, currentCultureName, isDraft: true)["rules"] as JArray;
		foreach (JToken page in request.pages.Where((JToken pg) => pg["viewType"]?.ToString() == "TabPage"))
		{
			if (source.FirstOrDefault((JToken rl) => rl["id"].ToString() == string.Format("readonly{0}", page["name"])) == null)
			{
				List<object> list = new List<object>();
				list.Add(new
				{
					action = "DisableRow",
					exceptTheseColumns = new List<object>()
				});
				if (request.rules == null)
				{
					request.rules = new JArray();
				}
				request.rules.Add(JToken.FromObject(new
				{
					id = string.Format("readonly{0}", page["name"]),
					dataView = page["name"].ToString(),
					condition = string.Format("PcPage.ReadOnly = true AND PcPage.PageSeq = {0}", configuratorHelper.GetPageNumber(page["name"].ToString())),
					mode = "A",
					actions = list.ToArray(),
					customizable = false,
					disabled = false
				}));
			}
		}
	}
}
