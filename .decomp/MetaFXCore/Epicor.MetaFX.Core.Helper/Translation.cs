using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Layers;
using Epicor.MetaFX.Core.Models.MetaUI.Strings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core.Helper;

public class Translation
{
	private readonly ICacheService cache;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly IJsonMerger jsonMerger;

	private readonly JObject translatableStrings;

	private string generatedStringFile;

	private Dictionary<string, string> untranslatableStrings = new Dictionary<string, string>();

	private readonly IFileProvider _fileProvider;

	public string TranslatedLayout { get; private set; }

	public string TranslatedSearch { get; private set; }

	public string TranslatedTools { get; private set; }

	public string TranslatedDataViews { get; private set; }

	public List<string> TranslatedPages { get; private set; } = new List<string>();

	public string TranslatedEvents { get; private set; }

	public string TranslatedRules { get; private set; }

	public Translation(ICacheService cache, ILogger logger, IContentLoader contentLoader, IJsonMerger jsonMerger, [Optional] IFileProvider provider)
	{
		this.logger = logger;
		this.contentLoader = contentLoader;
		this.jsonMerger = jsonMerger;
		this.cache = cache;
		translatableStrings = DefaultStringsModel();
		_fileProvider = provider ?? new FileProvider();
	}

	private string StringsJsonFile()
	{
		string getCacheLocation = ((ICacheLocation)cache).GetCacheLocation;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		return Path.Combine(getCacheLocation.Replace(directorySeparatorChar + "cache", ""), "strings.jsonc");
	}

	public void ExtractTranslatableText(string metaFilePath, string _type, LayerRequest layerRequest = null, bool baseStrings = false, string mode = "", int uxAppVersion = 0)
	{
		string layerCacheKey = ((layerRequest == null) ? null : layerRequest.CacheKey);
		string getCacheLocation = ((ICacheLocation)cache).GetCacheLocation;
		string text = (baseStrings ? "baseStrings" : "strings");
		generatedStringFile = Path.Combine(getCacheLocation, text + ".generated.json");
		if (_type == "layout")
		{
			TranslatedLayout = Path.Combine(getCacheLocation, CacheKey("layout.processed", metaFilePath, layerCacheKey));
			cache.Get(CacheKey("layout.processed", metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, _type, layerRequest));
		}
		else if (_type == "search")
		{
			TranslatedSearch = Path.Combine(getCacheLocation, CacheKey("search.processed", metaFilePath));
			cache.Get(CacheKey("search.processed", metaFilePath), () => GenerateTranslatableText(metaFilePath, _type));
		}
		else if (_type == "tools")
		{
			TranslatedTools = Path.Combine(getCacheLocation, CacheKey("tools.processed", metaFilePath, layerCacheKey));
			cache.Get(CacheKey("tools.processed", metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, _type, layerRequest));
		}
		else if (_type == "dataviews")
		{
			TranslatedDataViews = Path.Combine(getCacheLocation, CacheKey("dataview.processed", metaFilePath, layerCacheKey));
			cache.Get(CacheKey("dataview.processed", metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, _type, layerRequest));
		}
		else if (_type == "pages")
		{
			string pageName = GetPagename(metaFilePath, uxAppVersion);
			string text2 = Path.GetFileName(metaFilePath).Replace(Path.GetExtension(metaFilePath), "");
			if (mode == "ApplicationStrings")
			{
				GenerateTranslatableText(metaFilePath, "layout", layerRequest, pageName);
			}
			else
			{
				string item = Path.Combine(getCacheLocation, CacheKey("page." + text2, metaFilePath, layerCacheKey));
				TranslatedPages.Add(item);
				cache.Get(CacheKey("page." + text2, metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, "layout", layerRequest, pageName));
			}
		}
		else if (_type == "events")
		{
			TranslatedEvents = Path.Combine(getCacheLocation, CacheKey("events.processed", metaFilePath, layerCacheKey));
			cache.Get(CacheKey("events.processed", metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, _type, layerRequest));
		}
		else if (_type == "rules")
		{
			TranslatedRules = Path.Combine(getCacheLocation, CacheKey("rules.processed", metaFilePath, layerCacheKey));
			cache.Get(CacheKey("rules.processed", metaFilePath, layerCacheKey), () => GenerateTranslatableText(metaFilePath, _type, layerRequest));
		}
		if (!contentLoader.Exists(StringsJsonFile()))
		{
			CreateStringsJsonFile();
		}
	}

