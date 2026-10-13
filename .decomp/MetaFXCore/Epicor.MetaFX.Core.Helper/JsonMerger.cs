using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.LayerManager;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Layers;
using Epicor.MetaFX.Core.SchemaUpgrade;
using JsonDiffPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tavis;

namespace Epicor.MetaFX.Core.Helper;

public class JsonMerger : IJsonMerger
{
	public const string CURRENT_SCHEMA_URL = "https://schemas.developer.epicor.com/schemas";

	public const string CURRENT_SCHEMA_LEVEL_DEFAULT = "v1";

	public const string CURRENT_CONTENT_LEVEL_EVENT = "5.0";

	public const string CURRENT_CONTENT_LEVEL_TOOL = "2.0";

	public const string CURRENT_CONTENT_LEVEL_DATAVIEW = "4.0";

	public const string CURRENT_CONTENT_LEVEL_LAYOUT = "3.0";

	public const string CURRENT_CONTENT_LEVEL_RULE = "3.0";

	public const string CURRENT_CONTENT_LEVEL_SEARCH = "4.0";

	public const string CURRENT_CONTENT_LEVEL_STRINGS = "3.0";

	public const string CURRENT_DIFF_VERSION = "2.0";

	private const string PATCH = "patch";

	public const string REFERENCES_TYPE = "references";

	public const string STRINGS_TYPE = "strings";

	public const string LAYOUT_TYPE = "layout";

	public const string EVENTS_TYPE = "events";

	public const string DATAVIEW_TYPE = "dataviews";

	public const string LAYER_CONFIG = "config";

	public const string TOOLS_TYPE = "tools";

	public const string RULES_TYPE = "rules";

	public const string SEARCH_TYPE = "search";

	public const string COMBO_TYPE = "combo";

	public const string PEEK_TYPE = "peek";

	public const string PAGES_TYPE = "pages";

	public const string PATCH_TYPE = "patch";

	public const string PROPERTIES_TYPE = "properties";

	public const string COMPONENTS = "components";

	public const string APP_DESIGNER_UDC_TYPE = "appDesignerUdcType";

	public const string CONTROLID = "controlId";

	public const string REFERENCED_CONTROL_ID = "referencedControlId";

	public const string MODEL = "model";

	public const string PARENTID = "parentId";

	public const string DATA = "data";

	public const string KEY = "key";

	public const string VALUE = "value";

	public const string ID = "id";

	public const string COMBO = "combo";

	public const string USER_DEFINED_COMPONENT = "Udc";

	public const int MAX_SCHEMA_UPGRADE_COUNTER = 50;

	public const string REDIRECT_TOOL_IDENTIFIER = "ToolRedirectTo";

	public static readonly string[] FlatFiles = new string[4] { "rules", "tools", "events", "dataviews" };

	public static readonly string[] FileWithModes = new string[4] { "rules", "events", "tools", "dataviews" };

	private readonly HelperMethods helper;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	private int currentDepth;

	private JArray patchTokens;

	private string type;

	private JArray PatchesJArray { get; set; } = new JArray();

	public bool Debug { get; set; }

	public JToken RawJson { get; private set; }

	public List<JObject> RawTokens { get; private set; }

	public JsonMerger(ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.logger = logger;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
		helper = new HelperMethods(logger, contentLoader);
	}

