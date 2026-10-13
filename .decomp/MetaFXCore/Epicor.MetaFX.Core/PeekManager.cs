using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core;

public class PeekManager
{
	private readonly IContentLoader contentLoader;

	private readonly JsonMerger jsonMerger;

	private readonly ILogger logger;

	private readonly string rootPath;

	private ICacheService cache;

	private JObject localizedStrings = new JObject();

	public PeekManager(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		jsonMerger = new JsonMerger(this.logger, contentLoader, cacheService);
		cache = cacheService;
	}

	public ViewMetadataResponse Load(EpMetaFxPeekRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		string currentCultureName2 = currentCultureName();
		if (!contentLoader.DirectoryExists(Path.Combine(rootPath, "shared", "peek", request.like, request.properties.peekForm)))
		{
			logger.Debug("Processing Error : No folder found");
			throw new FileNotFoundException("Processing Error : No folder found");
		}
		return cache.Get(CacheKey(currentCultureName2), () => GetPeek(request, localizedStringsFunc));
	}

	public IEnumerable<PeekFolder> GetPeeks()
	{
		if (!contentLoader.DirectoryExists(Path.Combine(rootPath, "shared", "peek")))
		{
			logger.Debug("Processing Error : No folder found");
			throw new FileNotFoundException("Processing Error : No folder found");
		}
		List<PeekFolder> list = Enumerable.Empty<PeekFolder>().ToList();
		string[] directories = contentLoader.GetDirectories(Path.Combine(rootPath, "shared", "peek"));
		if (directories.Length != 0)
		{
			string[] array = directories;
			foreach (string path in array)
			{
				string name = new DirectoryInfo(path).Name;
				PeekFolder item = new PeekFolder
				{
					like = name,
					peekForms = GetFoldersCollection(path)
				};
				list.Add(item);
			}
		}
		return list;
	}

	private ViewMetadataResponse GetPeek(EpMetaFxPeekRequest request, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("Processing peek " + request.like);
		JObject jObject = new JsonMerger(logger, contentLoader, cache).LoadStrings(Path.Combine(rootPath, "shared", "peek", request.like, request.properties.peekForm, "strings.json"));
		if (jObject != null)
		{
			localizedStrings = jObject;
		}
		localizedStrings = jObject;
		ViewMetadataResponse viewMetadataResponse = new ViewMetadataResponse();
		viewMetadataResponse.Layout = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/layout.jsonc", "layout");
		viewMetadataResponse.Events = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/events.jsonc", "events");
		viewMetadataResponse.ToolBar = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/tools.jsonc", "tools");
		viewMetadataResponse.DataViews = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/dataviews.jsonc", "dataviews");
		viewMetadataResponse.References = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/references.jsonc", "references");
		viewMetadataResponse.Rules = LoadViewType(rootPath + "/shared/peek/" + request.like + "/" + request.properties.peekForm + "/rules.jsonc", "rules");
		return viewMetadataResponse;
	}

	private object LoadViewType(string filePath, string viewType)
	{
		return LoadViewContent(filePath, viewType)?[viewType];
	}

	private JObject LoadViewContent(string filePath, string viewType)
	{
		if (contentLoader.Exists(filePath))
		{
			return jsonMerger.Load(filePath, viewType, localizedStrings);
		}
		return null;
	}

	private IEnumerable<string> GetFoldersCollection(string path)
	{
		logger.Debug("Get Sub Folders collection");
		string[] directories = Directory.GetDirectories(path);
		string[] array = new string[directories.Length];
		for (int i = 0; i < directories.Length; i++)
		{
			array[i] = new DirectoryInfo(directories[i]).Name;
		}
		return array;
	}

	private string CacheKey(string currentCultureName)
	{
		return "response." + currentCultureName + ".json";
	}
}