	private string GetPagename(string metaFilePath, int uxpAppVersion)
	{
		string result = null;
		if (contentLoader.Exists(metaFilePath))
		{
			dynamic val = JObject.Parse(contentLoader.ReadAllText(metaFilePath, uxpAppVersion), new JsonLoadSettings
			{
				CommentHandling = CommentHandling.Ignore
			})["layout"];
			result = (string)val.name;
		}
		return result;
	}

	public JObject MergeWithGeneratedString(JObject stringsJson)
	{
		if (stringsJson != null)
		{
			if (contentLoader.Exists(generatedStringFile))
			{
				JObject jObject = JObject.Parse(contentLoader.ReadAllText(generatedStringFile));
				foreach (JProperty item in jObject.Properties())
				{
					if (item.Name == "$schema" || item.Name == "contentVersion" || item.Name == "imports" || item.Name == "patch")
					{
						continue;
					}
					if (stringsJson.ContainsKey(item.Name))
					{
						JObject obj = (JObject)jObject.Property(item.Name).Value;
						JObject jObject2 = (JObject)stringsJson.GetValue(item.Name);
						foreach (KeyValuePair<string, JToken> item2 in obj)
						{
							if (!jObject2.ContainsKey(item2.Key))
							{
								jObject2.Add(item2.Key, item2.Value);
							}
						}
					}
					else
					{
						stringsJson.Add(item.Name, jObject[item.Name]);
					}
				}
			}
			logger.Debug("strings merged");
			return stringsJson;
		}
		if (contentLoader.Exists(generatedStringFile))
		{
			return JObject.Parse(contentLoader.ReadAllText(generatedStringFile));
		}
		return null;
	}

	private object GenerateTranslatableText(string metaFilePath, string _type, LayerRequest layerRequest = null, string pageName = null)
	{
		JObject jObject = jsonMerger.Load(metaFilePath, _type, null, shouldConvertToHierarchicalStructure: true, layerRequest, pageName);
		string text = jObject.ToString();
		switch (_type)
		{
		case "layout":
		case "pages":
		{
			string[] array = LayoutRegexPatterns();
			foreach (string pattern2 in array)
			{
				text = Regex.Replace(text, pattern2, ProcessTranslatableText);
			}
			break;
		}
		case "search":
		{
			string[] array = SearchRegexPatterns();
			foreach (string pattern4 in array)
			{
				text = Regex.Replace(text, pattern4, ProcessTranslatableText);
			}
			break;
		}
		case "tools":
		case "dataviews":
		{
			if (_type == "dataviews")
			{
				text = GenerateToolsTranslatableText(jObject).ToString();
			}
			string[] array = ToolsRegexPatterns();
			foreach (string pattern3 in array)
			{
				text = Regex.Replace(text, pattern3, ProcessTranslatableText);
			}
			break;
		}
		case "events":
		{
			string[] array = EventsRegexPatterns();
			foreach (string pattern in array)
			{
				text = Regex.Replace(text, pattern, ProcessTranslatableText);
			}
			break;
		}
		}
		PersistGeneratedStrings(generatedStringFile);
		logger.Debug("generated GenerateTranslatableText");
		return JObject.Parse(text);
	}

