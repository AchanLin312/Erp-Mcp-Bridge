using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.LayerManager;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.LayerDataFixup;

public class FixCustomizationLayers
{
	private readonly string originalVersionRootFolderPath;

	private readonly IContentLoader previousVersionContentLoader;

	private readonly ILogger logger;

	private readonly ILayerService layerService;

	private readonly Dictionary<string, Dictionary<string, JObject>> viewJsonsCache;

	public FixCustomizationLayers(string originalVersionRootFolderPath, IContentLoader previousVersionContentLoader, ILayerService layerService, ILogger logger)
	{
		this.originalVersionRootFolderPath = originalVersionRootFolderPath;
		this.previousVersionContentLoader = previousVersionContentLoader;
		this.logger = logger;
		this.layerService = layerService;
		viewJsonsCache = new Dictionary<string, Dictionary<string, JObject>>();
	}

	public bool FixLayers(EpMetaFXLayerFixRequest request)
	{
		if (request.clearBackupData)
		{
			layerService.DeleteBackupTypeLayers();
		}
		else
		{
			layerService.ProcessAllLayersForDataFix(request.applications, ProcessLayer);
		}
		return false;
	}

	private bool ProcessLayer(string viewId, string layerName, Dictionary<string, JToken> layerJsonObject)
	{
		bool flag = false;
		bool flag2 = layerJsonObject.Any((KeyValuePair<string, JToken> item) => LayerManager.GetLayerVersion(item.Value) != null);
		if (flag2)
		{
			flag = false;
			LogMessage("Layer is of the new version and does not require processing.");
		}
		else
		{
			foreach (KeyValuePair<string, JToken> item in layerJsonObject.Where((KeyValuePair<string, JToken> item) => !JsonMerger.FlatFiles.Contains<string>(item.Key, StringComparer.OrdinalIgnoreCase)))
			{
				JObject baseViewMetaUI = GetBaseViewMetaUI(viewId, item.Key);
				if (baseViewMetaUI != null && !flag2)
				{
					flag = ProcessDiffs(baseViewMetaUI, item) | flag;
				}
			}
		}
		return flag;
	}

	private bool ProcessDiffs(JObject baseMetaUI, KeyValuePair<string, JToken> layerDiffs)
	{
		List<JToken> list = layerDiffs.Value["Diffs"].Where((JToken diff) => diff["Path"].ToString() == "components" && diff["Operation"].ToString() == "Replace" && diff["Value"] != null && diff["Value"].HasValues).ToList();
		int num = 0;
		foreach (JToken item in list)
		{
			JToken layerComponent = item["Value"];
			JToken jToken = (baseMetaUI["layout"]["components"] as JArray).Where((JToken c) => c["id"].ToString() == layerComponent["id"].ToString()).FirstOrDefault();
			if (jToken != null)
			{
				if (!LayerContentModified(jToken["model"] as JObject, layerComponent["model"] as JObject))
				{
					LogMessage("Removing Diff from layer. Page: " + layerDiffs.Key + ", Component: " + Environment.NewLine + layerComponent.ToString());
					item.Remove();
					num++;
				}
			}
			else
			{
				LogMessage("Base component not found. Page: " + layerDiffs.Key + ", Component: " + Environment.NewLine + layerComponent["id"].ToString());
			}
		}
		if (num > 0)
		{
			LogMessage($"Total JSON diffs removed from Page {layerDiffs.Key}: {num}");
			return true;
		}
		return false;
	}

	private bool LayerContentModified(JObject baseComponent, JObject layerComponent)
	{
		foreach (JProperty item in from p in layerComponent.Properties()
			where p.Name != "id" && p.Name != "guid"
			select p)
		{
			if (baseComponent[item.Name] != null)
			{
				if (item.Value is JValue)
				{
					if (baseComponent[item.Name].ToString() != item.Value.ToString())
					{
						return true;
					}
				}
				else if (item.Value is JObject)
				{
					if (LayerContentModified(baseComponent[item.Name] as JObject, item.Value as JObject))
					{
						return true;
					}
				}
				else if (item.Value is JArray && (item.Value.Count() > baseComponent[item.Name].Count() || baseComponent[item.Name].ToString() != item.Value.ToString()))
				{
					return true;
				}
				continue;
			}
			return true;
		}
		return false;
	}

	private JObject GetBaseViewMetaUI(string viewId, string fileName)
	{
		if (viewJsonsCache.ContainsKey(viewId) && viewJsonsCache[viewId].ContainsKey(fileName))
		{
			return viewJsonsCache[viewId][fileName];
		}
		string text = Path.Combine(originalVersionRootFolderPath, viewId);
		if (!viewJsonsCache.ContainsKey(viewId))
		{
			if (!previousVersionContentLoader.DirectoryExists(text))
			{
				LogMessage("Base application not found " + viewId + ", File: " + fileName);
				return null;
			}
			viewJsonsCache.Add(viewId, new Dictionary<string, JObject>());
		}
		Dictionary<string, JObject> dictionary = viewJsonsCache[viewId];
		if (!dictionary.ContainsKey(fileName))
		{
			string path = (fileName.Equals("layout", StringComparison.OrdinalIgnoreCase) ? Path.Combine(text, "layout.jsonc") : Path.Combine(text, "pages", fileName + ".layout.jsonc"));
			if (!previousVersionContentLoader.Exists(path))
			{
				LogMessage("Base page not found " + viewId + ", File: " + fileName);
				return null;
			}
			try
			{
				JObject value = JsonConvert.DeserializeObject<JObject>(previousVersionContentLoader.ReadAllText(path));
				LogMessage("Loaded MetaUI for " + fileName);
				dictionary.Add(fileName, value);
			}
			catch (Exception ex)
			{
				LogMessage("Failed to load MetaUI for " + fileName + ". Error: " + ex.Message);
				return null;
			}
		}
		return dictionary[fileName];
	}

	private void LogMessage(string message, bool debugOnly = false)
	{
		if (!debugOnly)
		{
			logger.Debug(message);
		}
	}
}
