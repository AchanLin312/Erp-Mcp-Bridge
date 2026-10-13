using System;
using System.IO;
using Epicor.MetaFX.Core.Applications.Component.metadata.@abstract;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Applications.Component.metadata;

internal class LayoutMetadata : BaseMetadata
{
	public LayoutMetadata(string rootPath, ILogger logger, string componentRootPath, IContentLoader contentLoader)
		: base(rootPath, logger, "layout", componentRootPath, contentLoader)
	{
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("save component layout " + request.id);
		string path = Path.Combine(componentRootPath, request.id, "layout.designer.draft.jsonc");
		if (request.layout != null && request.layout.HasValues)
		{
			SetComponent(path, request.layout, currentCultureName, request.id);
		}
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("publish component layout " + request.id);
		DeleteDraftFile(Path.Combine(componentRootPath, request.id, "layout.designer.draft.jsonc"));
		string path = Path.Combine(componentRootPath, request.id, "layout.jsonc");
		if (request.layout != null && request.layout.HasValues)
		{
			SetComponent(path, request.layout, currentCultureName, request.id);
		}
	}

	private void SetComponent(string path, JObject content, Func<string> currentCultureName, string componentId)
	{
		string contents = CreateDesignerComponent(content, currentCultureName, componentId, "Responsive");
		contentLoader.WriteAllText(path, contents);
	}

	private string CreateDesignerComponent(JObject content, Func<string> currentCultureName, string componentId, string pageType)
	{
		logger.Debug("Creating the designer schema layout for component");
		JObject componentTemplate = GetComponentTemplate(currentCultureName, componentId);
		if (componentTemplate == null)
		{
			throw new Exception("Processing Error : Component layout template is null");
		}
		JObject layoutTree = JObject.FromObject(content);
		FlattenLayout flattenLayout = new FlattenLayout();
		componentTemplate[_type] = flattenLayout.ConvertToFlattenLayout(layoutTree);
		if (pageType != "Absolute")
		{
			ResetReusableComboRef(componentTemplate, isPage: false);
		}
		return JsonConvert.SerializeObject(componentTemplate, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
	}
}
