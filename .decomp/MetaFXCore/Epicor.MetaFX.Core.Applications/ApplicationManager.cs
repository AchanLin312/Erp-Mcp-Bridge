using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using Epicor.MetaFX.Core.Applications.Component;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Applications;

public class ApplicationManager
{
	private class AdditionalAppInformation
	{
		public bool HasDraft { get; set; }

		public DateTime? LastUpdated { get; set; }
	}

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly string rootPath;

	private readonly HelperMethods helper;

	private const string BASE_FOLDER = "Base";

	private readonly string templateRootPath;

	private static List<ApplicationType> applicationTypes = null;

	private static object lockapplicationTypes = new object();

	private readonly ICacheService cache;

	private readonly IUserDefinedApplicationService userDefinedAppService;

	private readonly ILayerService layerService;

	public ApplicationManager(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService, IUserDefinedApplicationService userDefinedApplicationService = null, ILayerService layerService = null)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		templateRootPath = rootPath + "/shared/templates";
		helper = new HelperMethods(this.logger, contentLoader);
		cache = cacheService;
		userDefinedAppService = userDefinedApplicationService;
		this.layerService = layerService;
	}

	public List<ApplicationSubType> GetApplicationSubTypes(string applicationType)
	{
		return (from ap in (from ap in GetApplicationTypesFromJson()
				where ap.Type.ToLowerInvariant() == applicationType.ToLowerInvariant()
				select ap).SelectMany((ApplicationType st) => st.SubTypes)
			orderby ap.SubType
			select ap).ToList();
	}

	public List<Application> GetApplications(ApplicationRequest request)
	{
		if (request.Type.ToLowerInvariant() == "component")
		{
			return new ComponentManager(rootPath, logger, contentLoader, rootPath + "/shared/components", userDefinedAppService).GetComponents(request);
		}
		ApplicationSubType searchApplicationSubType = null;
		if (!string.IsNullOrEmpty(request.Type))
		{
			searchApplicationSubType = GetApplicationTypesFromJson().FirstOrDefault((ApplicationType appType) => appType.Type.ToLowerInvariant() == request.Type.ToLowerInvariant())?.SubTypes?.FirstOrDefault((ApplicationSubType appType) => appType.SubType.ToLowerInvariant() == request.SubType.ToLowerInvariant());
		}
		List<Application> list = (from app in GetViewApplications(request.SearchText, searchApplicationSubType)
			orderby app.LastUpdated descending
			select app).ToList();
		if (request.IncludeAllLayers)
		{
			if (request.IncludePersLayers)
			{
				GetPersLayersForApplications(request, list);
			}
			else
			{
				GetLayersForApplications(request, list);
			}
		}
		return list;
	}

	public ViewMetadataResponse GetNewApplication(Application request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		helper.Validate(request);
		ApplicationType applicationType = GetApplicationTypesFromJson().FirstOrDefault((ApplicationType appType) => appType.Type.ToLowerInvariant() == request.Type?.ToLowerInvariant());
		if (applicationType == null)
		{
			throw new ValidationException("Application Type is invalid.");
		}
		ApplicationSubType applicationSubType = applicationType.SubTypes.FirstOrDefault((ApplicationSubType appType) => appType.SubType.ToLowerInvariant() == request.SubType?.ToLowerInvariant());
		if (applicationSubType == null)
		{
			throw new ValidationException("Application SubType is invalid.");
		}
		string path = rootPath + "/" + applicationType.FilePath + request.Id;
		if (contentLoader.DirectoryExists(path) && contentLoader.GetFiles(path).Length != 0)
		{
			throw new ValidationException("Id " + request.Id + " already exists.");
		}
		EpMetaFxRequest epMetaFxRequest = new EpMetaFxRequest();
		epMetaFxRequest.properties = new EpMetaFxRequestProperty
		{
			mode = "AppStudio"
		};
		if (request.Type.ToLowerInvariant() == "component")
		{
			epMetaFxRequest.id = ((!string.IsNullOrEmpty(applicationSubType.SubType)) ? applicationSubType.SubType : "component");
			logger.Debug("Get new component " + request.Type + request.SubType + request.Id + ".");
			ViewMetadataResponse component = new ComponentManager(rootPath, logger, contentLoader, templateRootPath).GetComponent(epMetaFxRequest, currentCultureName, localizedStringsFunc);
			userDefinedAppService?.CreateNewRow((component != null) ? request.Id : null);
			return component;
		}
		CreateNewAppFromTemplate(request.Id, request.SubType);
		ViewMetadataResponse newViewApplication = GetNewViewApplication(request, currentCultureName, localizedStringsFunc, epMetaFxRequest, applicationSubType);
		userDefinedAppService?.CreateNewRow((newViewApplication != null) ? request.Id : null);
		return newViewApplication;
	}

	private void CreateNewAppFromTemplate(string id, string subType)
	{
		EpMetaFxSaveRequestEx epMetaFxSaveRequestEx = new EpMetaFxSaveRequestEx();
		epMetaFxSaveRequestEx.id = id;
		epMetaFxSaveRequestEx.subApplicationType = subType;
		string text = rootPath + "/" + epMetaFxSaveRequestEx.id;
		string text2 = rootPath + "/" + GetTemplateFolder(epMetaFxSaveRequestEx.subApplicationType);
		if (!contentLoader.DirectoryExists(text2))
		{
			text2 = rootPath + "/" + GetTemplateFolder("Base");
		}
		if (!contentLoader.DirectoryExists(text2))
		{
			throw new ValidationException("Source directory is invalid.");
		}
		if (!contentLoader.DirectoryExists(text))
		{
			contentLoader.CreateDirectory(text);
		}
		CopyTemplateApplication(epMetaFxSaveRequestEx, text2, text, IsPages: false, updateRef: false);
	}

	internal void CopyTemplateApplication(EpMetaFxSaveRequestEx request)
	{
		string text = rootPath + "/" + request.id;
		if (contentLoader.DirectoryExists(text) || string.IsNullOrEmpty(request.subApplicationType))
		{
			return;
		}
		string text2 = rootPath + "/" + GetTemplateFolder(request.subApplicationType);
		if (!contentLoader.DirectoryExists(text2))
		{
			text2 = rootPath + "/" + GetTemplateFolder("Base");
		}
		contentLoader.CreateDirectory(text);
		CopyTemplateApplication(request, text2, text);
		text2 += "/pages";
		if (contentLoader.DirectoryExists(text2))
		{
			text += "/pages";
			if (!contentLoader.DirectoryExists(text))
			{
				contentLoader.CreateDirectory(text);
				CopyTemplateApplication(request, text2, text, IsPages: true);
			}
		}
	}

	private void CopyTemplateApplication(EpMetaFxSaveRequestEx request, string sourcePath, string targetPath, bool IsPages = false, bool updateRef = true)
	{
		string[] files = Directory.GetFiles(sourcePath, "*.jsonc", SearchOption.TopDirectoryOnly);
		foreach (string text in files)
		{
			string fileName = Path.GetFileName(text);
			string path = Path.Combine(targetPath, fileName);
			JObject jObject = JObject.Parse(File.ReadAllText(text));
			if (updateRef)
			{
				jObject = UpdateContentRef(jObject, request.id, text, IsPages);
			}
			StringBuilder stringBuilder = new StringBuilder();
			JsonSerializer jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented
			});
			using (StringWriter textWriter = new StringWriter(stringBuilder))
			{
				jsonSerializer.Serialize(textWriter, jObject);
			}
			contentLoader.WriteAllText(path, stringBuilder.ToString());
		}
	}

	private JObject UpdateContentRef(JObject content, string viewId, string filePath, bool IsPages = false)
	{
		foreach (JToken item in content.SelectTokens("$..$ref"))
		{
			if (item is JValue jValue)
			{
				string text = jValue.Value.ToString();
				if (!text.StartsWith("#"))
				{
					string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, text));
					Uri uri = new Uri(Path.GetDirectoryName((!IsPages) ? (rootPath + "//" + viewId + "//") : (rootPath + "//" + viewId + "//pages//")) + "/", UriKind.RelativeOrAbsolute);
					Uri uri2 = new Uri(fullPath);
					Uri uri3 = uri.MakeRelativeUri(uri2);
					jValue.Value = Uri.UnescapeDataString(uri3.ToString());
				}
			}
		}
		return content;
	}

	private List<Application> GetViewApplications(string searchText, ApplicationSubType searchApplicationSubType = null)
	{
		List<Application> list = new List<Application>();
		ApplicationType applicationType = GetApplicationTypesFromJson()?.FirstOrDefault((ApplicationType app) => app.Type.ToLowerInvariant() == "view");
		UserDefinedApplicationInfo[] applicationDetails = userDefinedAppService?.GetUserDefinedViews();
		if (applicationType != null)
		{
			Dictionary<string, AdditionalAppInformation> additionalAppInformation = new Dictionary<string, AdditionalAppInformation>();
			string[] directories = contentLoader.GetDirectories(rootPath, delegate(string directory, string jsoncPath, DateTime? lastUpdatedCurrent)
			{
				if (!additionalAppInformation.TryGetValue(directory, out var value2))
				{
					value2 = new AdditionalAppInformation
					{
						HasDraft = false,
						LastUpdated = lastUpdatedCurrent
					};
					additionalAppInformation.Add(directory, value2);
				}
				if (jsoncPath.IndexOf(".draft.jsonc", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					value2.HasDraft = true;
				}
				if (value2.LastUpdated < lastUpdatedCurrent)
				{
					value2.LastUpdated = lastUpdatedCurrent;
				}
			});
			foreach (string text in directories)
			{
				if (string.IsNullOrEmpty(searchText) || text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1)
				{
					ApplicationSubType applicationSubType = GetApplicationSubType(Path.GetFileName(text), applicationType);
					if (!string.IsNullOrEmpty(applicationSubType?.SubType) && (string.IsNullOrEmpty(searchApplicationSubType?.SubType) || !(searchApplicationSubType?.SubType.ToLowerInvariant() != applicationSubType.SubType.ToLowerInvariant())))
					{
						string fileName = Path.GetFileName(text);
						UserDefinedApplicationInfo userDefinedAppInfo = helper.GetUserDefinedAppInfo(applicationDetails, fileName);
						additionalAppInformation.TryGetValue(text, out var value);
						list.Add(new Application
						{
							Id = fileName,
							Type = "view",
							SubType = applicationSubType.SubType,
							IsLayerDisabled = applicationSubType.IsLayerDisabled,
							LastUpdated = ((value != null) ? value.LastUpdated : new DateTime?(Directory.GetLastWriteTime(text))),
							SystemFlag = userDefinedAppInfo.SystemFlag,
							HasDraftContent = HasDraftContent(value, text),
							Layers = null,
							CreatedBy = ((userDefinedAppInfo.SysCharacter02 == string.Empty) ? userDefinedAppInfo.LastUpdatedBy : userDefinedAppInfo.SysCharacter02),
							CanAccessBase = userDefinedAppService?.IfUserHasSecurityAccess(userDefinedAppInfo.SysCharacter01),
							SecurityCode = userDefinedAppInfo.SysCharacter01,
							LastUpdatedBy = userDefinedAppInfo.LastUpdatedBy
						});
					}
				}
			}
		}
		return list;
	}

	private bool HasDraftContent(AdditionalAppInformation appInformation, string folder)
	{
		return appInformation?.HasDraft ?? contentLoader.Exists(Path.Combine(folder, "layout.designer.draft.jsonc"));
	}

	private string GetTemplateFolder(string applicationType)
	{
		return "shared/templates/" + applicationType;
	}

	private ApplicationSubType GetApplicationSubType(string folder, ApplicationType applicationType)
	{
		ApplicationSubType applicationSubType = applicationType?.SubTypes?.FirstOrDefault((ApplicationSubType at) => at.Prefix != null && at.Prefix.Length != 0 && at.Prefix.Any((string pfx) => folder.ToLowerInvariant().StartsWith(pfx.ToLowerInvariant())));
		if (applicationSubType != null)
		{
			return applicationSubType;
		}
		return null;
	}

	private ViewMetadataResponse GetNewViewApplication(Application request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, EpMetaFxRequest metaFxRequest, ApplicationSubType applicationType)
	{
		string id = applicationType.SubType;
		string path = rootPath + "/" + GetTemplateFolder(applicationType.SubType);
		if (!contentLoader.DirectoryExists(path))
		{
			id = "Base";
			path = rootPath + "/" + GetTemplateFolder("Base");
			if (!contentLoader.DirectoryExists(path))
			{
				throw new ValidationException("Template not found for " + applicationType.SubType + ".");
			}
		}
		metaFxRequest.id = id;
		logger.Debug("Get new application " + request.Type + request.SubType + request.Id + ".");
		ViewMetadataResponse result = new ViewManager(templateRootPath, logger, null, cache, contentLoader).Load(metaFxRequest, currentCultureName, localizedStringsFunc);
		logger.Debug("Get new application completed for " + request.Type + request.SubType + request.Id + ".");
		return result;
	}

	private List<ApplicationType> GetApplicationTypesFromJson()
	{
		if (applicationTypes != null && applicationTypes.Count > 0)
		{
			return applicationTypes;
		}
		lock (lockapplicationTypes)
		{
			if (applicationTypes != null)
			{
				return applicationTypes;
			}
			string filePath = helper.GetFilePath(Path.Combine(rootPath, "shared", "templates"), "ApplicationType");
			if (!contentLoader.Exists(filePath))
			{
				applicationTypes = new List<ApplicationType>();
			}
			else
			{
				applicationTypes = (from app in JsonConvert.DeserializeObject<List<ApplicationType>>(contentLoader.ReadAllText(filePath))
					orderby app.Type
					select app).ToList();
			}
		}
		return applicationTypes;
	}

	private void GetLayersForApplications(ApplicationRequest request, List<Application> viewApplications)
	{
		List<EpMetaFxLayer> applicationLayers = GetApplicationLayers(request);
		if (!(applicationLayers?.Any()).Value)
		{
			return;
		}
		foreach (IGrouping<string, EpMetaFxLayer> layerGroup in from x in applicationLayers
			group x by x.ViewId into x
			orderby x.Key
			select x)
		{
			Application view = viewApplications.Where((Application app) => app.Id == layerGroup.Key).FirstOrDefault();
			if (view != null)
			{
				view.Layers = layerGroup.Select((EpMetaFxLayer l) => new EpMetaFxLayerForApplication
				{
					Id = l.ViewId,
					SubType = view.SubType,
					TypeCode = "KNTCCustLayer",
					LastUpdated = l.ChangedOn,
					LastUpdatedBy = l.LastUpdatedBy,
					Company = l.Company,
					LayerName = l.LayerName,
					DeviceType = l.DeviceType,
					CGCCode = l.CGCCode,
					UpgradeError = l.UpgradeError,
					UpgradeStatus = l.UpgradeStatus,
					PublishError = l.PublishError,
					PublishStatus = l.PublishStatus,
					HasDraftContent = l.HasDraftContent,
					IsPublished = !l.WIP,
					SystemFlag = l.SystemFlag
				}).ToList();
			}
		}
	}

	private List<EpMetaFxLayer> GetApplicationLayers(ApplicationRequest request)
	{
		if (request.IncludePersLayers)
		{
			return GetLayers(request, "KNTCPersLayer", request.IncludePersLayers);
		}
		return GetLayers(request, "KNTCCustLayer", isPersonalization: false);
	}

	private List<EpMetaFxLayer> GetLayers(ApplicationRequest request, string typeCode, bool isPersonalization)
	{
		return layerService.GetLayersPaged(new EpMetaFxGetLayersRequest
		{
			IncludeUnpublishedLayers = !request.IsPublished,
			TypeCode = typeCode,
			ViewSearchText = request.SearchText,
			IncludePersLayers = isPersonalization
		});
	}

	private void GetPersLayersForApplications(ApplicationRequest request, List<Application> viewApplications)
	{
		List<EpMetaFxLayer> applicationLayers = GetApplicationLayers(request);
		if (!(applicationLayers?.Any()).Value)
		{
			return;
		}
		foreach (IGrouping<string, EpMetaFxLayer> layerGroup in from x in applicationLayers
			group x by x.ViewId into x
			orderby x.Key
			select x)
		{
			foreach (EpMetaFxLayer layer in layerGroup)
			{
				if (layer.TypeCode == "KineticGridLayout" && !viewApplications.Exists((Application viewApp) => viewApp.Id == layer.ViewId))
				{
					Application item = new Application
					{
						Id = layer.ViewId,
						Type = "view",
						SubType = "Grid",
						LastUpdated = layer.ChangedOn,
						SystemFlag = layer.SystemFlag,
						LastUpdatedBy = layer.LastUpdatedBy,
						HasDraftContent = false,
						CreatedBy = layer.LastUpdatedBy,
						CanAccessBase = false,
						SecurityCode = "",
						Layers = null,
						IsLayerDisabled = false
					};
					viewApplications.Add(item);
				}
			}
			Application view = viewApplications.Where((Application app) => app.Id == layerGroup.Key).FirstOrDefault();
			if (view != null)
			{
				view.Layers = layerGroup.Select((EpMetaFxLayer l) => new EpMetaFxLayerForApplication
				{
					Id = l.ViewId,
					SubType = view.SubType,
					TypeCode = l.TypeCode,
					LastUpdated = l.ChangedOn,
					LastUpdatedBy = l.LastUpdatedBy,
					Company = l.Company,
					LayerName = l.LayerName,
					DeviceType = l.DeviceType,
					CGCCode = l.CGCCode,
					UpgradeError = l.UpgradeError,
					UpgradeStatus = l.UpgradeStatus,
					PublishError = l.PublishError,
					PublishStatus = l.PublishStatus,
					HasDraftContent = l.HasDraftContent,
					IsPublished = !l.WIP,
					SystemFlag = l.SystemFlag
				}).ToList();
			}
		}
	}
}
