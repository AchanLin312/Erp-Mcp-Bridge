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

public class ComboManager
{
	private readonly HelperMethods helper;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly string rootPath;

	public ComboManager(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService = null)
	{
		this.rootPath = Path.Combine(rootPath, "shared", "combo");
		this.logger = logger;
		this.contentLoader = contentLoader;
		helper = new HelperMethods(this.logger, contentLoader);
	}

	public object Load(EpMetaFxComboRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		currentCultureName();
		return GetComboDetails(request, localizedStringsFunc);
	}

	private object GetComboDetails(EpMetaFxComboRequest request, Func<JObject, string, JObject> localizedStringsFunc)
	{
		JsonMerger jsonMerger = new JsonMerger(logger, contentLoader, null);
		if (!contentLoader.DirectoryExists(rootPath))
		{
			logger.Debug("Processing Error : No folder found");
			throw new FileNotFoundException("Processing Error : No folder found");
		}
		JObject localizedStrings = new JObject();
		logger.Debug("Processing search " + request.Folder);
		jsonMerger.LoadStrings(helper.GetFilePath(Path.Combine(rootPath, request.Folder, request.SubFolder), "strings"));
		return jsonMerger.Load(helper.GetFilePath(Path.Combine(rootPath, request.Folder, request.SubFolder), "combo"), "combo", localizedStrings)?["combo"];
	}

	public IEnumerable<ComboFolder> GetCombo()
	{
		if (!contentLoader.DirectoryExists(rootPath))
		{
			logger.Debug("Processing Error : No folder found");
			throw new FileNotFoundException("Processing Error : No folder found");
		}
		List<ComboFolder> list = Enumerable.Empty<ComboFolder>().ToList();
		string[] directories = contentLoader.GetDirectories(rootPath);
		if (directories.Length != 0)
		{
			string[] array = directories;
			foreach (string path in array)
			{
				string name = new DirectoryInfo(path).Name;
				ComboFolder item = new ComboFolder
				{
					Folder = name,
					ComboSubFolders = GetFoldersCollection(path)
				};
				list.Add(item);
			}
		}
		return list;
	}

	private IEnumerable<string> GetFoldersCollection(string path)
	{
		logger.Debug("Get Sub Folders collection");
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

	private string CacheKey(string currentCustureName)
	{
		return "response." + currentCustureName + ".json";
	}
}
