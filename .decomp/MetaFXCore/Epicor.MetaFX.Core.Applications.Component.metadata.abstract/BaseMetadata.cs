using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Applications.Component.metadata.@abstract;

internal abstract class BaseMetadata
{
	protected readonly ILogger logger;

	protected readonly string rootPath;

	protected readonly string componentRootPath;

	protected readonly IContentLoader contentLoader;

	protected readonly string draft_file_name;

	protected readonly string base_file;

	protected readonly string _type;

	private string control = string.Empty;

	protected BaseMetadata(string rootPath, ILogger logger, string _type, string componentRootPath, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.componentRootPath = componentRootPath;
		this.contentLoader = contentLoader;
		this._type = _type;
		draft_file_name = ((_type == "layout") ? (_type + ".designer.draft.jsonc") : (_type + ".draft.jsonc"));
		base_file = _type + ".jsonc";
	}

	public abstract void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc);

	public abstract void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc);

	protected JObject GetComponentTemplate(Func<string> currentCultureName, string componentId)
	{
		string path = Path.Combine(componentRootPath, componentId, base_file);
		string path2 = Path.Combine(componentRootPath, componentId, draft_file_name);
		if (contentLoader.Exists(path2))
		{
			return JObject.Parse(contentLoader.ReadAllText(path2));
		}
		if (contentLoader.Exists(path))
		{
			return JObject.Parse(contentLoader.ReadAllText(path));
		}
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			TemplateType = "component"
		};
		return JObject.FromObject(new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader).LoadTypesDefaultContent(request, _type, currentCultureName));
	}

	protected void DeleteDraftFile(string draftFilePath)
	{
		if (contentLoader.Exists(draftFilePath))
		{
			contentLoader.Delete(draftFilePath);
		}
	}

	protected void ResetReusableComboRef(JObject layout, bool isPage)
	{
		foreach (JToken item in (IEnumerable<JToken>)layout["layout"]["components"])
		{
			UpdateReusableComboRef(item as JObject, isPage);
		}
	}

	private void UpdateReusableComboRef(JObject model, bool isPage)
	{
		if (model == null)
		{
			return;
		}
		foreach (JProperty item in model.Properties())
		{
			if (item.Name == "appDesignerComboType" && !string.IsNullOrEmpty(item.Value.ToString()))
			{
				RemoveExpandedReusableComboProps(model, isPage);
				break;
			}
			if (item.Value is JObject)
			{
				UpdateReusableComboRef(item.Value as JObject, isPage);
			}
			if (!(item.Value is JArray))
			{
				continue;
			}
			foreach (JToken item2 in item.Value as JArray)
			{
				UpdateReusableComboRef(item2 as JObject, isPage);
			}
		}
	}

	private void RemoveExpandedReusableComboProps(JObject model, bool isPage)
	{
		List<string> requiredProperties = new List<string> { "epBindingTextColumn", "filtersAppend", "filtersParams", "hiddenColumnsAppend", "$ref" };
		List<string> reusableComboProperties = new List<string>
		{
			"comboId", "svc", "svcPath", "tableName", "dropDownStyle", "valueField", "textField", "viewName", "sort", "columns",
			"filters", "returnObj", "required", "isEpiReadOnly", "appendList", "isDelimited", "parameterName", "retrieveColumns", "hiddenColumns", "type",
			"subType"
		};
		if (string.IsNullOrEmpty(model["subType"]?.ToString()))
		{
			model["$ref"] = model["appDesignerComboType"];
		}
		model.Properties().ToList().ForEach(delegate(JProperty prop)
		{
			if (reusableComboProperties.Contains(prop.Name))
			{
				prop.Remove();
			}
			else if (requiredProperties.Contains(prop.Name) && (string.IsNullOrEmpty(prop.Value?.ToString()) || prop.Value.ToString() == "[]"))
			{
				prop.Remove();
			}
		});
	}
}