	public JObject Load(string metaFilePath, string _type, JObject localizedStrings = null, bool shouldConvertToHierarchicalStructure = true, LayerRequest layerRequest = null, string pageName = null, bool isUntranslated = true, int uxAppVersion = 0, string basis = null)
	{
		string jsonraw = readJsonFile(metaFilePath, uxAppVersion);
		JObject json = parseJson(jsonraw);
		PatchesJArray = new JArray();
		type = _type;
		bool flag = layerRequest?.ApplyPatches ?? true;
		VersionCheckAndUpgrade(ref json);
		if (type == "layout" || type == "search")
		{
			expandNonImportJsonPatchesRootLevel(ref json, metaFilePath);
		}
		expandNonImportJsonPointers(ref json, metaFilePath, flag);
		if (helper.HasImports(ref json))
		{
			processImports(ref json, metaFilePath, flag);
		}
		JToken content = ((!(type == "layout")) ? json[type] : json[type]?["components"]);
		if ((type == "layout") & shouldConvertToHierarchicalStructure & isUntranslated)
		{
			if (Debug)
			{
				helper.CheckDuplicateId(content, metaFilePath);
			}
			FlattenLayout flattenLayout = new FlattenLayout();
			json["layout"] = flattenLayout.ConvertToHierarchicalStructure(json["layout"] as JObject);
			if (Debug)
			{
				FlattenLayout.ComponentToken componentToken = flattenLayout.FirstOrphanControl();
				if (componentToken != null)
				{
					throw new Exception(string.Format("{0} control has orphan parent ID", componentToken.Component.GetValue("id")));
				}
			}
		}
		RawJson = json[type];
		if (((type != "strings") & isUntranslated) && layerRequest != null)
		{
			if (layerRequest.PersonalizedLayer == null)
			{
				List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
				if (customizedLayers == null || !customizedLayers.Any())
				{
					goto IL_0436;
				}
			}
			if (layerRequest.CustomizedLayers.Any())
			{
				foreach (EpMetaFxLayer customizedLayer in layerRequest.CustomizedLayers)
				{
					(bool, List<PageInfo>) tuple = helper.CheckAndUpgradeLayer(customizedLayer.Content, customizedLayer.LayerName);
					if (!tuple.Item1)
					{
						continue;
					}
					layerRequest.LayerService.UpgradeLayer(customizedLayer, tuple.Item2);
					foreach (PageInfo item in tuple.Item2)
					{
						customizedLayer.Content[item.Name] = JObject.Parse(JsonConvert.SerializeObject(item));
					}
				}
			}
			if (layerRequest.PersonalizedLayer != null)
			{
				(bool, List<PageInfo>) tuple2 = helper.CheckAndUpgradeLayer(layerRequest.PersonalizedLayer.Content, layerRequest.PersonalizedLayer.LayerName);
				if (tuple2.Item1)
				{
					layerRequest.LayerService.UpgradeLayer(layerRequest.PersonalizedLayer, tuple2.Item2);
					foreach (PageInfo item2 in tuple2.Item2)
					{
						layerRequest.PersonalizedLayer.Content[item2.Name] = JObject.Parse(JsonConvert.SerializeObject(item2));
					}
				}
			}
			if (string.IsNullOrEmpty(pageName))
			{
				RawJson = ApplyCustomizedPatch(type, json[type], layerRequest);
			}
			else
			{
				RawJson = ApplyCustomizedPatchForPage(json[type], layerRequest, pageName);
				if (RawJson == null)
				{
					return null;
				}
			}
			if (((type == "layout" || type == "pages") && RawJson != null && (RawJson as JObject).Properties().Descendants().OfType<JObject>()
				.Any((JObject a) => a["$ref"] != null)) & shouldConvertToHierarchicalStructure)
			{
				FlattenLayout flattenLayout2 = new FlattenLayout();
				JObject json2 = flattenLayout2.ConvertToFlattenLayout(RawJson as JObject);
				expandNonImportJsonPointers(ref json2, metaFilePath, flag);
				RawJson = flattenLayout2.ConvertToHierarchicalStructure(json2);
			}
			json[type] = RawJson;
		}
		goto IL_0436;
		IL_0436:
		if ((type == "events") & flag)
		{
			ProcessEventPatches(ref json);
		}
		if (flag)
		{
			processPatches(ref json);
		}
		if (Debug && type != "layout")
		{
			helper.CheckDuplicateId(content, metaFilePath);
		}
		ReplaceStrings(ref json, localizedStrings);
		return json;
	}

	public void LoadPatchTokens(string metaFilePath, string rootPath, int uxAppVersion = 0)
	{
		if (!contentLoader.Exists(metaFilePath))
		{
			contentLoader.WriteAllText(metaFilePath, contentLoader.ReadAllText(Path.Combine(rootPath, "shared", "templates", "Apps", "patch.jsonc")));
		}
		string jsonraw = readJsonFile(metaFilePath, uxAppVersion);
		JObject json = parseJson(jsonraw);
		type = "patch";
		if (helper.HasImports(ref json))
		{
			processImports(ref json, metaFilePath, isProcessPatches: false);
		}
		patchTokens = json[type] as JArray;
	}

