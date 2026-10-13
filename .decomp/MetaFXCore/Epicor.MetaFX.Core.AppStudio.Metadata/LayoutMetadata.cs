using System;
using System.IO;
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

internal class LayoutMetadata : BaseMetadataType
{
	private readonly IContentLoader contentLoader;

	public LayoutMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "layout", contentLoader, cacheService)
	{
		this.contentLoader = contentLoader;
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("Save pages for the " + request.id);
		string path = Path.Combine(rootPath, request.id, "layout.draft.jsonc");
		if (request.classicLayout != null)
		{
			SetLayout(path, request.classicLayout, "Absolute", request.viewType, currentCultureName);
		}
		string path2 = Path.Combine(rootPath, request.id, "layout.designer.draft.jsonc");
		if (request.layout != null && request.layout.HasValues)
		{
			SetLayout(path2, request.layout, string.Empty, request.viewType, currentCultureName);
		}
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool inBulkPublish = false)
	{
		logger.Debug("publish layout for the " + request.id);
		string text = Path.Combine(rootPath, request.id, "layout.jsonc");
		if (request.layout != null && request.layout.HasValues)
		{
			DeleteDraftFiles(request.id);
			SetLayout(text, request.layout, "Responsive", request.viewType, currentCultureName);
		}
		else if (inBulkPublish)
		{
			string text2 = Path.Combine(rootPath, request.id, "layout.designer.draft.jsonc");
			if (contentLoader.Exists(text2))
			{
				SaveDraftToPublished(text2, text);
				DeleteDraftFiles(request.id);
			}
		}
		else
		{
			DeleteDraftFiles(request.id);
		}
	}

	public override bool HasActiveDraft(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "layout.designer.draft.jsonc");
		return contentLoader.Exists(path);
	}

	private void SetLayout(string path, JObject content, string pageType, string viewType, Func<string> currentCultureName)
	{
		contentLoader.WriteAllText(path, CreateDesignerLayout(content, viewType, pageType, currentCultureName));
	}

	private void SaveDraftToPublished(string layoutDesignerDraftPath, string layoutPublishedPath)
	{
		contentLoader.WriteAllText(layoutPublishedPath, contentLoader.ReadAllText(layoutDesignerDraftPath));
	}

	private void DeleteDraftFiles(string viewId)
	{
		logger.Debug("Deleting the draft files after publish");
		string path = Path.Combine(rootPath, viewId, "layout.designer.draft.jsonc");
		string path2 = Path.Combine(rootPath, viewId, "layout.draft.jsonc");
		contentLoader.Delete(path);
		contentLoader.Delete(path2);
	}

	private string CreateDesignerLayout(JObject layout, string viewType, string pageType, Func<string> currentCultureName)
	{
		logger.Debug("Creating the designer schema layout");
		JObject jObject = LoadTemplate(viewType, currentCultureName);
		if (jObject == null)
		{
			throw new Exception("Processing Error : Template is null");
		}
		JObject jObject2 = JObject.FromObject(layout);
		if (jObject2["pageType"] == null)
		{
			jObject2.Add("pageType", pageType);
		}
		else if (!string.IsNullOrEmpty(pageType))
		{
			jObject2["pageType"] = pageType;
		}
		ResetReusableComponentRef(jObject2);
		FlattenLayout flattenLayout = new FlattenLayout();
		jObject["layout"] = flattenLayout.ConvertToFlattenLayout(jObject2);
		if (pageType != "Absolute")
		{
			ResetReusableComboRef(jObject, isPage: false);
		}
		return JsonConvert.SerializeObject(jObject, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
	}

	private JObject LoadTemplate(string viewType, Func<string> currentCultureName)
	{
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			ViewType = viewType
		};
		return JObject.FromObject(new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader).GetTemplate(request, currentCultureName));
	}
}
