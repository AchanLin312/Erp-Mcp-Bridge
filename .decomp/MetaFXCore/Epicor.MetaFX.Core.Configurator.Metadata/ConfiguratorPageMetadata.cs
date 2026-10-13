using System;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Metadata;

internal class ConfiguratorPageMetadata : PageMetadata
{
	private static readonly string[] FilesToIgnore = new string[2] { "SmartString.layout", "ConfigurationSummary.layout" };

	private readonly IContentLoader contentLoader;

	public ConfiguratorPageMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, contentLoader, cacheService)
	{
		this.contentLoader = contentLoader;
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool inBulkPublish = false)
	{
		if (!inBulkPublish)
		{
			CreateOrRemoveTabPage(request, currentCultureName);
		}
		base.Publish(request, currentCultureName, localizedStringsFunc, inBulkPublish);
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		CreateOrRemoveTabPage(request, currentCultureName);
		base.Save(request, currentCultureName, localizedStringsFunc);
		CopyPages(request);
	}

	protected override void DeletePages(string folder)
	{
		contentLoader.GetFiles(folder).ToList().ForEach(delegate(string file)
		{
			if (!Enumerable.Contains<string>(FilesToIgnore, Path.GetFileNameWithoutExtension(file)))
			{
				contentLoader.Delete(file);
			}
		});
	}

	private void CopyPages(EpMetaFxSaveRequestEx request)
	{
		string path = Path.Combine(rootPath, request.id, "pages");
		string[] filesToIgnore = FilesToIgnore;
		foreach (string text in filesToIgnore)
		{
			if (contentLoader.Exists(Path.Combine(path, text + ".jsonc")))
			{
				contentLoader.WriteAllText(Path.Combine(path, text + ".designer.draft.jsonc"), contentLoader.ReadAllText(Path.Combine(path, text + ".jsonc")));
			}
		}
	}

	private void CreateOrRemoveTabPage(EpMetaFxSaveRequestEx request, Func<string> currentCultureName)
	{
		if (request.pages == null || !request.pages.Any() || !request.pages.Any((JToken pg) => pg["viewType"]?.ToString() == "TabPage"))
		{
			request.pages = new JArray();
		}
		else
		{
			if (request.pages.Any((JToken pg) => pg["viewType"]?.ToString() == "Tab"))
			{
				return;
			}
			JToken jToken = LoadPageTemplate("Configurator", currentCultureName)["layout"];
			JToken value = (jToken["viewType"] = "Tab");
			jToken["name"] = value;
			JToken jToken3 = jToken["navigation"];
			value = (jToken3["pageCaption"] = "Main");
			jToken["caption"] = value;
			jToken3["enable"] = true;
			jToken3["parentPage"] = request.layout["name"];
			foreach (JToken page in request.pages)
			{
				page["navigation"]["parentPage"] = "Tab";
				page["viewType"] = "TabPage";
			}
			request.pages.Add(jToken);
		}
	}
}
