using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Applications.Component.metadata.@abstract;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Applications.Component.metadata;

internal class PropertiesMetadata : BaseMetadata
{
	public PropertiesMetadata(string rootPath, ILogger logger, string componentRootPath, IContentLoader contentLoader)
		: base(rootPath, logger, "properties", componentRootPath, contentLoader)
	{
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("save component properties " + request.id);
		ValidateProperties(request.properties);
		CreateNewcomponentDirectory(componentRootPath, request.id);
		string path = Path.Combine(componentRootPath, request.id, "properties.draft.jsonc");
		if (request.properties != null)
		{
			SetProperties(path, request.properties, currentCultureName, request.id);
		}
	}

	public override void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("publish component properties " + request.id);
		ValidateProperties(request.properties);
		CreateNewcomponentDirectory(componentRootPath, request.id);
		DeleteDraftFile(Path.Combine(componentRootPath, request.id, "properties.draft.jsonc"));
		string path = Path.Combine(componentRootPath, request.id, "properties.jsonc");
		if (request.properties != null)
		{
			SetProperties(path, request.properties, currentCultureName, request.id);
		}
	}

	private void SetProperties(string path, JArray content, Func<string> currentCultureName, string componentId)
	{
		string contents = CreateDesignerComponentProperties(content, currentCultureName, componentId);
		contentLoader.WriteAllText(path, contents);
	}

	private string CreateDesignerComponentProperties(JArray content, Func<string> currentCultureName, string componentId)
	{
		logger.Debug("Creating the designer schema properties for component");
		JObject obj = GetComponentTemplate(currentCultureName, componentId) ?? throw new Exception("Processing Error : Component properties template is null");
		obj[_type] = content;
		return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
	}

	private void ValidateProperties(JArray content)
	{
		if (content != null && content.HasValues)
		{
			List<Properties>? source = content.ToObject<List<Properties>>();
			if (source.Count((Properties x) => string.IsNullOrEmpty(x.ControlId) || string.IsNullOrEmpty(x.ControlPropertyName) || string.IsNullOrEmpty(x.ExposedPropertyCaption)) > 0)
			{
				logger.Debug("Empty fields found the properties object.");
				throw new Exception("All fields in the properties are mandatory.");
			}
			if ((from i in source
				group i by new { i.ControlId, i.ControlPropertyName }).Count(g => g.Count() > 1) > 0)
			{
				logger.Debug("Duplicate Properties matching ControlID and PropertyName exists.");
				throw new Exception("Duplicate properties exists.");
			}
		}
	}

	private void CreateNewcomponentDirectory(string componentRootPath, string ComponentId)
	{
		if (!contentLoader.DirectoryExists(Path.Combine(componentRootPath, ComponentId)))
		{
			Directory.CreateDirectory(Path.Combine(componentRootPath, ComponentId));
		}
	}
}
