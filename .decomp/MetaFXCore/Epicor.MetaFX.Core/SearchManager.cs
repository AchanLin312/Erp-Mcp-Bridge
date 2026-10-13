using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core;

public class SearchManager
{
	private readonly IContentLoader contentLoader;

	private readonly HelperMethods helper;

	private readonly JsonMerger jsonMerger;

	private readonly ILogger logger;

	private readonly string rootPath;

	private ICacheService cache;

	private JObject localizedStrings = new JObject();

	private Translation translation;

	private readonly Lazy<FileCacheService> temporaryCacheService;

	private readonly IFileProvider physicalFileProvider;

	public SearchManager(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService, IFileProvider fileProvider = null)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		helper = new HelperMethods(this.logger, contentLoader);
		jsonMerger = new JsonMerger(this.logger, contentLoader, cacheService);
		cache = cacheService;
		temporaryCacheService = new Lazy<FileCacheService>(() => new FileCacheService(((ICacheLocation)cacheService).GetCacheLocation, logger, new FileContentLoader()));
		physicalFileProvider = fileProvider ?? new FileProvider();
	}

	public object Load(EpMetaFxSearchRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		string currentCulture = currentCultureName();
		translation = new Translation(temporaryCacheService.Value, logger, contentLoader, jsonMerger);
		return cache.Get(cache.BuildResponseCacheKey(currentCulture, request.like + "." + request.properties.SearchForm, string.Empty), delegate
		{
			temporaryCacheService.Value.ClearTransientCache();
			return GetSearch(request, currentCulture, localizedStringsFunc);
		});
	}

	private object GetSearch(EpMetaFxSearchRequest request, string culture, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("Processing search " + request.like);
		string searchFormPath = Path.Combine(rootPath, "shared", "search", request.like, request.properties.SearchForm);
		bool debug = request.properties.debug;
		JObject stringsJson = jsonMerger.LoadStrings(helper.GetFilePath(searchFormPath, "strings"));
		translation.ExtractTranslatableText(helper.GetFilePath(searchFormPath, "search"), "search");
		stringsJson = translation.MergeWithGeneratedString(stringsJson);
		if (stringsJson != null)
		{
			localizedStrings = localizedStringsFunc(stringsJson, "");
			translation.GenerateNotTranslatedStrings(stringsJson, localizedStrings, culture);
		}
		string filePath = helper.GetFilePath(searchFormPath, "search");
		string filePath2 = helper.GetFilePath(searchFormPath, "dataviews");
		string filePath3 = helper.GetFilePath(searchFormPath, "rules");
		string filePath4 = helper.GetFilePath(searchFormPath, "events");
		logger.Debug("Loading search metadata " + filePath);
		SearchMetaDataResponse result = new SearchMetaDataResponse
		{
			SearchMetadata = temporaryCacheService.Value.Get(ViewTypeCacheKey(culture, "search", GetFilePath(searchFormPath, "search", translation.TranslatedSearch)), () => LoadViewType(searchFormPath, "search", translation.TranslatedSearch)),
			DataViews = temporaryCacheService.Value.Get(ViewTypeCacheKey(culture, "dataviews", filePath2), () => LoadViewType(searchFormPath, "dataviews")),
			Rules = temporaryCacheService.Value.Get(ViewTypeCacheKey(culture, "rules", filePath3), () => LoadViewType(searchFormPath, "rules")),
			Events = temporaryCacheService.Value.Get(ViewTypeCacheKey(culture, "events", filePath4), () => LoadViewType(searchFormPath, "events"))
		};
		if (!debug)
		{
			temporaryCacheService.Value.ClearTransientCache();
		}
		return result;
	}

	public void InvalidateCache(EpMetaFxCacheInvalidateRequest request)
	{
		if (string.IsNullOrEmpty(request.LanguageId) && string.IsNullOrEmpty(request.Like))
		{
			logger.Debug("No search or language provided, cache cannot be cleared.");
		}
		else if (!string.IsNullOrEmpty(request.LanguageId) && string.IsNullOrEmpty(request.Like))
		{
			string directoryPath = Path.Combine(rootPath, "shared", "search");
			string[] array = physicalFileProvider.DirectoryGetDirectories(directoryPath);
			foreach (string directoryPath2 in array)
			{
				string[] array2 = physicalFileProvider.DirectoryGetDirectories(directoryPath2);
				foreach (string path in array2)
				{
					cache.WithNewCacheLocation(Path.Combine(path, "cache"));
					cache.Clear(request.LanguageId);
				}
			}
			logger.Debug("Cache cleared for all searches for language " + request.LanguageId);
		}
		else if (string.IsNullOrEmpty(request.viewId))
		{
			string directoryPath3 = Path.Combine(rootPath, "shared", "search", request.Like);
			string[] array = physicalFileProvider.DirectoryGetDirectories(directoryPath3);
			foreach (string path2 in array)
			{
				cache.WithNewCacheLocation(Path.Combine(path2, "cache"));
				cache.Clear(request.LanguageId);
			}
			logger.Debug("Cache cleared for all search forms within " + request.Like);
		}
		else
		{
			string newCacheLocation = Path.Combine(rootPath, "shared", "search", request.Like, request.viewId, "cache");
			cache.WithNewCacheLocation(newCacheLocation);
			cache.Clear(request.LanguageId);
			logger.Debug("Cache cleared for search " + request.Like + ", form " + request.viewId);
		}
	}

	public bool GetSearchForm(EpMetaFxSearchRequest request)
	{
		logger.Debug("Find search LIKE property folder for " + request.like);
		string folderPath = Path.Combine(rootPath, "shared", "search", request.like, request.properties.SearchForm);
		return contentLoader.Exists(helper.GetFilePath(folderPath, "search"));
	}

	private string ViewTypeCacheKey(string currentCultureName, string viewType, string filePath)
	{
		return $"{currentCultureName}.{viewType}.{physicalFileProvider.FileGetLastWriteTimeUtc(filePath).Ticks}.json";
	}

	private JObject LoadSearchContent(string filePath, string viewType)
	{
		if (contentLoader.Exists(filePath))
		{
			return jsonMerger.Load(filePath, viewType, localizedStrings);
		}
		return null;
	}

	private object LoadViewType(string viewid, string viewType, [Optional] string fileName, [Optional] string mode)
	{
		string filePath = GetFilePath(viewid, viewType, fileName);
		return LoadSearchContent(filePath, viewType)?[viewType];
	}

	private string GetFilePath(string viewid, string viewType, [Optional] string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return helper.GetFilePath(viewid, viewType);
		}
		if (fileName.StartsWith(rootPath))
		{
			return fileName;
		}
		return Path.Combine(rootPath, viewid, fileName);
	}

	public IEnumerable<SearchFolder> GetSearches(string startsWith)
	{
		string path = Path.Combine(rootPath, "shared", "search");
		if (!contentLoader.DirectoryExists(path))
		{
			throw new FileNotFoundException("Processing Error : No folder found");
		}
		List<SearchFolder> list = new List<SearchFolder>();
		string[] directories = contentLoader.GetDirectories(path, startsWith);
		foreach (string path2 in directories)
		{
			string name = new DirectoryInfo(path2).Name;
			SearchFolder item = new SearchFolder
			{
				Folder = name,
				SearchSubFolders = GetFoldersCollection(path2)
			};
			list.Add(item);
		}
		return list;
	}

	private IEnumerable<string> GetFoldersCollection(string path)
	{
		string[] array = (from d in contentLoader.GetDirectories(path)
			where !d.EndsWith("cache")
			select d).ToArray();
		string[] array2 = new string[array.Length];
		for (int num = 0; num < array.Length; num++)
		{
			array2[num] = new DirectoryInfo(array[num]).Name;
		}
		return array2;
	}
}