	public JObject LoadStrings(string metaFilePath)
	{
		if (!contentLoader.Exists(metaFilePath))
		{
			return null;
		}
		JObject jObject = Load(metaFilePath, "strings");
		if (jObject != null)
		{
			IEnumerator<KeyValuePair<string, JToken>> items = jObject.GetEnumerator();
			try
			{
				JObject jObject2 = new JObject();
				List<string> source = new List<string> { "$schema", "contentVersion", "imports", "patch" };
				while (items.MoveNext())
				{
					if (!source.Any((string x) => x.Equals(items.Current.Key, StringComparison.InvariantCultureIgnoreCase)))
					{
						jObject2.Add(items.Current.Key, items.Current.Value);
					}
				}
				return jObject2;
			}
			finally
			{
				if (items != null)
				{
					items.Dispose();
				}
			}
		}
		return null;
	}

	public static bool IsPageContentKey(string key)
	{
		switch (key)
		{
		default:
			return !(key == "rules");
		case "layout":
		case "events":
		case "dataviews":
		case "tools":
			return false;
		}
	}

	private JToken ApplyCustomizedPatch(string viewType, JToken baseContent, LayerRequest layerRequest)
	{
		Dictionary<string, JToken> dictionary = new LayerManager(layerRequest.RootPath, logger, layerRequest.LayerService, contentLoader, cacheService).ApplyCustomizedViews(new Dictionary<string, JToken> { { viewType, baseContent } }, layerRequest, viewType, string.Empty);
		if (dictionary == null || !dictionary.Any())
		{
			return null;
		}
		baseContent = ((!(viewType != "layout")) ? ((JContainer)((dictionary.First().Value != null) ? JObject.Parse(dictionary.First().Value.ToString(), new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		}) : null)) : ((JContainer)((dictionary.First().Value != null) ? JArray.Parse(dictionary.First().Value.ToString(), new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		}) : null)));
		return baseContent;
	}

	private JToken ApplyCustomizedPatchForPage(JToken baseContent, LayerRequest layerRequest, string pageName)
	{
		Dictionary<string, JToken> dictionary = new Dictionary<string, JToken>();
		dictionary.Add(pageName, baseContent);
		Dictionary<string, JToken> dictionary2 = new LayerManager(layerRequest.RootPath, logger, layerRequest.LayerService, contentLoader, cacheService).ApplyCustomizedViews(dictionary, layerRequest, "pages", pageName);
		if (dictionary2 == null || !dictionary2.ContainsKey(pageName))
		{
			return null;
		}
		return dictionary2[pageName];
	}

	private string readJsonFile(string file, int uxAppVersion = 0)
	{
		if (!contentLoader.Exists(file))
		{
			logger.Debug("Processing Error : No file found " + file);
			throw new FileNotFoundException("Processing Error : No file found '" + Path.GetFileName(file) + "'");
		}
		return contentLoader.ReadAllText(file, uxAppVersion);
	}

	private JObject parseJson(string jsonraw)
	{
		return JObject.Parse(jsonraw, JsonSettings());
	}

	private JsonLoadSettings JsonSettings()
	{
		return new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		};
	}

	private bool checkSchemaVersion(ref JObject json, string _type)
	{
		switch (_type)
		{
		case "events":
			return getContentVersion(ref json, _type) == "5.0";
		case "tools":
			return getContentVersion(ref json, _type) == "2.0";
		case "dataviews":
			return getContentVersion(ref json, _type) == "4.0";
		case "layout":
			return getContentVersion(ref json, _type) == "3.0";
		case "rules":
			return getContentVersion(ref json, _type) == "3.0";
		case "search":
			return getContentVersion(ref json, _type) == "4.0";
		case "strings":
			return getContentVersion(ref json, _type) == "3.0";
		case "pages":
		case "references":
			return true;
		default:
			return getContentVersion(ref json, _type) == "v1";
		}
	}

	private string getContentVersion(ref JObject json, string _type)
	{
		JProperty jProperty = json.Properties().FirstOrDefault((JProperty j) => j.Name == "$schema");
		if (jProperty == null)
		{
			return "";
		}
		string text = jProperty.ToString().ToLower().Replace("/" + _type + ".json", "");
		int num = text.LastIndexOf('/') + 1;
		text = text.Substring(num, text.Length - num - 1);
		string text2 = ((json["contentVersion"] != null) ? json["contentVersion"].ToString() : "");
		logger.Debug("Schema Version=" + text);
		logger.Debug("Content Version=" + text2);
		return text2;
	}

	private bool hasPatches(ref JObject json)
	{
		JToken jToken = json["patch"];
		if (jToken == null || !jToken.HasValues)
		{
			if (patchTokens != null)
			{
				return patchTokens.Count > 0;
			}
			return false;
		}
		return true;
	}

	private void expandNonImportJsonPointers(ref JObject json, string parentPath, bool applyPatch)
	{
		List<JObject> list = (from j in (from p in json.Properties()
				where p.Name != "imports"
				select p).Descendants().OfType<JObject>()
			where j["$ref"] != null
			select j).ToList();
		if (list.Count > 0)
		{
			logger.Debug("expanding NonImport based Json Pointers " + parentPath);
		}
		foreach (JObject item in list)
		{
			string jsonraw = helper.resolveJsonPointer(item, parentPath, out var pointerDocumentPath);
			JObject json2 = parseJson(jsonraw);
			VersionCheckAndUpgrade(ref json2);
			if (helper.HasImports(ref json2))
			{
				logger.Debug("This NonImportJson Reference Pointer document contains Imports to Process");
				processImports(ref json2, pointerDocumentPath, applyPatch);
			}
			expandNonImportJsonPointers(ref json2, pointerDocumentPath, applyPatch);
			if (hasPatches(ref json2) & applyPatch)
			{
				logger.Debug("This NonImportJson Reference Pointer document contains Patches to Process");
				processPatches(ref json2);
			}
			string text = item["$ref"].ToString();
			int num = text.IndexOf("#", StringComparison.Ordinal);
			if (num > 0)
			{
				int num2 = num + 1;
				if (text.Length > num2 + 1 && text[num2] == '/')
				{
					JsonPointer jsonPointer = new JsonPointer(text.Substring(num2));
					JToken jToken;
					try
					{
						JToken valueFromPointer = jsonPointer.Find(json2);
						if (item["$ref"].ToString().EndsWith("#/layout/components"))
						{
							jToken = json2;
						}
						else
						{
							addReusableComoboProperties(item, text);
							jToken = TokenToReplace(item, valueFromPointer);
						}
					}
					catch (Exception ex)
					{
						throw new Exception($"Failed to dereference token in pointer {jsonPointer} with error {ex.Message}", ex);
					}
					if (jToken == null)
					{
						continue;
					}
					string referenceType = GetReferenceType(item["$ref"].ToString());
					if (!(referenceType == "combo"))
					{
						if (referenceType == "Udc")
						{
							ReplaceUdcValue(item, jToken);
						}
						else
						{
							item.Replace(jToken);
						}
					}
					else
					{
						ReplaceComboValue(item, jToken);
					}
				}
				else
				{
					item.Replace(json2);
				}
			}
			else
			{
				item.Replace(json2);
			}
		}
	}

	private void addReusableComoboProperties(JObject pointer, string value)
	{
		string[] array = value.Replace("../", "").Split(new char[1] { '/' });
		if (array.Length == 6)
		{
			string text = array[2];
			string text2 = array[3];
			if (pointer["type"] == null)
			{
				pointer.Add("type", text);
			}
			else
			{
				pointer["type"] = text;
			}
			if (pointer["subType"] == null)
			{
				pointer.Add("subType", text2);
			}
			else
			{
				pointer["subType"] = text2;
			}
		}
	}

	private void ReplaceComboValue(JObject pointer, JToken valueToReplace)
	{
		pointer.Merge(valueToReplace, new JsonMergeSettings
		{
			MergeArrayHandling = MergeArrayHandling.Union
		});
		(from x in pointer.Children()
			select (JProperty)x).FirstOrDefault((JProperty x) => x.Name == "$ref")?.Remove();
	}

	private void ReplaceUdcValue(JObject pointer, JToken valueToReplace)
	{
		JToken jToken = pointer["properties"];
		IEnumerable<JToken> enumerable = valueToReplace["layout"]["components"].Where((JToken cc) => (string?)cc["sourceTypeId"] == "metafx-panel-card");
		List<JToken> nonComplexComponents = valueToReplace["layout"]["components"].ToList().FindAll((JToken cc) => (string?)cc["sourceTypeId"] != "metafx-panel-card");
		CreateReusableComponentLayoutStructure(isComplex: false, jToken as JArray, null, nonComplexComponents);
		foreach (JObject item in enumerable)
		{
			List<JObject> list = (from j in item.Properties().Descendants().OfType<JObject>()
				where j.ContainsKey("sourceTypeId")
				select j).ToList();
			list.Add(item);
			CreateReusableComponentLayoutStructure(isComplex: true, jToken as JArray, list, nonComplexComponents);
		}
		pointer.Property("properties").Remove();
		if (!pointer.ContainsKey("appDesignerUdcType"))
		{
			pointer.Add("appDesignerUdcType", pointer["$ref"]);
		}
		pointer.Property("$ref").Remove();
		JObject jObject2 = new FlattenLayout().ConvertToHierarchicalStructure(valueToReplace["layout"] as JObject);
		if (jObject2["components"] is JArray jArray)
		{
			foreach (JToken item2 in jArray)
			{
				if ((string?)item2["parentId"] == null)
				{
					item2["parentId"] = pointer["id"].ToString();
				}
			}
		}
		pointer.Add("components", jObject2["components"]);
	}

	private void CreateReusableComponentLayoutStructure(bool isComplex, JArray properties, List<JObject> complexComponents, List<JToken> nonComplexComponents)
	{
		foreach (JToken property in properties)
		{
			JToken controlId = property["controlId"];
			JObject component = (isComplex ? complexComponents.ToList().Find((JObject comp) => comp["id"].ToString() == controlId.ToString()) : (nonComplexComponents.ToList().Find((JToken comp) => comp["id"].ToString() == controlId.ToString()) as JObject));
			if (component == null || !(component["model"] is JObject jObject))
			{
				continue;
			}
			jObject.Add("referencedControlId", controlId);
			foreach (JToken item in (IEnumerable<JToken>)property["data"])
			{
				string text = item["key"].ToString();
				if (!jObject.ContainsKey(text))
				{
					jObject.Add(text, item["value"]);
				}
				else
				{
					jObject[text] = item["value"];
				}
				if (!(text == "id"))
				{
					continue;
				}
				dynamic childComponents;
				if (isComplex)
				{
					childComponents = complexComponents.ToList().FindAll((JObject comp) => (string?)comp["parentId"] == component["id"].ToString());
					nonComplexComponents.ToList().ForEach(delegate(JToken comp)
					{
						if (Convert.ToString(comp["parentId"]).Contains(component["id"].ToString()))
						{
							childComponents.Add(comp as JObject);
						}
					});
				}
				else
				{
					childComponents = nonComplexComponents.ToList().FindAll((JToken comp) => (string?)comp["parentId"] == component["id"].ToString());
				}
				foreach (dynamic item2 in childComponents)
				{
					item2["parentId"] = Convert.ToString(item2["parentId"]).Replace(component["id"].ToString(), item["value"].ToString());
				}
				component["id"] = item["value"];
			}
		}
	}

	private void expandNonImportJsonPatchesRootLevel(ref JObject json, string parentPath)
	{
		List<JObject> list = (from j in (from p in json.Properties()
				where p.Name != "imports"
				select p).Descendants().OfType<JObject>()
			where j["$ref"] != null
			select j).ToList();
		if (list.Count > 0)
		{
			logger.Debug("expanding NonImport based Json Pointers " + parentPath);
		}
		foreach (JObject item in list)
		{
			string jsonraw = helper.resolveJsonPointer(item, parentPath, out var _);
			JObject json2 = parseJson(jsonraw);
			VersionCheckAndUpgrade(ref json2);
			string text = item["$ref"].ToString();
			int num = text.IndexOf("#", StringComparison.Ordinal);
			if (num <= 0)
			{
				continue;
			}
			int num2 = num + 1;
			if (text.Length <= num2 + 1 || text[num2] != '/')
			{
				continue;
			}
			JsonPointer jsonPointer = new JsonPointer(text.Substring(num2));
			JToken jToken = null;
			if (jsonPointer.ToString() == "/patch")
			{
				try
				{
					processImports(ref json2, Path.Combine(Path.GetDirectoryName(parentPath), "patch.jsonc"), isProcessPatches: false);
					JToken valueFromPointer = jsonPointer.Find(json2);
					jToken = TokenToReplace(item, valueFromPointer);
				}
				catch (Exception ex)
				{
					logger.Debug($"Failed to dereference token in pointer {jsonPointer} with error {ex.Message}");
				}
				if (jToken != null)
				{
					item.Replace(jToken);
				}
			}
		}
	}

	private JToken TokenToReplace(JObject pointer, JToken valueFromPointer)
	{
		foreach (JProperty item in pointer.Children())
		{
			if (!(item.Name != "$ref"))
			{
				continue;
			}
			foreach (JProperty item2 in valueFromPointer.Children())
			{
				if (item2.Name == item.Name)
				{
					item2.Value = item.Value;
				}
			}
		}
		return valueFromPointer;
	}

	private void processImports(ref JObject json, string filepath, bool isProcessPatches = true)
	{
		logger.Debug($"Processing {filepath} level {currentDepth}");
		currentDepth++;
		expandImportJsonPointers(ref json, filepath);
		expandNonImportJsonPointers(ref json, filepath, isProcessPatches);
		if (isProcessPatches)
		{
			processPatches(ref json);
		}
	}

	private void expandImportJsonPointers(ref JObject json, string parentPath)
	{
		List<JObject> list = (from j in (from p in json.Properties()
				where p.Name == "imports"
				select p).Descendants().OfType<JObject>()
			where j["$ref"] != null
			select j).ToList();
		if (list.Count > 0)
		{
			logger.Debug("expanding Import based Json Pointers " + parentPath);
		}
		for (int num = list.Count - 1; num > -1; num--)
		{
			JObject jObject = list[num];
			string name = ((JProperty)jObject.Parent).Name;
			JObject obj = jObject;
			string jsonraw = helper.resolveJsonPointer(obj, parentPath, out var pointerDocumentPath);
			JObject json2 = parseJson(jsonraw);
			VersionCheckAndUpgrade(ref json2);
			if (helper.HasImports(ref json2))
			{
				processImports(ref json2, pointerDocumentPath, isProcessPatches: false);
			}
			if (!parentPath.ToLower().EndsWith("patch.jsonc"))
			{
				processPatches(ref json2);
			}
			if (json[name] == null)
			{
				json.Add(new JProperty(name, ""));
				json.Merge(json2, new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Union,
					MergeNullValueHandling = MergeNullValueHandling.Merge
				});
			}
			else if (name == "search")
			{
				JObject baseJson = (JObject)json2[name];
				JObject json3 = (JObject)json[name];
				mergeSearch(ref json3, ref baseJson);
				json.Merge(json2, new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Union,
					MergeNullValueHandling = MergeNullValueHandling.Merge
				});
			}
			else
			{
				IEnumerable<JToken> source = from t in json2[name].Concat(json[name])
					group t by t["id"] into g
					select g.Last();
				object[] content;
				if (json2["patch"] != null && json2["patch"].Any() && json["patch"] != null)
				{
					IEnumerable<JToken> source2 = from t in json2["patch"].Concat(json["patch"])
						group t by t["token"] into g
						select g.Last();
					JObject obj2 = json;
					content = source2.ToArray();
					obj2["patch"] = new JArray(content);
					PatchesJArray.Add(source2.ToArray());
				}
				JObject obj3 = json;
				content = source.ToArray();
				obj3[name] = new JArray(content);
			}
			jObject.RemoveAll();
		}
	}

	private JObject ProcessEventPatches(ref JObject json)
	{
		if (PatchesJArray == null)
		{
			return json;
		}
		string rawJson = json.ToString();
		foreach (JToken item in from t in PatchesJArray
			group t by t["token"] into g
			select g.Last())
		{
			string oldValue = item["token"].ToString();
			if (item["value"] == null)
			{
				throw new ArgumentException("Token Patch needs to contain a value property");
			}
			var (jTokenType, newValue) = UpdatePatchesValue(item, ref rawJson);
			if (jTokenType == JTokenType.String)
			{
				rawJson = rawJson.Replace(oldValue, newValue);
			}
		}
		json = parseJson(rawJson);
		PatchesJArray.RemoveAll();
		return json;
	}

	private (JTokenType jTokenType, string patchValue) UpdatePatchesValue(JToken patchToken, ref string rawJson)
	{
		JValue jValue = patchToken["value"] as JValue;
		if (jValue != null && (jValue.Type == JTokenType.Boolean || jValue.Type == JTokenType.Integer))
		{
			string newValue = string.Empty;
			if (jValue.Type == JTokenType.Boolean)
			{
				newValue = Convert.ToBoolean(jValue.Value).ToString().ToLower();
			}
			else if (jValue.Type == JTokenType.Integer)
			{
				newValue = Convert.ToInt32(jValue.Value).ToString();
			}
			rawJson = rawJson.Replace("\"%" + patchToken["id"]?.ToString() + "%\"", newValue);
		}
		if (jValue != null)
		{
			return (jTokenType: jValue.Type, patchValue: jValue.Value.ToString());
		}
		throw new Exception("patchToken doesn't have valid patchValue");
	}

	private void mergeSearch(ref JObject json, ref JObject baseJson)
	{
		foreach (JProperty item in (from t in json.Children()
			select (JProperty)t into y
			where y.Value != null || (string?)y.Value != ""
			select y).ToList())
		{
			if (item.Name == "sortBy" || item.Name == "criteria")
			{
				foreach (JObject baseProperty in baseJson.Property(item.Name).Descendants().OfType<JObject>()
					.ToList())
				{
					if (item.Name == "criteria")
					{
						foreach (JObject criteriaProp in (from x in baseProperty.Descendants().OfType<JObject>()
							where (JValue)x["id"] != null
							select x).ToList())
						{
							JObject jObject = (from x in item.Descendants().OfType<JObject>().Descendants()
									.OfType<JObject>()
								where (JValue)x["id"] != null
								select x).FirstOrDefault((JObject x) => (string)((JValue)x["id"]).Value == (string)((JValue)criteriaProp["id"]).Value);
							if (jObject != null && jObject.Count == 1)
							{
								jObject.Remove();
							}
							if (jObject != null)
							{
								criteriaProp.Remove();
							}
						}
					}
					else if (baseProperty["id"] != null)
					{
						JToken jToken = item.Value.Children().FirstOrDefault((JToken x) => (string)((JValue)x["id"]).Value == (string)((JValue)baseProperty["id"]).Value);
						if (jToken != null && jToken.Count() == 1)
						{
							jToken.Remove();
						}
						if (jToken != null)
						{
							baseProperty.Remove();
						}
					}
				}
			}
			else if (baseJson.Property(item.Name) != null)
			{
				baseJson.Property(item.Name).Remove();
			}
		}
	}

	private void processPatches(ref JObject json)
	{
		if (hasPatches(ref json))
		{
			processAndRemoveExtendedNonRFC6902Patchs(ref json);
			processAndRemoveRFC6902Patches(ref json);
		}
	}

	private void processAndRemoveRFC6902Patches(ref JObject json)
	{
		JToken jToken = json["patch"];
		if (jToken != null && jToken.HasValues)
		{
			JsonPatcher jsonPatcher = new JsonPatcher();
			PatchDocument document = PatchDocument.Parse(jToken.ToString());
			json.Remove("patch");
			JToken target = JToken.Parse(json.ToString(), JsonSettings());
			logger.Debug("Applying RFC6902 Patches");
			jsonPatcher.Patch(ref target, document);
			json = parseJson(target.ToString());
		}
	}

	private void processAndRemoveExtendedNonRFC6902Patchs(ref JObject json)
	{
		RawTokens = new List<JObject>();
		List<JObject> list = (from j in (from p in json.Properties()
				where p.Name == "patch"
				select p).Descendants().OfType<JObject>()
			where j["token"] != null
			select j).ToList();
		string rawJson = json.ToString();
		foreach (JObject item in list)
		{
			string oldValue = item["token"].ToString();
			if (item["value"] == null)
			{
				throw new ArgumentException("Token Patch needs to contain a value property");
			}
			var (jTokenType, newValue) = UpdatePatchesValue(item, ref rawJson);
			if (jTokenType == JTokenType.String)
			{
				rawJson = rawJson.Replace(oldValue, newValue);
			}
		}
		if (patchTokens != null)
		{
			foreach (JToken patchToken in patchTokens)
			{
				string text = patchToken["id"].ToString();
				if (patchToken["value"] == null)
				{
					throw new ArgumentException("Token Patch needs to contain a value property");
				}
				var (jTokenType2, newValue2) = UpdatePatchesValue(patchToken, ref rawJson);
				if (jTokenType2 == JTokenType.String)
				{
					rawJson = rawJson.Replace("%" + text + "%", newValue2);
				}
				if (patchToken["op"] == null)
				{
					patchToken["op"] = "replace";
				}
				if (patchToken["token"] == null)
				{
					patchToken["token"] = string.Format("%{0}%", patchToken["id"]);
				}
				RawTokens.Add(patchToken as JObject);
			}
		}
		json = parseJson(rawJson);
		foreach (JObject item2 in (from j in (from p in json.Properties()
				where p.Name == "patch"
				select p).Descendants().OfType<JObject>()
			where j["token"] != null
			select j).ToList())
		{
			item2.Remove();
		}
		List<JObject> list2 = (from j in (from p in json.Properties()
				where p.Name == "patch"
				select p).Descendants().OfType<JObject>()
			where j["op"] != null && j["op"].Value<string>() == "delete"
			select j).ToList();
		foreach (JObject deleteObj in list2)
		{
			if (deleteObj["value"] == null)
			{
				throw new ArgumentException("Token Patch needs to contain a value property");
			}
			(json[type] as JArray)?.Remove(((json[type] as JArray) ?? throw new InvalidOperationException()).FirstOrDefault((JToken x) => x.Value<JObject>().Property("id").Value.ToString() == deleteObj["value"].Value<string>()));
		}
		foreach (JObject item3 in list2)
		{
			item3.Remove();
		}
	}

	private void ReplaceStrings(ref JObject json, JObject localizedStrings)
	{
		if (localizedStrings == null)
		{
			return;
		}
		string text = json.ToString();
		int num = text.IndexOf("{{", StringComparison.Ordinal);
		int num2 = text.IndexOf("}}", StringComparison.Ordinal);
		while (num > -1 && num2 > -1 && num < num2)
		{
			string text2 = text.Substring(num, num2 - num + 2);
			string text3 = text2.Replace("{{", string.Empty).Replace("}}", string.Empty).Trim();
			string[] array = text3.Split(new char[1] { '.' });
			if (localizedStrings.ContainsKey(array[0]))
			{
				JObject jObject = (JObject)localizedStrings[array[0]];
				if (jObject.ContainsKey(array[1]))
				{
					string newValue = jObject[array[1]].ToString();
					text = text.Replace(text2, newValue);
				}
				else
				{
					logger.Debug("Unable to find string " + text3 + " in strings.json");
				}
			}
			else
			{
				logger.Debug("Unable to find string " + text3 + " in strings.json");
			}
			num = text.IndexOf("{{", num + 1, StringComparison.Ordinal);
			num2 = text.IndexOf("}}", num2 + 1, StringComparison.Ordinal);
		}
		json = parseJson(text);
	}

	private void VersionCheckAndUpgrade(ref JObject json)
	{
		if ((json.Count > 0 && json["$schema"] != null && (json["$schema"].ToString().EndsWith("patch.json") || json["$schema"].ToString().EndsWith("combo.json"))) || json.Count == 0 || json["$schema"] == null)
		{
			return;
		}
		int num = 1;
		while (!checkSchemaVersion(ref json, type))
		{
			json = schemaUpgrade.Upgrade(json, type);
			logger.Debug("Upgrading schema of " + type);
			if (num == 50)
			{
				throw new Exception("Unable to upgrade : " + type + ", Reached maximum count of recursive upgrade.");
			}
		}
	}

	private string GetReferenceType(string reference)
	{
		if (reference.EndsWith("#/layout/components"))
		{
			return "Udc";
		}
		if (reference.EndsWith("#/combo"))
		{
			return "combo";
		}
		return string.Empty;
	}
}
