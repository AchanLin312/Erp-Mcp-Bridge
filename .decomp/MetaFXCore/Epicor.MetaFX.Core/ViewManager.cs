using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.AppStudio;
using Epicor.MetaFX.Core.AppStudio.LayerManager;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Exceptions;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core;

public class ViewManager
{
	private readonly IFileProvider physicalFileProvider;

	private readonly HelperMethods helper;

	private readonly ILayerService layerService;

	private readonly ILogger logger;

	private readonly PatchDiff patchdiff;

	private readonly List<PatchInfo> patchInfos;

	private readonly Dictionary<string, List<JObject>> PatchRawTokens = new Dictionary<string, List<JObject>>();

	private readonly string rootPath;

	private ICacheService cache;

	private ICacheService temporaryCacheService;

	private readonly IContentLoader contentLoader;

	private bool debug;

	private bool checkDuplicateIds;

	private IJsonMerger jsonMerger;

	private JObject localizedStrings = new JObject();

	private Translation translation;

	public ViewManager(string rootPath, ILogger logger, ILayerService layerService, ICacheService cacheService, IContentLoader contentLoader, IFileProvider fileProvider = null)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.layerService = layerService;
		jsonMerger = new JsonMerger(this.logger, contentLoader, cacheService);
		helper = new HelperMethods(this.logger, contentLoader);
		patchdiff = new PatchDiff(this.logger);
		patchInfos = new List<PatchInfo>();
		cache = cacheService;
		this.contentLoader = contentLoader;
		physicalFileProvider = fileProvider ?? new FileProvider();
	}

	private ICacheService buildTemporaryCacheService(bool appDiffMode)
	{
		if (temporaryCacheService == null)
		{
			ICacheService cacheService2;
			if (!appDiffMode)
			{
				ICacheService cacheService = new FileCacheService(((ICacheLocation)cache).GetCacheLocation, logger, new FileContentLoader());
				cacheService2 = cacheService;
			}
			else
			{
				ICacheService cacheService = new NoCacheService();
				cacheService2 = cacheService;
			}
			temporaryCacheService = cacheService2;
		}
		return temporaryCacheService;
	}

	public ViewMetadataResponse Load(EpMetaFxRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		bool applyPersonalization = request.properties.mode != "AppStudio" && !request.properties.ignorePersonalization;
		return LoadViews(request, currentCultureName, localizedStringsFunc, applyPersonalization, applyPatches: true);
	}

	public string[] GetViews()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string> { "Shared", "Conflicts", "cache" };
		string[] array = physicalFileProvider.DirectoryGetDirectories(rootPath);
		for (int i = 0; i < array.Length; i++)
		{
			string name = new DirectoryInfo(array[i]).Name;
			if (!list2.Contains(name))
			{
				list.Add(name);
			}
		}
		return list.ToArray();
	}

	internal virtual ViewMetadataResponse LoadViews(EpMetaFxRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool applyPersonalization, bool applyPatches, bool includeUnPublishedLayers = true)
	{
		string text = LockController.CreateLock(request.id, startBlocked: true, TimeSpan.FromMinutes(2.0));
		string currentCulture = currentCultureName();
		buildTemporaryCacheService(request.properties.basis == "ApplicationDiff");
		translation = new Translation(temporaryCacheService, logger, contentLoader, jsonMerger);
		bool flag = (jsonMerger.Debug = request.properties.debug);
		debug = flag;
		checkDuplicateIds = request.properties.checkDuplicateIds;
		if (request.properties.mode == "AppStudio")
		{
			cache.Clear();
			cache = new NoCacheService();
		}
		LayerRequest layerRequest = null;
		try
		{
			layerRequest = GetLayerRequest(request, applyPersonalization, applyPatches, includeUnPublishedLayers);
		}
		catch (Exception inner)
		{
			if (applyPersonalization)
			{
				throw new MetaFxException(MetaFxExceptionType.Personalization, "PERS_LOAD_ERR", "Error Loading Personalization", inner);
			}
			throw new MetaFxException(MetaFxExceptionType.Default, "Error Loading Layer", inner);
		}
		if (request.properties.mode == "ApplicationStrings")
		{
			ViewMetadataResponse view = GetView(request, currentCulture, localizedStringsFunc, layerRequest);
			LockController.SafelyReleaseLock(request.id, text);
			return view;
		}
		bool isViewGenerated = false;
		ViewMetadataResponse viewMetadataResponse = cache.Get(cache.BuildResponseCacheKey(currentCulture, request.id, layerRequest.CacheKey), delegate
		{
			isViewGenerated = true;
			return GetView(request, currentCulture, localizedStringsFunc, layerRequest);
		}, fileCheck: true, text);
		string getCacheLocation = ((ICacheLocation)cache).GetCacheLocation;
		if (!string.IsNullOrEmpty(getCacheLocation))
		{
			Directory.Exists(Path.Combine(Directory.GetParent(getCacheLocation).FullName, "pages"));
		}
		else
			_ = 0;
		if (request.properties.mode != "AppStudio")
		{
			if (viewMetadataResponse == null && cache.Exceptions != null && cache.Exceptions.Count > 0)
			{
				List<Exception> innerExceptions = cache.Exceptions.ToList();
				cache.Exceptions.Clear();
				throw new AggregateException("One or more errors occurred.", innerExceptions);
			}
			CheckCorruptCache(viewMetadataResponse, request, text, currentCulture, isViewGenerated, layerRequest);
		}
		LockController.SafelyReleaseLock(request.id, text);
		if (!string.IsNullOrEmpty(request.properties.pageName))
		{
			JToken jToken = viewMetadataResponse.Pages.FirstOrDefault((JToken page) => page["name"].ToString() == request.properties.pageName);
			if (jToken != null)
			{
				if (jToken["viewType"].ToString() == "SlidingPanel")
				{
					viewMetadataResponse.Layout = jToken;
				}
				else
				{
					viewMetadataResponse.Layout = jToken;
					viewMetadataResponse.Pages = new JArray();
				}
			}
		}
		return viewMetadataResponse;
	}

	internal virtual ViewMetadataResponse LoadViewsWithNoLock(EpMetaFxRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool applyPersonalization, bool applyPatches, bool includeUnPublishedLayers = true)
	{
		string currentCulture = currentCultureName();
		buildTemporaryCacheService(request.properties.basis == "ApplicationDiff");
		translation = new Translation(temporaryCacheService, logger, contentLoader, jsonMerger);
		bool flag = (jsonMerger.Debug = request.properties.debug);
		debug = flag;
		checkDuplicateIds = request.properties.checkDuplicateIds;
		if (request.properties.mode == "AppStudio")
		{
			cache.Clear();
			cache = new NoCacheService();
		}
		LayerRequest layerRequest = null;
		try
		{
			layerRequest = GetLayerRequest(request, applyPersonalization, applyPatches, includeUnPublishedLayers);
		}
		catch (Exception inner)
		{
			if (applyPersonalization)
			{
				throw new MetaFxException(MetaFxExceptionType.Personalization, "PERS_LOAD_ERR", "Error Loading Personalization", inner);
			}
			throw new MetaFxException(MetaFxExceptionType.Default, "Error Loading Layer", inner);
		}
		if (request.properties.mode == "ApplicationStrings")
		{
			return GetView(request, currentCulture, localizedStringsFunc, layerRequest);
		}
		bool isViewGenerated = false;
		ViewMetadataResponse viewMetadataResponse = cache.Get(cache.BuildResponseCacheKey(currentCulture, request.id, layerRequest.CacheKey), delegate
		{
			isViewGenerated = true;
			return GetView(request, currentCulture, localizedStringsFunc, layerRequest);
		}, fileCheck: true);
		string getCacheLocation = ((ICacheLocation)cache).GetCacheLocation;
		if (!string.IsNullOrEmpty(getCacheLocation))
		{
			Directory.Exists(Path.Combine(Directory.GetParent(getCacheLocation).FullName, "pages"));
		}
		else
			_ = 0;
		if (request.properties.mode != "AppStudio")
		{
			if (viewMetadataResponse == null && cache.Exceptions != null && cache.Exceptions.Count > 0)
			{
				List<Exception> innerExceptions = cache.Exceptions.ToList();
				cache.Exceptions.Clear();
				throw new AggregateException("One or more errors occurred.", innerExceptions);
			}
			CheckCorruptCache(viewMetadataResponse, request, "", currentCulture, isViewGenerated, layerRequest);
		}
		if (!string.IsNullOrEmpty(request.properties.pageName))
		{
			JToken jToken = viewMetadataResponse.Pages.FirstOrDefault((JToken page) => page["name"].ToString() == request.properties.pageName);
			if (jToken != null)
			{
				if (jToken["viewType"].ToString() == "SlidingPanel")
				{
					viewMetadataResponse.Layout = jToken;
				}
				else
				{
					viewMetadataResponse.Layout = jToken;
					viewMetadataResponse.Pages = new JArray();
				}
			}
		}
		return viewMetadataResponse;
	}

	public void InvalidateCache(EpMetaFxCacheInvalidateRequest request)
	{
		string transactionId = string.Empty;
		try
		{
			transactionId = LockController.CreateLock(request.viewId, startBlocked: true, TimeSpan.FromMinutes(2.0));
			if (!string.IsNullOrEmpty(request.LanguageId) && string.IsNullOrEmpty(request.viewId))
			{
				string[] array = physicalFileProvider.DirectoryGetDirectories(rootPath);
				foreach (string path in array)
				{
					cache.WithNewCacheLocation(Path.Combine(path, "cache"));
					cache.Clear(request.LanguageId);
				}
				logger.Debug("Cache cleared for all applications for language " + request.LanguageId);
			}
			else if (!string.IsNullOrEmpty(request.viewId))
			{
				cache.WithNewCacheLocation(Path.Combine(rootPath, request.viewId, "cache"));
				cache.Clear(request.LanguageId);
				logger.Debug("Cache cleared for application " + request.viewId);
			}
		}
		catch (Exception ex)
		{
			logger.Debug("Error occurred while invalidating the cache - " + ex.Message);
		}
		finally
		{
			LockController.SafelyReleaseLock(request.viewId, transactionId);
		}
	}

	internal Dictionary<string, JToken> GetJsonsToApplyLayer(ViewMetadataResponse viewMetadataResponse)
	{
		Dictionary<string, JToken> dictionary = new Dictionary<string, JToken>();
		if (viewMetadataResponse.Layout != null)
		{
			dictionary.Add("layout", JToken.FromObject(viewMetadataResponse.Layout));
		}
		if (viewMetadataResponse.Rules != null)
		{
			dictionary.Add("rules", JToken.FromObject(viewMetadataResponse.Rules));
		}
		if (viewMetadataResponse.Events != null)
		{
			dictionary.Add("events", JToken.FromObject(viewMetadataResponse.Events));
		}
		if (viewMetadataResponse.ToolBar != null)
		{
			dictionary.Add("tools", JToken.FromObject(viewMetadataResponse.ToolBar));
		}
		if (viewMetadataResponse.DataViews != null)
		{
			dictionary.Add("dataviews", JToken.FromObject(viewMetadataResponse.DataViews));
		}
		if (viewMetadataResponse.Pages != null)
		{
			dynamic pages = viewMetadataResponse.Pages;
			if (pages != null)
			{
				foreach (dynamic item in pages)
				{
					dictionary.Add((string)item.name, item);
				}
			}
		}
		return dictionary;
	}

	private void SaveUpdatedLayers(LayerRequest layerRequest)
	{
		ILayerService obj = layerService;
		if (obj != null && obj.PropertyLevelDiffsEnabled())
		{
			List<EpMetaFxLayer> list = null;
			List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
			if (customizedLayers != null && customizedLayers.Any())
			{
				list = layerRequest.CustomizedLayers;
			}
			if (layerRequest.PersonalizedLayer != null)
			{
				list = ((list != null) ? list.Concat(new List<EpMetaFxLayer> { layerRequest.PersonalizedLayer }).ToList() : new List<EpMetaFxLayer> { layerRequest.PersonalizedLayer });
			}
			if (list != null)
			{
				new LayerManager(rootPath, logger, layerService, contentLoader, cache).SaveUpdatedLayers(list);
			}
		}
	}

	private LayerRequest GetLayerRequest(EpMetaFxRequest request, bool applyPersonalization, bool applyPatches, bool includeUnPublishedLayers)
	{
		LayerManager layerManager = new LayerManager(rootPath, logger, layerService, contentLoader, cache);
		LayerRequest layerRequest = new LayerRequest
		{
			Request = request,
			RootPath = rootPath,
			ApplyPersonalization = applyPersonalization,
			ApplyPatches = applyPatches,
			LayerService = layerService
		};
		CustomizationLayerRequest customizationLayerRequest = new CustomizationLayerRequest
		{
			DeviceType = request.properties.deviceType,
			UserName = request.properties.userName,
			Layers = request.properties.layers,
			ViewId = request.id,
			Company = request.properties.company,
			LayerType = (request.properties.layerType ?? "KNTCCustLayer"),
			CountryGroupCode = request.properties.countryGroupCode,
			CountryCode = request.properties.countryCode,
			IsDesignerMode = (request.properties.mode == "AppStudio"),
			UxAppVersion = request.properties.layerVersion
		};
		if (customizationLayerRequest.LayerType != "KNTCPersLayer")
		{
			customizationLayerRequest.IncludeUnPublishedLayers = ((customizationLayerRequest.IsDesignerMode && includeUnPublishedLayers) ? true : false);
		}
		layerRequest.CustomizedLayers = layerManager.GetCustomizedLayers(customizationLayerRequest);
		if (layerRequest.ApplyPersonalization)
		{
			layerRequest.PersonalizedLayer = layerManager.GetPersonalizedLayer(customizationLayerRequest);
		}
		string cacheKey;
		if (layerRequest.PersonalizedLayer == null)
		{
			List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
			if (customizedLayers == null || !customizedLayers.Any())
			{
				cacheKey = string.Empty;
				goto IL_018a;
			}
		}
		cacheKey = layerManager.GetCacheKey(layerRequest);
		goto IL_018a;
		IL_018a:
		layerRequest.CacheKey = cacheKey;
		return layerRequest;
	}

	private ViewMetadataResponse GetView(EpMetaFxRequest request, string culture, Func<JObject, string, JObject> localizedStringsFunc, LayerRequest layerRequest)
	{
		logger.Debug("Processing view " + request.id);
		bool isUntranslated = true;
		string mode = request.properties.mode;
		bool useLayers = request.properties.layers?.Any() ?? false;
		string filePath = GetFilePath(request.id, "patch");
		jsonMerger.LoadPatchTokens(filePath, rootPath, request.properties.baseAppVersion);
		AssertViewCanBeOpened(request.id);
		JObject jObject = ExtractTranslationStrings(request.id, mode, layerRequest, baseStrings: false, request.properties.basis);
		if (mode != "AppStudio" && request.properties.basis != "ApplicationDiff")
		{
			isUntranslated = false;
		}
		if (jObject != null && request.properties.basis != "ApplicationDiff")
		{
			localizedStrings = localizedStringsFunc(jObject, request.id);
			translation.GenerateNotTranslatedStrings(jObject, localizedStrings, culture);
		}
		if (mode == "ApplicationStrings")
		{
			return GetViewApplicationStrings(request.id, mode, culture, localizedStringsFunc, useLayers, layerRequest);
		}
		ViewMetadataResponse viewMetadataResponse = new ViewMetadataResponse();
		viewMetadataResponse.HasOrphans = false;
		viewMetadataResponse.Orphans = new JArray();
		viewMetadataResponse.Layout = temporaryCacheService.Get(ViewTypeCacheKey(culture, "layout", GetFilePath(request.id, "layout", translation.TranslatedLayout), layerRequest.CacheKey), () => (useLayers || !(mode == "AppStudio") || !LayoutDesignerDraftFilesExists(request.id) || request.properties.baseAppVersion != 0) ? LoadViewType(request.id, "layout", translation.TranslatedLayout, mode, layerRequest, isUntranslated, null, request.properties.baseAppVersion) : LoadViewType(request.id, "layout", "layout.designer.draft.jsonc", mode));
		viewMetadataResponse.Events = temporaryCacheService.Get(ViewTypeCacheKey(culture, "events", GetFilePath(request.id, "events", translation.TranslatedEvents), layerRequest.CacheKey), () => (useLayers || !(mode == "AppStudio") || !EventsDesignerFilesExists(request.id) || request.properties.baseAppVersion != 0) ? LoadViewType(request.id, "events", translation.TranslatedEvents, mode, layerRequest, isUntranslated, null, request.properties.baseAppVersion) : LoadViewType(request.id, "events", "events.draft.jsonc", mode));
		viewMetadataResponse.ToolBar = temporaryCacheService.Get(ViewTypeCacheKey(culture, "tools", GetFilePath(request.id, "tools", translation.TranslatedTools), layerRequest.CacheKey), () => (useLayers || !(mode == "AppStudio") || !ToolsDesignerFilesExists(request.id) || request.properties.baseAppVersion != 0) ? LoadViewType(request.id, "tools", translation.TranslatedTools, null, layerRequest, isUntranslated, null, request.properties.baseAppVersion) : LoadViewType(request.id, "tools", "tools.draft.jsonc", mode));
		viewMetadataResponse.DataViews = temporaryCacheService.Get(ViewTypeCacheKey(culture, "dataviews", GetFilePath(request.id, "dataviews", translation.TranslatedDataViews), layerRequest.CacheKey), () => (useLayers || !(mode == "AppStudio") || !DataViewsDesignerFilesExists(request.id) || request.properties.baseAppVersion != 0) ? LoadViewType(request.id, "dataviews", translation.TranslatedDataViews, null, layerRequest, isUntranslated: true, null, request.properties.baseAppVersion) : LoadViewType(request.id, "dataviews", "dataviews.draft.jsonc", mode));
		viewMetadataResponse.References = LoadViewType(request.id, "references", null, null, layerRequest, isUntranslated, null, request.properties.baseAppVersion);
		viewMetadataResponse.Rules = temporaryCacheService.Get(ViewTypeCacheKey(culture, "rules", GetFilePath(request.id, "rules"), layerRequest.CacheKey), () => (useLayers || !(mode == "AppStudio") || !RulesDesignerFilesExists(request.id) || request.properties.baseAppVersion != 0) ? LoadViewType(request.id, "rules", translation.TranslatedRules, mode, layerRequest, isUntranslated: true, null, request.properties.baseAppVersion) : LoadViewType(request.id, "rules", "rules.draft.jsonc", mode));
		viewMetadataResponse.Pages = temporaryCacheService.Get(ViewTypeCacheKey(culture, "pages", GetFilePath(request.id, "pages", "pages"), layerRequest.CacheKey), () => LoadPages(request.id, useLayers, layerRequest, mode, request.properties.baseAppVersion));
		SaveUpdatedLayers(layerRequest);
		if (mode == "AppStudio")
		{
			viewMetadataResponse = new ViewDesignerMetadataResponse
			{
				Layout = viewMetadataResponse.Layout,
				Events = viewMetadataResponse.Events,
				ToolBar = viewMetadataResponse.ToolBar,
				DataViews = viewMetadataResponse.DataViews,
				References = ((request.properties.basis == "ApplicationDiff") ? null : viewMetadataResponse.References),
				Rules = viewMetadataResponse.Rules,
				PatchInfo = ((request.properties.basis == "ApplicationDiff") ? null : patchInfos),
				Pages = viewMetadataResponse.Pages,
				PatchTokens = ((request.properties.basis == "ApplicationDiff") ? null : PatchRawTokens),
				HasOrphans = false,
				Orphans = new JArray()
			};
			if (request.properties.basis != "ApplicationDiff" && !useLayers)
			{
				if (LayoutDraftFilesExists(request.id))
				{
					((ViewDesignerMetadataResponse)viewMetadataResponse).DraftLayout = LoadViewType(request.id, "layout", "layout.draft.jsonc", mode, null, isUntranslated: true, null, request.properties.baseAppVersion);
				}
				if (RulesDesignerFilesExists(request.id))
				{
					((ViewDesignerMetadataResponse)viewMetadataResponse).DraftRules = LoadViewType(request.id, "rules", "rules.draft.jsonc", mode, null, isUntranslated: true, null, request.properties.baseAppVersion);
				}
				if (EventsDesignerFilesExists(request.id))
				{
					((ViewDesignerMetadataResponse)viewMetadataResponse).DraftEvents = LoadViewType(request.id, "events", "events.draft.jsonc", mode, null, isUntranslated: true, null, request.properties.baseAppVersion);
				}
				if (ToolsDesignerFilesExists(request.id))
				{
					((ViewDesignerMetadataResponse)viewMetadataResponse).ToolBar = LoadViewType(request.id, "tools", "tools.draft.jsonc", mode, null, isUntranslated: true, null, request.properties.baseAppVersion);
				}
			}
			viewMetadataResponse.Orphans = LoadOrphanPages(viewMetadataResponse, request.id, useLayers, layerRequest, mode);
		}
		else
		{
			viewMetadataResponse.HasOrphans = OrphansManager.HasPagesFlaggedWithOrphans(layerRequest, viewMetadataResponse.Pages);
		}
		if (!debug)
		{
			temporaryCacheService.ClearTransientCache();
		}
		return viewMetadataResponse;
	}

	private JObject ExtractTranslationStrings(string viewid, string mode, LayerRequest layerRequest, bool baseStrings = false, string basis = null)
	{
		JObject jObject = jsonMerger.LoadStrings(helper.GetFilePath(Path.Combine(rootPath, viewid), "strings"));
		if (mode != "AppStudio" && basis != "ApplicationDiff")
		{
			translation.ExtractTranslatableText(helper.GetFilePath(Path.Combine(rootPath, viewid), "layout"), "layout", layerRequest, baseStrings);
			translation.ExtractTranslatableText(helper.GetFilePath(Path.Combine(rootPath, viewid), "tools"), "tools", layerRequest, baseStrings);
			string filePath = helper.GetFilePath(Path.Combine(rootPath, viewid), "dataviews");
			if (contentLoader.Exists(filePath))
			{
				translation.ExtractTranslatableText(filePath, "dataviews", layerRequest, baseStrings);
			}
			string path = Path.Combine(rootPath, viewid, "pages");
			new JArray();
			if (contentLoader.DirectoryExists(path))
			{
				foreach (string item in from p in contentLoader.GetFiles(path)
					where p.EndsWith("layout.jsonc")
					select p)
				{
					translation.ExtractTranslatableText(item, "pages", layerRequest, baseStrings, mode);
				}
			}
			string filePath2 = helper.GetFilePath(Path.Combine(rootPath, viewid), "events");
			if (contentLoader.Exists(filePath2))
			{
				translation.ExtractTranslatableText(filePath2, "events", layerRequest, baseStrings);
			}
			jObject = translation.MergeWithGeneratedString(jObject);
		}
		return jObject;
	}

	private ViewMetadataResponse GetViewApplicationStrings(string viewid, string mode, string culture, Func<JObject, string, JObject> localizedStringsFunc, bool useLayers, LayerRequest layerRequest)
	{
		bool flag = layerRequest?.CustomizedLayers.Any() ?? false;
		if (flag)
		{
			translation.ResetTranlatableStings();
			ExtractTranslationStrings(viewid, mode, null, baseStrings: true);
		}
		ViewMetadataResponse result = new ViewMetadataResponse
		{
			ApplicationStrings = translation.GetApplicationStrings(flag)
		};
		if (!debug)
		{
			temporaryCacheService.ClearTransientCache();
		}
		return result;
	}

	private object GetPage(string viewid, string mode, string page, LayerRequest layerRequest)
	{
		logger.Debug("Processing page " + page);
		string directoryPath = Path.Combine(rootPath, viewid, "pages");
		if (physicalFileProvider.DirectoryExists(directoryPath))
		{
			string fileName = physicalFileProvider.DirectoryGetFiles(directoryPath).FirstOrDefault((string p) => p.EndsWith(page + ".layout.jsonc"));
			return LoadViewType(viewid, "layout", fileName, mode, layerRequest, isUntranslated: true, page);
		}
		return null;
	}

	private JArray LoadPages(string viewid, bool useLayers, LayerRequest layerRequest, [Optional] string mode, int uxAppVersion = 0)
	{
		string path = Path.Combine(rootPath, viewid, "pages");
		bool flag = mode == "AppStudio" && LayoutDesignerDraftFilesExists(viewid) && !useLayers;
		JArray jArray = new JArray();
		if (contentLoader.DirectoryExists(path))
		{
			string[] files = contentLoader.GetFiles(path);
			if (checkDuplicateIds)
			{
				CheckDuplicateId(flag, rootPath + "/" + viewid + "/");
			}
			for (int i = 0; i < files.Length; i++)
			{
				string fileName = Path.GetFileName(files[i]);
				string pageName = string.Empty;
				string fileName2 = Path.GetFileName(files[i]).Replace(Path.GetExtension(files[i]), "");
				if ((((fileName != null) & flag) && !fileName.EndsWith("draft.jsonc")) || (fileName != null && !flag && fileName.EndsWith("draft.jsonc")))
				{
					continue;
				}
				if (contentLoader.Exists(files[i]))
				{
					dynamic val = JObject.Parse(contentLoader.ReadAllText(files[i], uxAppVersion), new JsonLoadSettings
					{
						CommentHandling = CommentHandling.Ignore
					})["layout"];
					pageName = (string)val.name;
				}
				if (!translation.TranslatedPages.Any())
				{
					dynamic val2 = LoadViewType(viewid, "layout", files[i], mode, flag ? null : layerRequest, isUntranslated: true, pageName, uxAppVersion);
					if (!((val2 == null) ? true : false))
					{
						jArray.Add(val2);
					}
					continue;
				}
				string fileName3 = translation.TranslatedPages.FirstOrDefault((string x) => x.Contains("page." + fileName2));
				dynamic val3 = LoadViewType(viewid, "layout", fileName3, mode, flag ? null : layerRequest, isUntranslated: false, pageName, uxAppVersion);
				if (!((val3 == null) ? true : false))
				{
					jArray.Add(val3);
				}
			}
		}
		if (!flag && layerRequest != null)
		{
			if (layerRequest.PersonalizedLayer == null)
			{
				List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
				if (customizedLayers == null || !customizedLayers.Any())
				{
					goto IL_052c;
				}
			}
			Dictionary<string, JToken> baseContents = new Dictionary<string, JToken>();
			JArray jArray2 = ApplyCustomizedPatchForPages(baseContents, layerRequest);
			for (int num = 0; num < jArray2.ToList().Count(); num++)
			{
				dynamic data = jArray2[num];
				if (!jArray.Any((JToken p) => p["name"]?.ToString() == data["name"]?.ToString()))
				{
					jArray.Add(data);
				}
			}
		}
		goto IL_052c;
		IL_052c:
		return jArray;
	}

	private JArray LoadOrphanPages(ViewMetadataResponse response, string viewid, bool useLayers, LayerRequest layerRequest, [Optional] string mode)
	{
		JArray pages = new JArray();
		try
		{
			if (layerRequest != null)
			{
				List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
				if (customizedLayers != null && customizedLayers.Any())
				{
					List<string> orphanPagesAfterLoad = OrphansManager.GetOrphanPagesAfterLoad(layerRequest, response.Pages);
					JArray patchedPages = new JArray();
					foreach (string item in orphanPagesAfterLoad)
					{
						Dictionary<string, JToken> baseContents = OrphansManager.CreateOrphanPageContent(logger, item);
						ApplyCustomizedPatchForPages(baseContents, layerRequest, item).ToList().ForEach(delegate(JToken patchedPage)
						{
							patchedPages.Add(patchedPage);
						});
					}
					foreach (JToken item2 in patchedPages)
					{
						if (OrphansManager.JComponents(item2).Any())
						{
							string patchedPageName = FlattenLayout.GetJPropValue(item2, "name", "");
							if (!pages.Any((JToken p) => FlattenLayout.GetJPropValue(p, "name", "") == patchedPageName))
							{
								pages.Add(item2);
							}
						}
					}
				}
			}
			OrphansManager.GetOrphanPagesFromPages(logger, response.Pages).ToList().ForEach(delegate(JToken orphanedPage)
			{
				pages.Add(orphanedPage);
			});
			response.HasOrphans = pages.Any();
		}
		catch (Exception ex)
		{
			logger.Debug("Error processing orphans: " + ex.Message);
			throw;
		}
		return pages;
	}

	private void CheckDuplicateId(bool draft, string path)
	{
		string path2 = (draft ? "layout.designer.draft.jsonc" : "layout.jsonc");
		List<string> ids = new List<string>();
		List<string> duplicateIds = new List<string>();
		helper.CheckDuplicateId(ref ids, GetComponents(path2), path2, ref duplicateIds);
		string[] files = contentLoader.GetFiles(Path.Combine(path, "pages"));
		files = (draft ? files.Where((string f) => f.EndsWith("layout.designer.draft.jsonc")).ToArray() : files.Where((string f) => f.EndsWith("layout.jsonc")).ToArray());
		string[] array = files;
		foreach (string path3 in array)
		{
			helper.CheckDuplicateId(ref ids, GetComponents(path3), path3, ref duplicateIds);
		}
		if (duplicateIds.Count > 0)
		{
			throw new DataMisalignedException(string.Join("\n", duplicateIds));
		}
	}

	private JToken GetComponents(string path)
	{
		if (!contentLoader.Exists(path))
		{
			return null;
		}
		return JObject.Parse(contentLoader.ReadAllText(path))?["layout"]?["components"];
	}

	private object LoadViewType(string viewid, string viewType, [Optional] string fileName, [Optional] string mode, [Optional] LayerRequest layerRequest, bool isUntranslated = true, string pageName = null, int uxAppVersion = 0)
	{
		string filePath = GetFilePath(viewid, viewType, fileName);
		JToken jToken = LoadViewContent(filePath, viewType, layerRequest, isUntranslated, pageName, uxAppVersion)?[viewType];
		if (mode == "AppStudio" && jToken != null)
		{
			if (viewType == "layout")
			{
				patchInfos.AddRange(GetPatchInfo(viewType, jToken));
			}
			if (!PatchRawTokens.ContainsKey(viewType))
			{
				PatchRawTokens.Add(viewType, jsonMerger.RawTokens);
			}
		}
		return jToken;
	}

	private JArray ApplyCustomizedPatchForPages(Dictionary<string, JToken> baseContents, LayerRequest layerRequest, string pageName = "")
	{
		JArray jArray = new JArray();
		Dictionary<string, JToken> dictionary = new LayerManager(rootPath, logger, layerService, contentLoader, cache).ApplyCustomizedViews(baseContents, layerRequest, "pages", pageName);
		if (dictionary == null || !dictionary.Any())
		{
			return jArray;
		}
		foreach (KeyValuePair<string, JToken> item2 in dictionary)
		{
			JObject item = JObject.Parse(item2.Value.ToString(), new JsonLoadSettings
			{
				CommentHandling = CommentHandling.Ignore
			});
			jArray.Add(item);
		}
		return jArray;
	}

	private JObject LoadViewContent(string filePath, string viewType, LayerRequest layerRequest = null, bool isUntranslated = true, string pageName = null, int uxAppVersion = 0)
	{
		if (contentLoader.Exists(filePath))
		{
			IJsonMerger obj = jsonMerger;
			JObject jObject = localizedStrings;
			bool isUntranslated2 = isUntranslated;
			return obj.Load(filePath, viewType, jObject, shouldConvertToHierarchicalStructure: true, layerRequest, pageName, isUntranslated2, uxAppVersion);
		}
		return null;
	}

	private IEnumerable<PatchInfo> GetPatchInfo(string viewType, JToken newjson)
	{
		return patchdiff.GetPatchInfo(viewType, jsonMerger.RawJson, newjson);
	}

	private string ViewTypeCacheKey(string currentCultureName, string viewType, string filePath, string layerCacheKey)
	{
		if (string.IsNullOrEmpty(layerCacheKey))
		{
			return currentCultureName + "." + viewType + ".json";
		}
		return currentCultureName + "." + viewType + "." + layerCacheKey + ".json";
	}

	private string GetFilePath(string viewid, string viewType, [Optional] string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return helper.GetFilePath(Path.Combine(rootPath, viewid), viewType);
		}
		if (fileName.StartsWith(rootPath))
		{
			return fileName;
		}
		return Path.Combine(rootPath, viewid, fileName);
	}

	private bool LayoutDesignerDraftFilesExists(string viewId)
	{
		return contentLoader.Exists(Path.Combine(rootPath, viewId, "layout.designer.draft.jsonc"));
	}

	private bool LayoutDraftFilesExists(string viewId)
	{
		return contentLoader.Exists(Path.Combine(rootPath, viewId, "layout.draft.jsonc"));
	}

	private bool RulesDesignerFilesExists(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "rules.draft.jsonc");
		return contentLoader.Exists(path);
	}

	private bool EventsDesignerFilesExists(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "events.draft.jsonc");
		return contentLoader.Exists(path);
	}

	private bool ToolsDesignerFilesExists(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "tools.draft.jsonc");
		return contentLoader.Exists(path);
	}

	private bool DataViewsDesignerFilesExists(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, "dataviews.draft.jsonc");
		return contentLoader.Exists(path);
	}

	private void AssertViewCanBeOpened(string viewid)
	{
		string filePath = GetFilePath(viewid, "layout");
		if (!contentLoader.Exists(filePath))
		{
			throw new AppNotFoundException("Application " + viewid + " is not accessible. Please contact your System Administrator.");
		}
	}

	private void CheckCorruptCache(ViewMetadataResponse view, EpMetaFxRequest request, string transactionId, string currentCulture, bool isViewGenerated, LayerRequest layerRequest)
	{
		if (view.Layout == null || view.ToolBar == null || view.DataViews == null)
		{
			MethodBase method = new StackTrace().GetFrame(2).GetMethod();
			object value = new
			{
				transactionId = transactionId,
				caller = method.Name,
				request = JsonConvert.SerializeObject(request).Replace('"', '\''),
				currentCulture = currentCulture,
				cacheSource = (isViewGenerated ? "Generated" : "Cached")
			};
			if (!isViewGenerated)
			{
				cache.Clear();
			}
			logger.Debug("Cache corrupted and was not saved to FileSystem/Database - Error details - " + JsonConvert.SerializeObject(value).Replace('"', '\''));
			LockController.SafelyReleaseLock(request.id, transactionId);
			throw new MetaFxException(MetaFxExceptionType.CorruptedCache, "The view could not be loaded. Please try to open the form again. If this error persists contact your system administrator.");
		}
	}

	public object LoadBaseMetaData(string appName, BaseMetaDataType type = BaseMetaDataType.Rules)
	{
		string viewType = type.ToString().ToLower();
		if (type == BaseMetaDataType.Rules)
		{
			return LoadViewType(appName, viewType);
		}
		return new { };
	}

	public void DeleteDrafts(EpMetaFxRequest request)
	{
		contentLoader.DiscardDraft(request, rootPath);
		string[] layers = request.properties.layers;
		if (layers != null && layers.Length != 0)
		{
			layerService.ClearLayerDraft(request);
		}
	}
}