	private JObject GenerateToolsTranslatableText(JObject jsonContent)
	{
		JToken? jToken = jsonContent["dataviews"];
		JToken jToken2 = null;
		foreach (JToken item in (IEnumerable<JToken>)jToken)
		{
			if (item["tools"] == null)
			{
				continue;
			}
			jToken2 = item["tools"]["new"];
			JToken jToken3 = item["tools"]["delete"];
			if (jToken3 != null && jToken3["text"] != null && jToken3["text"].ToString().Length != 0)
			{
				jToken3["text"] = "Delete " + jToken3["text"];
				if (jToken3["custom"] != null)
				{
					foreach (JToken item2 in (IEnumerable<JToken>)jToken3["custom"])
					{
						if (item2["text"] != null)
						{
							item2["text"] = "Delete " + item2["text"];
						}
					}
				}
			}
			if (jToken2 == null || jToken2["text"] == null)
			{
				continue;
			}
			if (jToken2["custom"] != null)
			{
				foreach (JToken item3 in (IEnumerable<JToken>)jToken2["custom"])
				{
					if (item3["text"] != null)
					{
						item3["text"] = "New " + item3["text"];
					}
				}
			}
			if (jToken3 == null || jToken3["text"] == null || jToken3["text"].ToString().Length == 0)
			{
				if (jToken3 == null)
				{
					(item["tools"] as JObject).Add("delete", JObject.Parse("{}"));
				}
				if (item["tools"]["delete"]["text"] == null)
				{
					(item["tools"]["delete"] as JObject).Add("text", "");
				}
				item["tools"]["delete"]["text"] = "Delete " + jToken2["text"];
			}
			jToken2["text"] = "New " + jToken2["text"];
		}
		return jsonContent;
	}

	private string ProcessTranslatableText(Match match)
	{
		string value = match.Groups[1].Value;
		string value2 = match.Groups[0].Value;
		if (value.Contains("{{"))
		{
			return match.ToString();
		}
		string text = ConvertFilter(value2, value);
		if (text != "")
		{
			return text.ToString();
		}
		string text2 = value.Trim();
		text2 = text2.TrimEnd(new char[1] { ':' });
		if (text2 == string.Empty)
		{
			return match.ToString();
		}
		string text3 = Key(text2);
		string result = match.ToString().Replace(text2, "{{strings." + text3 + "}}");
		if (DoesTranslatableStringsContainsKey(text3))
		{
			if (!((string?)translatableStrings["strings"][text3] == text2) && (!DoesTranslatableStringsContainsKey("Custom" + text3) || !((string?)translatableStrings["strings"]["Custom" + text3] == text2)))
			{
				result = match.ToString().Replace(text2, "{{strings.Custom" + text3 + "}}");
				if (!((JObject)translatableStrings["strings"]).ContainsKey("Custom" + text3))
				{
					((JObject)translatableStrings["strings"]).Add("Custom" + text3, text2);
				}
			}
			return result;
		}
		((JObject)translatableStrings["strings"]).Add(text3, text2);
		return result;
	}

	private string ProcessTranslatableText(string text)
	{
		if (text.Contains("{{"))
		{
			return text;
		}
		string text2 = text.Trim();
		text2 = text2.TrimEnd(new char[1] { ':' });
		if (text2 == string.Empty)
		{
			return text.ToString();
		}
		string text3 = Key(text2);
		string result = text.Replace(text2, "{{strings." + text3 + "}}");
		if (DoesTranslatableStringsContainsKey(text3))
		{
			return result;
		}
		((JObject)translatableStrings["strings"]).Add(text3, text2);
		return result;
	}

	private bool DoesTranslatableStringsContainsKey(string key)
	{
		if (translatableStrings["strings"].HasValues && ((JObject)translatableStrings["strings"]).GetValue(key) != null)
		{
			return true;
		}
		return false;
	}

	private string CacheKey(string name, string filePath, string layerCacheKey = null)
	{
		if (string.IsNullOrEmpty(layerCacheKey))
		{
			return name + ".json";
		}
		return name + "." + layerCacheKey + ".json";
	}

