using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Applications.Component.metadata;
using Epicor.MetaFX.Core.Applications.Component.metadata.@abstract;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Applications.Component;

public class ComponentManager
{
	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly string componentRootPath;

	private readonly List<BaseMetadata> metadatas;

	private readonly HelperMethods helper;

	private readonly string rootPath;

	private readonly IUserDefinedApplicationService userDefinedAppService;

	public ComponentManager(string rootPath, ILogger logger, IContentLoader contentLoader, string componentRootPath = "", IUserDefinedApplicationService userDefinedApplicationService = null)
	{
		this.logger = logger;
		this.contentLoader = contentLoader;
		helper = new HelperMethods(this.logger, contentLoader);
		this.rootPath = rootPath;
		this.componentRootPath = ((!string.IsNullOrEmpty(componentRootPath)) ? componentRootPath : (rootPath + "/shared/components"));
		metadatas = new List<BaseMetadata>
		{
			new PropertiesMetadata(rootPath, logger, this.componentRootPath, contentLoader),
			new LayoutMetadata(rootPath, logger, this.componentRootPath, contentLoader)
		};
		userDefinedAppService = userDefinedApplicationService;
	}

	public List<Application> GetComponents(ApplicationRequest request)
	{
		List<Application> list = new List<Application>();
		if (!contentLoader.DirectoryExists(componentRootPath))
		{
			logger.Debug("Processing Error : Components folder not found");
			return list;
		}
		UserDefinedApplicationInfo[] applicationDetails = userDefinedAppService?.GetUserDefinedViews();
		string[] directories = contentLoader.GetDirectories(componentRootPath);
		foreach (string text in directories)
		{
			string folderPath = (text.StartsWith(rootPath) ? text : Path.Combine(rootPath, text));
			if (!request.IsPublished || contentLoader.Exists(helper.GetFilePath(folderPath, "layout")))
			{
				string fileName = Path.GetFileName(text);
				UserDefinedApplicationInfo userDefinedAppInfo = helper.GetUserDefinedAppInfo(applicationDetails, fileName);
				list.Add(new Application
				{
					Id = fileName,
					Type = "component",
					SubType = "component",
					LastUpdated = Directory.GetLastWriteTime(text),
					SystemFlag = userDefinedAppInfo.SystemFlag,
					CreatedBy = ((userDefinedAppInfo.SysCharacter02 == string.Empty) ? userDefinedAppInfo.LastUpdatedBy : userDefinedAppInfo.SysCharacter02),
					CanAccessBase = userDefinedAppService?.IfUserHasSecurityAccess(userDefinedAppInfo.SysCharacter01),
					SecurityCode = userDefinedAppInfo.SysCharacter01
				});
			}
		}
		return list.OrderByDescending((Application comp) => comp.LastUpdated).ToList();
	}

	public ViewMetadataResponse GetComponent(EpMetaFxRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		currentCultureName();
		if (!contentLoader.DirectoryExists(Path.Combine(componentRootPath, request.id)))
		{
			logger.Debug("Processing Error : No folder found with componentId - " + request.id);
			throw new DirectoryNotFoundException("Processing Error : No folder found with componentId - " + request.id);
		}
		return GetComponentDetails(request.id, localizedStringsFunc);
	}

	private ViewMetadataResponse GetComponentDetails(string componentId, Func<JObject, string, JObject> localizedStringsFunc)
	{
		JsonMerger jsonMerger = new JsonMerger(logger, contentLoader, null);
		JObject localizedStrings = new JObject();
		logger.Debug("Processing component " + componentId);
		ViewMetadataResponse viewMetadataResponse = new ViewMetadataResponse();
		if (IsDraftFileExists(componentId, "layout.designer.draft.jsonc"))
		{
			viewMetadataResponse.Layout = jsonMerger.Load(GetFilePath(componentId, "layout.designer.draft.jsonc"), "layout", localizedStrings)?["layout"];
		}
		else
		{
			viewMetadataResponse.Layout = jsonMerger.Load(GetFilePath(componentId, "layout.jsonc"), "layout", localizedStrings)?["layout"];
		}
		viewMetadataResponse.Properties = (IsDraftFileExists(componentId, "properties.draft.jsonc") ? GetProperties(GetFilePath(componentId, "properties.draft.jsonc")) : GetProperties(GetFilePath(componentId, "properties.jsonc")));
		return viewMetadataResponse;
	}

	public void SaveComponent(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		metadatas.ForEach(delegate(BaseMetadata designer)
		{
			designer.Save(request, currentCultureName, localizedStringsFunc);
		});
	}

	public void PublishComponent(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		metadatas.ForEach(delegate(BaseMetadata designer)
		{
			designer.Publish(request, currentCultureName, localizedStringsFunc);
		});
	}

	public bool ComponentExists(string componentId)
	{
		return contentLoader.DirectoryExists(Path.Combine(componentRootPath, componentId));
	}

	private bool IsDraftFileExists(string componentId, string fileName)
	{
		return contentLoader.Exists(Path.Combine(componentRootPath, componentId, fileName));
	}

	private string GetFilePath(string componentId, string fileName)
	{
		return Path.Combine(componentRootPath, componentId, fileName);
	}

	private JArray GetProperties(string filePath)
	{
		return JObject.Parse(contentLoader.ReadAllText(filePath))["properties"] as JArray;
	}
}
