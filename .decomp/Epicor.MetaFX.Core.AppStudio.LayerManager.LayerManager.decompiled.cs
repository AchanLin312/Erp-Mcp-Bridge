using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.LayerManager;

public class LayerManager
{
	private const int MAX_RECURSIVE_PARENT_ALLOWED = 10;

	private readonly HelperMethods helper;

	private readonly bool propLevelDiffsEnabled;

	private readonly JsonDiff jsondiff;

	private readonly ILayerService layerService;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private readonly ILogger logger;

	private readonly string rootPath;

	private readonly JsonSerializer serializer;

	private readonly LayoutHelper layoutHelper;

	public LayerManager(string rootPath, ILogger logger, ILayerService layerService, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.layerService = layerService;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		layoutHelper = new LayoutHelper(logger);
		helper = new HelperMethods(this.logger, contentLoader);
		propLevelDiffsEnabled = this.layerService != null && this.layerService.PropertyLevelDiffsEnabled();
		jsondiff = new JsonDiff(this.logger, isEnableFullPath: false, propLevelDiffsEnabled);
		serializer = new JsonSerializer();
		serializer.Converters.Add(new StringEnumConverter());
	}

	internal void SaveLayer(SaveLayerRequest request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		EpMetaFxLayer layerRequest = request.Layer;
		logger.Debug("Save the layer " + layerRequest.LayerName + " for the " + layerRequest.ViewId + ".");
		helper.Validate(layerRequest);
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, JToken> item in request.Layer.Content)
		{
			if (Enumerable.Contains(JsonMerger.FileWithModes, item.Key) && item.Value == null)
			{
				list.Add(item.Key);
			}
		}
		if (layerRequest.CGCCode == null || layerRequest.TypeCode == "KNTCPersLayer")
		{
			layerRequest.CGCCode = string.Empty;
		}
		if (layerRequest.IsNew)
		{
			EpMetaFxGetLayersRequest request2 = new EpMetaFxGetLayersRequest
			{
				TypeCode = layerRequest.TypeCode,
				ViewId = layerRequest.ViewId,
				IncludeUnpublishedLayers = true
			};
			List<EpMetaFxLayer> allLayers = layerService.GetAllLayers(request2);
			if (allLayers != null)
			{
				if (allLayers.Exists((EpMetaFxLayer layer) => layer.LayerName == layerRequest.LayerName && (layer.Company == layerRequest.Company || layer.Company == "" || layerRequest.Company == "")))
				{
					logger.Debug("LayerName " + layerRequest.LayerName + " already exists.");
					throw new ValidationException("LayerName " + layerRequest.LayerName + " already exists.");
				}
				if (layerRequest.LayerName.Contains(","))
				{
					logger.Debug("LayerName Comma(,) is not allowed");
					throw new ValidationException("Comma(,) is not allowed in Layer Name");
				}
				if (!string.IsNullOrEmpty(layerRequest.CGCCode) && allLayers.Exists((EpMetaFxLayer layer) => layer.CGCCode == layerRequest.CGCCode))
				{
					logger.Debug("LayerName " + layerRequest.LayerName + " with CGCCode " + layerRequest.CGCCode + " already exists.");
					throw new ValidationException("LayerName " + layerRequest.LayerName + "  with CGCCode " + layerRequest.CGCCode + " already exists.");
				}
			}
		}
		Dictionary<string, JToken> baseJsons = GetBaseJsons(layerRequest, currentCultureName, localizedStringsFunc, request.CountryGroupCode, request.CountryCode);
		logger.Debug("base json: " + JsonConvert.SerializeObject(baseJsons));
		ApplyModes(layerRequest, baseJsons, list);
		Dictionary<string, JToken> jsonDifference = GetJsonDifference(layerRequest, baseJsons, list);
		layerRequest.Content = jsonDifference;
		UpdateUnchangedFiles(layerRequest, list);
		if (!SaveLayer(layerRequest, request.PublishParentLayers))
		{
			logger.Debug("Unable to save layers for " + layerRequest.ViewId + ".");
			throw new Exception("Unable to save layers for " + layerRequest.ViewId);
		}
		logger.Debug("Saved the layer " + layerRequest.LayerName + " for the " + layerRequest.ViewId + ".");
	}

	public void DeleteLayer(EpMetaFxLayerRequest request)
	{
		helper.Validate(request);
		if (request.CGCCode == null)
		{
			request.CGCCode = string.Empty;
		}
		logger.Debug("Delete the layer " + request.LayerName + " for the " + request.ViewId + ".");
		if (!layerService.DeleteLayer(request))
		{
			logger.Debug("Unable to delete layers for " + request.ViewId + ".");
			throw new Exception("Unable to delete layers for " + request.ViewId);
		}
		logger.Debug("Deleted the layer " + request.LayerName + " for the " + request.ViewId + ".");
	}

	public EpMetaFxLayer GetLayer(EpMetaFxLayerRequest request)
	{
		logger.Debug("Get layer for " + request.ViewId + ".");
		if (request.CGCCode == null)
		{
			request.CGCCode = string.Empty;
		}
		return layerService.GetLayer(request);
	}

	public bool LayerExists(EpMetaFxLayerRequest request)
	{
		logger.Debug("Check layer for existence " + request.ViewId + ".");
		if (request.CGCCode == null)
		{
			request.CGCCode = string.Empty;
		}
		return layerService.LayerExists(request);
	}

	public List<EpMetaFxLayer> GetLayers(EpMetaFxGetLayersRequest request)
	{
		logger.Debug("Get all customized and personalized layer for " + request.ViewId + ".");
		return layerService.GetLayers(request);
	}

	public int RevertLayerDiffs(EpMetaFxLayer theLayer, string contentType, string tokenId, [Optional] string propertyName, [Optional] string pageName)
	{
		return theLayer.Content.Where((KeyValuePair<string, JToken> kvp) => contentType == kvp.Key || (contentType == "pages" && pageName == kvp.Key)).FirstOrDefault().Value.ToObject<PageInfo>().Diffs.RemoveAll((JsonDiffInfo diff) => diff.Id.Equals(tokenId, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrEmpty(propertyName) || diff.Path.EndsWith("/" + propertyName)));
	}

	public List<EpMetaFXAuditLog> GetLayerAuditLogs(EpMetaFXAuditLogRequest request)
	{
		logger.Debug("Get all audit logs for " + request.ViewId + ".");
		return layerService.GetAuditLogs(request);
	}

	internal EpMetaFxLayer GetPersonalizedLayer(CustomizationLayerRequest request)
	{
		return GetPersonalizedLayer(request, request.UserName, request.Layers);
	}

	internal Dictionary<string, JToken> ApplyCustomizedViews(Dictionary<string, JToken> baseContent, LayerRequest layerRequest, string type, string pageName)
	{
		string mode = layerRequest.Request.properties.mode;
		if (layerRequest.CustomizedLayers.Any())
		{
			foreach (EpMetaFxLayer customizedLayer in layerRequest.CustomizedLayers)
			{
				GetPatchedView(baseContent, customizedLayer, type, pageName, mode);
			}
		}
		if (layerRequest.PersonalizedLayer != null)
		{
			GetPatchedView(baseContent, layerRequest.PersonalizedLayer, type, pageName, mode);
		}
		if (layerRequest.Request.properties.mode != "AppStudio" && Enumerable.Contains(JsonMerger.FileWithModes, type) && baseContent[type] != null && baseContent[type].HasValues)
		{
			foreach (JToken item in (from x in baseContent[type].Children().OfType<JToken>()
				where x.Value<string>("mode") == "D"
				select x)?.ToList())
			{
				(baseContent[type] as JArray)?.Remove(item);
			}
		}
		return baseContent;
	}

	internal List<EpMetaFxLayer> GetCustomizedLayers(CustomizationLayerRequest request)
	{
		List<EpMetaFxLayer> list = new List<EpMetaFxLayer>();
		if (request.LayerType != "KNTCPersLayer")
		{
			if (string.IsNullOrEmpty(request.CountryGroupCode) && string.IsNullOrEmpty(request.CountryCode) && (request.Layers == null || request.Layers.Length == 0))
			{
				return list;
			}
			EpMetaFxGetLayersRequest request2 = new EpMetaFxGetLayersRequest
			{
				TypeCode = "KNTCCustLayer",
				ViewId = request.ViewId,
				DeviceType = request.DeviceType,
				IncludeUnpublishedLayers = request.IncludeUnPublishedLayers
			};
			List<EpMetaFxLayer> list2 = layerService.GetLayers(request2);
			if (list2 == null)
			{
				return list;
			}
			if (request.DeviceType == "Phone" || request.DeviceType == "Tablet")
			{
				string[] layers = request.Layers;
				foreach (string layer in layers)
				{
					EpMetaFxLayer epMetaFxLayer = list2.FirstOrDefault((EpMetaFxLayer e) => e.DeviceType == request.DeviceType && e.ParentLayers.FirstOrDefault((string l) => l == layer) != null);
					if (epMetaFxLayer != null)
					{
						request.Layers = new string[1] { epMetaFxLayer.LayerName };
					}
				}
			}
			else
			{
				list2 = list2.Where((EpMetaFxLayer e) => e.DeviceType == "Desktop").ToList();
			}
			list2 = list2.OrderByDescending((EpMetaFxLayer x) => x.Company).ToList();
			if (!request.IsDesignerMode && (!string.IsNullOrEmpty(request.CountryGroupCode) || !string.IsNullOrEmpty(request.CountryCode)))
			{
				EpMetaFxLayer cGCLayer = GetCGCLayer(request, list2);
				if (cGCLayer != null)
				{
					list.Add(cGCLayer);
				}
			}
			string[] layers2 = request.Layers;
			if (layers2 != null && layers2.Length != 0)
			{
				string[] layers = request.Layers;
				foreach (string layer2 in layers)
				{
					if (!string.IsNullOrEmpty(layer2))
					{
						EpMetaFxLayer epMetaFxLayer2 = list2.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == layer2);
						if (epMetaFxLayer2 != null)
						{
							List<EpMetaFxLayer> customizedLayers = GetCustomizedLayers(list2, epMetaFxLayer2, request.IncludeUnPublishedLayers, request.UxAppVersion);
							list.AddRange(customizedLayers);
						}
					}
				}
			}
		}
		else
		{
			string[] layers3 = request.Layers;
			if (layers3 != null && layers3.Length != 0)
			{
				EpMetaFxGetLayersRequest request3 = new EpMetaFxGetLayersRequest
				{
					ViewId = request.ViewId,
					DeviceType = request.DeviceType
				};
				List<EpMetaFxLayer> layers4 = layerService.GetLayers(request3);
				if (layers4 == null)
				{
					return list;
				}
				IEnumerable<EpMetaFxLayer> source = layers4.Where((EpMetaFxLayer ly) => ly.TypeCode == "KNTCPersLayer");
				List<EpMetaFxLayer> list3 = layers4.Where((EpMetaFxLayer ly) => ly.TypeCode == "KNTCCustLayer").ToList();
				string[] layers = request.Layers;
				foreach (string layer3 in layers)
				{
					EpMetaFxLayer epMetaFxLayer3 = source.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == layer3);
					if (epMetaFxLayer3 == null)
					{
						continue;
					}
					if (epMetaFxLayer3.ParentLayers != null && epMetaFxLayer3.ParentLayers.Any())
					{
						string[] parentLayers = epMetaFxLayer3.ParentLayers;
						foreach (string customizedLayerName in parentLayers)
						{
							if (string.IsNullOrEmpty(layer3))
							{
								continue;
							}
							EpMetaFxLayer epMetaFxLayer4 = list3.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == customizedLayerName);
							if (epMetaFxLayer4 != null)
							{
								List<EpMetaFxLayer> customizedLayers2 = GetCustomizedLayers(list3, epMetaFxLayer4, request.IncludeUnPublishedLayers, request.UxAppVersion);
								if (customizedLayers2 != null)
								{
									list.AddRange(customizedLayers2);
								}
							}
						}
					}
					EpMetaFxLayer personalizedLayer = GetPersonalizedLayer(request, epMetaFxLayer3.UserName, epMetaFxLayer3.ParentLayers);
					list.Add(personalizedLayer);
				}
			}
		}
		return list;
	}

	public string GetCacheKey(LayerRequest layerRequest)
	{
		List<LayerCache> list = new List<LayerCache>();
		for (int i = 0; i < layerRequest.CustomizedLayers.Count(); i++)
		{
			LayerCache item = new LayerCache
			{
				LayerName = layerRequest.CustomizedLayers[i].LayerName,
				CompanyName = layerRequest.CustomizedLayers[i].Company,
				CGCCode = layerRequest.CustomizedLayers[i].CGCCode,
				DeviceType = layerRequest.CustomizedLayers[i].DeviceType
			};
			list.Add(item);
		}
		if (layerRequest.PersonalizedLayer != null)
		{
			LayerCache item2 = new LayerCache
			{
				CompanyName = layerRequest.PersonalizedLayer.Company,
				LayerName = layerRequest.PersonalizedLayer.LayerName,
				CGCCode = layerRequest.PersonalizedLayer.CGCCode
			};
			list.Add(item2);
		}
		string input = JsonConvert.SerializeObject(list);
		return helper.CalculateHash(input);
	}

	public void SaveUpdatedLayers(IEnumerable<EpMetaFxLayer> layers)
	{
		if (!propLevelDiffsEnabled)
		{
			return;
		}
		List<EpMetaFxLayer> list = layers.Where((EpMetaFxLayer l) => l.LayerUpdatedToPropDiffFormat == true && !l.Content.ContainsKey("config")).Select(delegate(EpMetaFxLayer layer)
		{
			PageInfo o = BuildPageInfo("config");
			layer.Content.Add("config", JToken.FromObject(o, serializer));
			return layer;
		}).ToList();
		if (list.Any())
		{
			layerService.UpdateLayersInDatabase(list).ForEach(delegate(EpMetaFxLayer layer)
			{
				logger.Debug("The layer " + layer.LayerName + "  for the view " + layer.ViewId + " is now updated to latest format.");
			});
		}
	}

	internal static string GetLayerVersion(JToken layerConfigJson)
	{
		string result = null;
		if (layerConfigJson != null)
		{
			PageInfo pageInfo = layerConfigJson.ToObject<PageInfo>();
			result = ((pageInfo?.Name == "config") ? pageInfo.Version : null);
		}
		return result;
	}

	private List<EpMetaFxLayer> GetCustomizedLayers(List<EpMetaFxLayer> allCustomizedLayers, EpMetaFxLayer layer, bool includeDraftContent, int uxAppVersion)
	{
		EpMetaFxLayerRequest request = new EpMetaFxLayerRequest
		{
			ViewId = layer.ViewId,
			Company = layer.Company,
			TypeCode = layer.TypeCode,
			LayerName = layer.LayerName,
			DeviceType = layer.DeviceType,
			UserName = layer.UserName,
			CGCCode = layer.CGCCode,
			ParentLayers = layer.ParentLayers,
			IncludeDraftContent = includeDraftContent,
			UxAppVersion = uxAppVersion
		};
		return BuildLayerList(allCustomizedLayers, request, new List<EpMetaFxLayer>(), includeDraftContent);
	}

	private EpMetaFxLayer GetPersonalizedLayer(CustomizationLayerRequest request, string userName, string[] parentLayers)
	{
		EpMetaFxLayerRequest request2 = new EpMetaFxLayerRequest
		{
			ViewId = request.ViewId,
			Company = request.Company,
			TypeCode = "KNTCPersLayer",
			ParentLayers = parentLayers,
			DeviceType = "Desktop",
			UserName = userName,
			CGCCode = string.Empty
		};
		return layerService.GetLayer(request2);
	}

	private List<EpMetaFxLayer> BuildLayerList(List<EpMetaFxLayer> allCustomizedLayers, EpMetaFxLayerRequest request, List<EpMetaFxLayer> layerList, bool includeDraftContent)
	{
		EpMetaFxLayer layer = layerService.GetLayer(request);
		if (layer == null)
		{
			return layerList;
		}
		layerList.Insert(0, layer);
		if (layerList.Count <= 10)
		{
			if (layer.ParentLayers != null && layer.ParentLayers.Any())
			{
				string[] parentLayers = layer.ParentLayers;
				foreach (string parentLayer in parentLayers)
				{
					EpMetaFxLayer epMetaFxLayer = allCustomizedLayers.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == parentLayer);
					if (epMetaFxLayer != null)
					{
						EpMetaFxLayerRequest request2 = new EpMetaFxLayerRequest
						{
							ViewId = epMetaFxLayer.ViewId,
							Company = epMetaFxLayer.Company,
							TypeCode = epMetaFxLayer.TypeCode,
							LayerName = parentLayer,
							DeviceType = epMetaFxLayer.DeviceType,
							UserName = epMetaFxLayer.UserName,
							ParentLayers = epMetaFxLayer.ParentLayers,
							CGCCode = epMetaFxLayer.CGCCode,
							IncludeDraftContent = includeDraftContent
						};
						BuildLayerList(allCustomizedLayers, request2, layerList, includeDraftContent);
					}
				}
			}
		}
		else
		{
			logger.Debug($"Max Parent layer count {10} reached for {request.ViewId}.");
		}
		return layerList;
	}

	private void GetPatchedView(Dictionary<string, JToken> baseContent, EpMetaFxLayer diffLayer, string type, string pageName, string mode)
	{
		if (diffLayer?.Content == null)
		{
			return;
		}
		string text = null;
		diffLayer.Content.TryGetValue("config", out var value);
		text = GetLayerVersion(value);
		foreach (KeyValuePair<string, JToken> item in diffLayer.Content.Where((KeyValuePair<string, JToken> kvp) => kvp.Key != "config"))
		{
			string key = item.Key;
			if (type == "pages")
			{
				if (string.IsNullOrEmpty(pageName))
				{
					switch (key)
					{
					case "layout":
					case "events":
					case "dataviews":
					case "tools":
					case "rules":
						continue;
					}
				}
				else if (key != pageName)
				{
					continue;
				}
			}
			else if (key != type)
			{
				continue;
			}
			if (type == "pages" && baseContent.ContainsKey(key) && !OrphansManager.IsOrphanPage(baseContent[key]) && string.IsNullOrEmpty(baseContent[key]["components"]?.ToString()))
			{
				logger.Debug("Content is null for " + diffLayer.TypeCode + "-" + diffLayer.LayerName + "-" + diffLayer.ViewId + "-" + key + ".");
				continue;
			}
			PageInfo pageInfo = item.Value.ToObject<PageInfo>();
			switch (pageInfo.Operation)
			{
			case OperationType.Add:
			{
				JToken value2 = pageInfo.Diffs[0].Value;
				if (!Enumerable.Contains(JsonMerger.FlatFiles, key))
				{
					baseContent[key] = ConvertToHierarchicalStructure(value2 as JObject, mode, diffLayer);
				}
				else
				{
					baseContent[key] = value2;
				}
				SetLayerProcessedPatchedFlag(diffLayer, type, key, patchedFlag: true);
				break;
			}
			case OperationType.Remove:
				if (baseContent.ContainsKey(key))
				{
					baseContent.Remove(key);
				}
				SetLayerProcessedPatchedFlag(diffLayer, type, key, patchedFlag: true);
				break;
			case OperationType.Replace:
				if (baseContent.ContainsKey(key))
				{
					List<JsonDiffInfo> diffs = pageInfo.Diffs;
					if (diffs != null && diffs.Any())
					{
						JToken json = baseContent[key];
						if (!Enumerable.Contains(JsonMerger.FlatFiles, key))
						{
							json = ConvertToFlattenLayout(json as JObject);
						}
						if (text == "2.0")
						{
							jsondiff.ApplyDiffs(key, ref json, pageInfo.Diffs);
						}
						else
						{
							JToken baseToken = json.DeepClone();
							jsondiff.ApplyDiffs_Orig(key, ref json, pageInfo.Diffs);
							if (propLevelDiffsEnabled)
							{
								pageInfo.Diffs = RunGetDiffs(key, baseToken, json);
								diffLayer.Content[key] = JToken.FromObject(pageInfo, serializer);
								diffLayer.LayerUpdatedToPropDiffFormat = true;
							}
						}
						if (!Enumerable.Contains(JsonMerger.FlatFiles, key))
						{
							json = ConvertToHierarchicalStructure(json as JObject, mode, diffLayer);
						}
						baseContent[key] = json;
						SetLayerProcessedPatchedFlag(diffLayer, type, key, patchedFlag: true);
						break;
					}
				}
				if (text != "2.0")
				{
					diffLayer.LayerUpdatedToPropDiffFormat = true;
				}
				break;
			}
			logger.Debug($"Customized the layer {diffLayer.TypeCode}-{diffLayer.LayerName}-{pageInfo.Operation}={diffLayer.ViewId}-{key}.");
		}
	}

	private void SetLayerProcessedPatchedFlag(EpMetaFxLayer layer, string type, string key, bool patchedFlag)
	{
		if (type != "pages" || !JsonMerger.IsPageContentKey(key))
		{
			return;
		}
		if (layer.ProcessedInfo == null)
		{
			layer.ProcessedInfo = new Dictionary<string, LayerProcessedInfo>();
		}
		string key2 = LayerProcessedInfo.GetKey(type, key);
		if (layer.ProcessedInfo.TryGetValue(key2, out var value))
		{
			if (patchedFlag && !value.PatchedFlag)
			{
				value.PatchedFlag = true;
			}
		}
		else
		{
			value = new LayerProcessedInfo(patchedFlag);
			layer.ProcessedInfo.Add(key2, value);
		}
	}

	private Dictionary<string, JToken> GetBaseJsons(EpMetaFxLayer request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, string sessionCountryGroupCode, string sessionCountryCode)
	{
		string[] array = ((request.ParentLayers != null) ? (request.ParentLayers.Clone() as string[]) : new string[0]);
		if ((!string.IsNullOrEmpty(sessionCountryGroupCode) || !string.IsNullOrEmpty(sessionCountryCode)) && request.TypeCode == "KNTCPersLayer")
		{
			string cGCLayer = GetCGCLayer(request, sessionCountryGroupCode, sessionCountryCode);
			if (!string.IsNullOrEmpty(cGCLayer))
			{
				List<string> list = (array ?? Array.Empty<string>()).ToList();
				list.Insert(0, cGCLayer);
				array = list.ToArray();
			}
		}
		ViewManager viewManager = new ViewManager(rootPath, logger, layerService, cacheService, contentLoader);
		EpMetaFxRequest epMetaFxRequest = new EpMetaFxRequest
		{
			id = request.ViewId,
			properties = new EpMetaFxRequestProperty
			{
				deviceType = "Desktop",
				userName = request.UserName,
				company = request.Company,
				mode = "AppStudio"
			}
		};
		epMetaFxRequest.properties.layers = array;
		if (epMetaFxRequest.properties.layers != null && epMetaFxRequest.properties.layers.Length == 0)
		{
			epMetaFxRequest.properties.layers = new string[1] { "" };
		}
		bool flag = request.TypeCode.ToLowerInvariant() == "KNTCPersLayer".ToLowerInvariant();
		return viewManager.GetJsonsToApplyLayer(viewManager.LoadViews(epMetaFxRequest, currentCultureName, localizedStringsFunc, applyPersonalization: false, flag, !flag));
	}

	private List<JsonDiffInfo> RunGetDiffs(string key, JToken baseToken, JToken modifiedToken)
	{
		List<JsonDiffInfo> diffs = jsondiff.GetDiffs(baseToken, modifiedToken);
		if (diffs.Count != 0 && modifiedToken != null && modifiedToken is JObject)
		{
			for (int i = 0; i < diffs.Count; i++)
			{
				JObject layout = JObject.FromObject(diffs[i]);
				ResetReusableComboRef(ref layout, isPage: false, key);
				diffs[i].Value = layout["Value"];
			}
		}
		return diffs;
	}

	private Dictionary<string, JToken> GetJsonDifference(EpMetaFxLayer request, Dictionary<string, JToken> baseContent, List<string> unchangedFiles)
	{
		Dictionary<string, JToken> dictionary = new Dictionary<string, JToken>();
		if (propLevelDiffsEnabled)
		{
			PageInfo o = BuildPageInfo("config");
			dictionary.Add("config", JToken.FromObject(o, serializer));
		}
		if (request.Content != null)
		{
			foreach (KeyValuePair<string, JToken> item in request.Content)
			{
				if (unchangedFiles.Contains(item.Key))
				{
					continue;
				}
				string key = item.Key;
				JToken jToken = item.Value;
				PageInfo pageInfo = BuildPageInfo(item.Key);
				if (!Enumerable.Contains(JsonMerger.FlatFiles, key))
				{
					jToken = ConvertToFlattenLayout(jToken as JObject);
					jToken = PreprocessBeforeJsonDiff(jToken);
				}
				if (!baseContent.ContainsKey(key) && OrphansManager.AddOrphanPageToBase(jToken, baseContent, key) == -1)
				{
					continue;
				}
				if (baseContent.ContainsKey(key))
				{
					pageInfo.Operation = OperationType.Replace;
					JToken jToken2 = baseContent[key];
					if (!Enumerable.Contains(JsonMerger.FlatFiles, key))
					{
						jToken2 = ConvertToFlattenLayout(jToken2 as JObject);
						jToken2 = PreprocessBeforeJsonDiff(jToken2);
					}
					List<JsonDiffInfo> diffs = RunGetDiffs(key, jToken2, jToken);
					pageInfo.Diffs = diffs;
				}
				else
				{
					pageInfo.Operation = OperationType.Add;
					pageInfo.Diffs = new List<JsonDiffInfo>();
					pageInfo.Diffs.Add(new JsonDiffInfo
					{
						Value = jToken,
						Operation = OperationType.Add
					});
				}
				dictionary.Add(item.Key, JToken.FromObject(pageInfo, serializer));
			}
			foreach (KeyValuePair<string, JToken> item2 in baseContent)
			{
				if (!dictionary.ContainsKey(item2.Key))
				{
					if (!Enumerable.Contains(JsonMerger.FlatFiles, item2.Key))
					{
						PageInfo o2 = BuildPageInfo(item2.Key, OperationType.Remove);
						dictionary.Add(item2.Key, JToken.FromObject(o2, serializer));
					}
					else
					{
						PageInfo o3 = BuildPageInfo(item2.Key, OperationType.Replace);
						dictionary.Add(item2.Key, JToken.FromObject(o3, serializer));
					}
				}
			}
		}
		return dictionary;
	}

	private JToken PreprocessBeforeJsonDiff(JToken token)
	{
		token = RemovePropertiesToIgnoreDuringDiff(token);
		return token;
	}

	internal static JToken RemovePropertiesToIgnoreDuringDiff(JToken token)
	{
		string[] array = new string[2] { "layoutData", "model.objectCreationDate" };
		JArray jArray = OrphansManager.JComponents(token);
		if (jArray.Any())
		{
			string[] array2 = array;
			foreach (string propName in array2)
			{
				if (propName.IndexOf(".") == -1)
				{
					foreach (JObject item in jArray.Where((JToken component) => FlattenLayout.GetJPropValue(component, propName, null) != null))
					{
						item.Property(propName)?.Remove();
					}
					continue;
				}
				foreach (JToken item2 in from component in jArray
					let tkn = component.SelectToken(propName)
					where tkn != null
					select tkn)
				{
					item2.Parent.Remove();
				}
			}
			OrphansManager.RemoveOrphanContainerComponents(jArray);
		}
		return token;
	}

	private JObject ConvertToFlattenLayout(JObject hierarchicalObject)
	{
		return new FlattenLayout().ConvertToFlattenLayout(hierarchicalObject);
	}

	private JObject ConvertToHierarchicalStructure(JObject hierarchicalObject, string mode, EpMetaFxLayer diffLayer)
	{
		if (mode == "AppStudio")
		{
			OrphansManager.RemovePreviousLayerOrphanComponents(hierarchicalObject);
		}
		FlattenLayout flattenLayout = new FlattenLayout();
		JObject jObject = flattenLayout.ConvertToHierarchicalStructure(hierarchicalObject);
		if (OrphansManager.AddOrphanComponentsPanel(logger, jObject, (from ctrl in flattenLayout.OrphanControlsList()
			select ctrl.Component).ToList(), mode, hierarchicalObject, diffLayer) && flattenLayout.OrphanControlsList().Count > 0)
		{
			flattenLayout = new FlattenLayout();
			jObject = flattenLayout.ConvertToHierarchicalStructure(hierarchicalObject);
		}
		return jObject;
	}

	private void ApplyModes(EpMetaFxLayer request, Dictionary<string, JToken> baseJsons, List<string> unchangedFiles)
	{
		Dictionary<string, JToken> dictionary = new Dictionary<string, JToken>();
		foreach (KeyValuePair<string, JToken> baseJson in baseJsons)
		{
			dictionary.Add(baseJson.Key, JsonConvert.DeserializeObject(baseJson.Value.ToString()) as JToken);
		}
		JsonDiff jsonDiff = new JsonDiff(logger);
		foreach (KeyValuePair<string, JToken> item in dictionary)
		{
			if (Enumerable.Contains(JsonMerger.FileWithModes, item.Key) && !unchangedFiles.Contains(item.Key))
			{
				JToken value = item.Value;
				if (request.Content.ContainsKey(item.Key))
				{
					request.Content[item.Key] = jsonDiff.ApplyModes(item.Key, value, request.Content[item.Key]);
				}
			}
		}
	}

	private void DeletePage(EpMetaFxLayerRequest request, EpMetaFxLayer layer)
	{
		logger.Debug("Delete the page " + request.LayerName + "-" + request.PageName + " for view " + request.ViewId + ".");
		PageInfo pageInfo = layer.Content[request.PageName].ToObject<PageInfo>();
		if (pageInfo.Diffs != null)
		{
			pageInfo.Diffs.Clear();
			FileCacheService fileCacheService = new FileCacheService(Path.Combine(rootPath, request.ViewId, "cache"), logger, contentLoader);
			new ViewManager(rootPath, logger, layerService, fileCacheService, contentLoader).InvalidateCache(new EpMetaFxCacheInvalidateRequest
			{
				viewId = request.ViewId
			});
		}
		layer.Content[request.PageName] = JToken.FromObject(pageInfo);
		if (!layerService.SaveLayer(layer, publishParentLayer: false))
		{
			logger.Debug("Unable to delete page for " + request.ViewId + "-" + request.LayerName + "-" + request.PageName + ".");
			throw new Exception("Unable to delete page.");
		}
		logger.Debug("Delete completed for layer " + request.LayerName + "-" + request.PageName + " for view " + request.ViewId + ".");
	}

	private string GetCGCLayer(EpMetaFxLayer request, string countryGroupCode, string countryCode)
	{
		EpMetaFxGetLayersRequest request2 = new EpMetaFxGetLayersRequest
		{
			TypeCode = "KNTCCustLayer",
			ViewId = request.ViewId,
			DeviceType = request.DeviceType
		};
		List<EpMetaFxLayer> layers = layerService.GetLayers(request2);
		if (layers == null)
		{
			return null;
		}
		return GetCGCLayerInternal(layers, countryCode, countryGroupCode)?.LayerName;
	}

	private EpMetaFxLayer GetCGCLayer(CustomizationLayerRequest request, List<EpMetaFxLayer> layers)
	{
		EpMetaFxLayer cGCLayerInternal = GetCGCLayerInternal(layers, request?.CountryCode, request?.CountryGroupCode);
		if (cGCLayerInternal != null && request != null)
		{
			EpMetaFxLayerRequest request2 = new EpMetaFxLayerRequest
			{
				ViewId = request.ViewId,
				Company = cGCLayerInternal.Company,
				TypeCode = cGCLayerInternal.TypeCode,
				ParentLayers = cGCLayerInternal.ParentLayers,
				DeviceType = cGCLayerInternal.DeviceType,
				UserName = cGCLayerInternal.UserName,
				CGCCode = cGCLayerInternal.CGCCode,
				LayerName = cGCLayerInternal.LayerName
			};
			return layerService.GetLayer(request2);
		}
		return null;
	}

	private EpMetaFxLayer GetCGCLayerInternal(List<EpMetaFxLayer> allCustomizedLayers, string countryCode, string countryGroupCode)
	{
		EpMetaFxLayer epMetaFxLayer = null;
		if (!string.IsNullOrEmpty(countryCode))
		{
			epMetaFxLayer = allCustomizedLayers.FirstOrDefault((EpMetaFxLayer ly) => ly.CGCCode == countryCode);
		}
		if (!string.IsNullOrEmpty(countryGroupCode))
		{
			epMetaFxLayer = epMetaFxLayer ?? allCustomizedLayers.FirstOrDefault((EpMetaFxLayer ly) => ly.CGCCode == countryGroupCode);
		}
		return epMetaFxLayer;
	}

	private void UpdateUnchangedFiles(EpMetaFxLayer layerRequest, List<string> unchangedFiles)
	{
		if (!unchangedFiles.Any())
		{
			return;
		}
		EpMetaFxLayerRequest request = new EpMetaFxLayerRequest
		{
			ViewId = layerRequest.ViewId,
			Company = layerRequest.Company,
			TypeCode = layerRequest.TypeCode,
			LayerName = layerRequest.LayerName,
			ParentLayers = layerRequest.ParentLayers,
			DeviceType = layerRequest.DeviceType,
			UserName = layerRequest.UserName,
			CGCCode = layerRequest.CGCCode,
			IncludeDraftContent = true
		};
		EpMetaFxLayer layer = layerService.GetLayer(request);
		if (layer == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item in layer.Content)
		{
			if (unchangedFiles.Contains(item.Key))
			{
				layerRequest.Content[item.Key] = layer.Content[item.Key];
			}
		}
	}

	internal static PageInfo BuildPageInfo(string name, [Optional] OperationType? operation)
	{
		return new PageInfo
		{
			Name = name,
			Version = ((name == "config") ? GetDiffVersion() : GetVersion(name)),
			Operation = operation
		};
	}

	private static string GetDiffVersion()
	{
		return "2.0";
	}

	private static string GetVersion(string jsonType)
	{
		return jsonType switch
		{
			"layout" => "3.0", 
			"rules" => "3.0", 
			"events" => "5.0", 
			"tools" => "2.0", 
			"dataviews" => "4.0", 
			_ => "3.0", 
		};
	}

	private bool SaveLayer(EpMetaFxLayer request, bool publishParentLayers)
	{
		if (publishParentLayers)
		{
			string[] parentLayers = request.ParentLayers;
			if (parentLayers != null && parentLayers.Length != 0 && !request.WIP && request.TypeCode == "KNTCCustLayer")
			{
				PublishParentLayers(request);
			}
		}
		return layerService.SaveLayer(request, publishParentLayer: false);
	}

	protected void ResetReusableComboRef(ref JObject layout, bool isPage, string key)
	{
		UpdateReusableComboRef(layout, isPage, key);
	}

	private void UpdateReusableComboRef(JObject model, bool isPage, string key)
	{
		if (model == null)
		{
			return;
		}
		foreach (JProperty item in model.Properties())
		{
			if (item.Name == "subType" && !string.IsNullOrEmpty(item.Value.ToString()))
			{
				RemoveExpandedReusableComboProps(model, isPage, key);
				break;
			}
			if (item.Value is JObject)
			{
				UpdateReusableComboRef(item.Value as JObject, isPage, key);
			}
			if (!(item.Value is JArray))
			{
				continue;
			}
			foreach (JToken item2 in item.Value as JArray)
			{
				UpdateReusableComboRef(item2 as JObject, isPage, key);
			}
		}
	}

	private void RemoveExpandedReusableComboProps(JObject model, bool isPage, string key)
	{
		string text = "../shared/combo";
		List<string> requiredProperties = new List<string> { "epBindingTextColumn", "filtersAppend", "filtersParams", "hiddenColumnsAppend", "$ref" };
		List<string> reusableComboProperties = new List<string>
		{
			"comboId", "svc", "svcPath", "tableName", "dropDownStyle", "valueField", "textField", "viewName", "sort", "columns",
			"filters", "returnObj", "required", "isEpiReadOnly", "appendList", "isDelimited", "parameterName", "retrieveColumns", "hiddenColumns", "type",
			"subType"
		};
		if (!string.IsNullOrEmpty(model["subType"]?.ToString()) && !string.IsNullOrEmpty(model["type"]?.ToString()))
		{
			if (key != "layout")
			{
				text = "../../shared/combo";
			}
			string text2 = text + "/" + model["type"].ToString() + "/" + model["subType"].ToString() + "/combo.jsonc#/combo";
			JToken value = (model["$ref"] = (isPage ? ("../" + text2) : text2));
			model["appDesignerComboType"] = value;
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

	private void PublishParentLayers(EpMetaFxLayer request)
	{
		EpMetaFxGetLayersRequest request2 = new EpMetaFxGetLayersRequest
		{
			TypeCode = request.TypeCode,
			ViewId = request.ViewId,
			IncludeUnpublishedLayers = true
		};
		List<EpMetaFxLayer> layers = layerService.GetLayers(request2);
		if (layers == null || layers.Count <= 0)
		{
			return;
		}
		string[] parentLayers = request.ParentLayers;
		foreach (string parent in parentLayers)
		{
			EpMetaFxLayer epMetaFxLayer = layers.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == parent);
			if (epMetaFxLayer != null)
			{
				epMetaFxLayer.WIP = false;
				PublishParentLayers(layers, epMetaFxLayer, request);
			}
		}
	}

	private void PublishParentLayers(List<EpMetaFxLayer> allCustomizedLayers, EpMetaFxLayer layer, EpMetaFxLayer request)
	{
		if (layer.HasDraftContent)
		{
			if (!layerService.SaveLayer(layer, publishParentLayer: true))
			{
				logger.Debug("Unable to publish parent layer " + layer.LayerName + " for the current layer " + request.LayerName + " for " + request.ViewId + ".");
				throw new Exception("Unable to publish parent layer " + layer.LayerName + " for the current layer " + request.LayerName + " for " + request.ViewId + ".");
			}
			logger.Debug("Published the parent layer " + layer.LayerName + " for the current layer " + request.LayerName + " for " + request.ViewId + ".");
		}
		if (layer.ParentLayers == null || !layer.ParentLayers.Any())
		{
			return;
		}
		string[] parentLayers = layer.ParentLayers;
		foreach (string parentLayer in parentLayers)
		{
			EpMetaFxLayer epMetaFxLayer = allCustomizedLayers.FirstOrDefault((EpMetaFxLayer ly) => ly.LayerName == parentLayer);
			if (epMetaFxLayer != null)
			{
				epMetaFxLayer.WIP = false;
				PublishParentLayers(allCustomizedLayers, epMetaFxLayer, request);
			}
		}
	}
}