	internal void PersistGeneratedStrings(string generatedStringFilePath)
	{
		DefaultContractResolver contractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};
		string content = JsonConvert.SerializeObject(translatableStrings, new JsonSerializerSettings
		{
			ContractResolver = contractResolver,
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
		logger.Debug("generated " + generatedStringFilePath + " file");
		_fileProvider.WriteAllTextThreadSafe(generatedStringFilePath, content);
	}

	private void CreateStringsJsonFile()
	{
		DefaultContractResolver contractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};
		string contents = JsonConvert.SerializeObject(DefaultStringsModel(), new JsonSerializerSettings
		{
			ContractResolver = contractResolver,
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
		contentLoader.WriteAllText(StringsJsonFile(), contents);
	}

	private JObject DefaultStringsModel()
	{
		return new JObject
		{
			{ "$schema", "https://schemas.developer.epicor.com/schemas/v1/Strings.json" },
			{ "contentVersion", "1.0" },
			{
				"imports",
				new JArray()
			},
			{
				"strings",
				new JObject()
			},
			{
				"patch",
				new JArray()
			}
		};
	}

	private string[] LayoutRegexPatterns()
	{
		return new string[26]
		{
			".*enabledCaption\":\\s*\"(.+)\"", ".*disabledCaption\":\\s*\"(.+)\"", ".*caption\":\\s*\"(.+)\"", ".*display\":\\s*\"(.+)\"", ".*emptyLabelText\":\\s*\"(.+)\"", ".*singularLabelText\":\\s*\"(.+)\"", ".*pluralLabelText\":\\s*\"(.+)\"", ".*errorMessage\":\\s*\"(.+)\"", ".*filter\":\\s*\"(.+)\"", ".*title\":\\s*\"(.+)\"",
			".*labelText\":\\s*\"(.+)\"", ".*pageCaption\":\\s*\"(.+)\"", ".*trueCaption\":\\s*\"(.+)\"", ".*falseCaption\":\\s*\"(.+)\"", ".*enableCaption\":\\s*\"(.+)\"", ".*disableCaption\":\\s*\"(.+)\"", ".*label\":\\s*\"(.+)\"", ".*description\":\\s*\"(.+)\"", ".*comboBoxLabel\":\\s*\"(.+)\"", ".*titleNodeText\":\\s*\"(.+)\"",
			".*partDescLabel\":\\s*\"(.+)\"", ".*partRevLabel\":\\s*\"(.+)\"", ".*text\":\\s*\"(.+)\"", ".*labelTextSearchDesc\":\\s*\"(.+)\"", ".*headerText\":\\s*\"(.+)\"", ".*columnCaptions\":\\s*\\[\\s*\"(.+)\"\\s*\\]"
		};
	}

	private string[] SearchRegexPatterns()
	{
		return new string[6] { ".*title\":\\s*\"(.+)\"", ".*label\":\\s*\"(.+)\"", ".*display\":\\s*\"(.+)\"", ".*StartingAt\":\\s*\"(.+)\"", ".*labelText\":\\s*\"(.+)\"", ".*labelTextSearchDesc\":\\s*\"(.+)\"" };
	}

	private string[] ToolsRegexPatterns()
	{
		return new string[1] { ".*text\":\\s*\"(.+)\"" };
	}

	private string[] EventsRegexPatterns()
	{
		return new string[3] { ".*title\":\\s*\"(.+)\"", ".*message\":\\s*\"(.+)\"", ".*errorMessage\":\\s*\"(.+)\"" };
	}

	private string TranslateColumnCaptions(string searchContent)
	{
		JObject jObject = JObject.Parse(searchContent);
		foreach (JToken item in (JArray)jObject["search"]["columns"])
		{
			JValue jValue = (JValue)item["title"];
			if (jValue != null)
			{
				jValue.Value = ProcessTranslatableText(jValue.Value.ToString());
			}
		}
		return jObject.ToString();
	}

	private string Key(string text)
	{
		string[] array = new string[8] { " ", ".", "*", "(", ")", "|", "{", "}" };
		foreach (string oldValue in array)
		{
			text = text.Replace(oldValue, "");
		}
		return text;
	}

	private string ConvertFilter(string originalStr, string filterCaption)
	{
		string text = originalStr.Replace("\"", "").Replace("/", "").Trim();
		List<string> source = new List<string> { ">", "<", "=", "<>", "?" };
		if (text.StartsWith("filter:"))
		{
			if (!source.Any((string x) => originalStr.Contains(x)))
			{
				string text2 = Key(filterCaption);
				string result = originalStr.Replace(filterCaption, "{{strings." + text2 + "}}");
				if (!DoesTranslatableStringsContainsKey(text2))
				{
					((JObject)translatableStrings["strings"]).Add(text2, filterCaption);
				}
				return result;
			}
			if (source.Any((string x) => originalStr.Contains(x)))
			{
				return originalStr;
			}
		}
		return "";
	}

	internal void GenerateNotTranslatedStrings(JObject strings, JObject localizedStrings, string culture)
	{
		if (localizedStrings["strings"] == null)
		{
			return;
		}
		List<string> list = new List<string>();
		untranslatableStrings.Clear();
		foreach (JProperty item in (IEnumerable<JToken>)localizedStrings["strings"])
		{
			if (((JValue)strings["strings"][item.Name]).Value == ((JValue)item.Value).Value)
			{
				list.Add(item.Value.ToString());
				untranslatableStrings.Add(item.Name, item.Value.ToString());
			}
		}
		_fileProvider.WriteAllTextThreadSafe(Path.Combine(((ICacheLocation)cache).GetCacheLocation, culture + ".strings.untranslated.txt"), ArrayToString(list.ToArray()));
	}

	private string ArrayToString(string[] arr)
	{
		return arr.Aggregate("", (string current, string num) => current + num + Environment.NewLine);
	}

	public List<ApplicationString> GetApplicationStrings(bool hasLayer)
	{
		List<ApplicationString> list = new List<ApplicationString>();
		string cacheFolderPath;
		if (!hasLayer)
		{
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			cacheFolderPath = directorySeparatorChar + "strings.generated.json";
		}
		else
		{
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			cacheFolderPath = directorySeparatorChar + "baseStrings.generated.json";
		}
		UpdateApplicationStrings(list, cacheFolderPath);
		if (hasLayer)
		{
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			UpdateApplicationStrings(list, directorySeparatorChar + "strings.generated.json", hasLayer);
		}
		UpdateAppMissingStrings(list);
		return list;
	}

	private void UpdateApplicationStrings(List<ApplicationString> appStrings, string cacheFolderPath, bool isLayer = false)
	{
		cacheFolderPath = ((ICacheLocation)cache).GetCacheLocation + cacheFolderPath;
		if (!contentLoader.Exists(cacheFolderPath))
		{
			return;
		}
		foreach (JProperty item in ((JObject)JObject.Parse(contentLoader.ReadAllText(cacheFolderPath))["strings"]).Properties())
		{
			UpdateAppStringsList(appStrings, new ApplicationString(item.Name, (string?)item.Value, isLayer));
		}
	}

	private void UpdateAppMissingStrings(List<ApplicationString> appStrings)
	{
		foreach (KeyValuePair<string, string> untranslatableString in untranslatableStrings)
		{
			UpdateAppStringsList(appStrings, new ApplicationString(untranslatableString.Key, untranslatableString.Value, layer: false, missing: true));
		}
	}

	private void UpdateAppStringsList(List<ApplicationString> appStrings, ApplicationString appString)
	{
		if (!appStrings.Any((ApplicationString appStr) => appStr.Id == appString.Id))
		{
			appStrings.Add(appString);
		}
	}

	public void ResetTranlatableStings()
	{
		translatableStrings["strings"] = new JObject();
	}
}
