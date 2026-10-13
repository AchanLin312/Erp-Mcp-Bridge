using System;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.Metadata;

internal class PageMetadata : BaseMetadataType
{
	private readonly IContentLoader contentLoader;

	public PageMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "pages", contentLoader, cacheService)
	{
		this.contentLoader = contentLoader;
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool inBulkPublish = false)
	{
		if (!((request.pages != null) | inBulkPublish))
		{
			return;
		}
		logger.Debug("publish layout for the " + request.id);
		string text = Path.Combine(rootPath, request.id);
		string text2 = Path.Combine(text, "pages");
		contentLoader.CreateDirectory(text2);
		if (request.pages != null && request.pages.HasValues)
		{
			DeletePages(text2);
			CreatePages(text, request.pages, currentCultureName, draft: false, request.viewType);
		}
		else if (inBulkPublish)
		{
			contentLoader.GetFiles(text2).ToList().ForEach(delegate(string file)
			{
				if (file.EndsWith("layout.designer.draft.jsonc"))
				{
					string path = file.Replace("layout.designer.draft.jsonc", "layout.jsonc");
					contentLoader.WriteAllText(path, contentLoader.ReadAllText(file));
					contentLoader.Delete(file);
				}
			});
		}
		else if (request.pages != null)
		{
			DeletePages(text2);
		}
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		if (request.pages != null)
		{
			logger.Debug("Save pages for the " + request.id);
			string text = Path.Combine(rootPath, request.id);
			contentLoader.CreateDirectory(Path.Combine(text, "pages"));
			DeleteDraftPages(Path.Combine(text, "pages"));
			if (request.pages.HasValues)
			{
				CreatePages(text, request.pages, currentCultureName, draft: true, request.viewType);
			}
		}
	}

	public override bool HasActiveDraft(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "pages");
		return contentLoader.GetFiles(path).Any((string pageName) => pageName.EndsWith("layout.designer.draft.jsonc"));
	}

	protected virtual void DeletePages(string folder)
	{
		contentLoader.EmptyDirectory(folder);
	}

	protected virtual void DeletePublishedPages(string folder)
	{
		contentLoader.GetFiles(folder).ToList().ForEach(delegate(string file)
		{
			if (file.EndsWith("layout.jsonc"))
			{
				contentLoader.Delete(file);
			}
		});
	}

	private void DeleteDraftPages(string folder)
	{
		contentLoader.GetFiles(folder).ToList().ForEach(delegate(string file)
		{
			if (file.EndsWith("layout.designer.draft.jsonc"))
			{
				contentLoader.Delete(file);
			}
		});
	}

	private void CreatePages(string folder, JToken content, Func<string> currentCultureName, bool draft, string viewType)
	{
		string arg = (draft ? "layout.designer.draft.jsonc" : "layout.jsonc");
		foreach (JToken item in JsonConvert.DeserializeObject<JArray>(content.ToString()))
		{
			string filePath = Path.Combine(folder, "pages", string.Format("{0}.{1}", item["name"], arg));
			SaveLayout(filePath, item, currentCultureName, draft, viewType);
		}
	}

	private void SaveLayout(string filePath, JToken page, Func<string> currentCultureName, bool draft, string viewType)
	{
		ResetReusableComponentRef((JObject)page);
		JObject jObject = CreateLayout(page, currentCultureName, draft, viewType);
		bool isPage = filePath.ToLower().Contains("pages");
		ResetReusableComboRef(jObject, isPage);
		string contents = JsonConvert.SerializeObject(jObject, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
		contentLoader.WriteAllText(filePath, contents);
	}

	private JObject CreateLayout(JToken layout, Func<string> currentCultureName, bool draft, string viewType)
	{
		logger.Debug("Creating the layout for page");
		JObject obj = LoadPageTemplate(viewType, currentCultureName) ?? throw new Exception("Processing Error : Template is null");
		JObject jObject = JObject.FromObject(layout);
		if (!draft)
		{
			jObject["pageType"] = "Responsive";
		}
		FlattenLayout flattenLayout = new FlattenLayout();
		obj["layout"] = flattenLayout.ConvertToFlattenLayout(jObject);
		return obj;
	}

	protected JObject LoadPageTemplate(string viewType, Func<string> currentCultureName)
	{
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			ViewType = viewType
		};
		return JObject.FromObject(new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader).GetTemplate(request, currentCultureName));
	}
}
