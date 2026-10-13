using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models._4xLayoutUpgrade;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core._4xLayoutUpgrade;

public class LayoutUpgradeManager
{
	private readonly Converter converter;

	private readonly HelperMethods helperMethods;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly string rootPath;

	private readonly JsonSerializer serializer;

	private FlattenLayout flattenLayout;

	public LayoutUpgradeManager(string rootPath, ILogger logger, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		helperMethods = new HelperMethods(logger, contentLoader);
		DefaultContractResolver contractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};
		serializer = new JsonSerializer
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = contractResolver
		};
		converter = new Converter();
		flattenLayout = new FlattenLayout();
	}

	public EpMetaFxResponse<ReportResponse> Upgrade(string viewID, Func<string> currentCultureNam)
	{
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		if (string.IsNullOrEmpty(viewID))
		{
			epMetaFxResponse.Message = "Invalid view definition.";
			return epMetaFxResponse;
		}
		if (!File.Exists(Path.Combine(rootPath, viewID, "layout.draft.jsonc")))
		{
			epMetaFxResponse.Message = "layout draft doesn't exists.";
			return epMetaFxResponse;
		}
		JObject layoutDraftPageContent = ReadJsonFile(Path.Combine(rootPath, viewID, "layout.draft.jsonc"));
		JObject layoutPageContent = ReadJsonFile(Path.Combine(rootPath, viewID, "layout.jsonc"));
		string text = viewID.Split(new char[1] { '.' }).Last();
		Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			TemplateType = "PAGETEMPLATE",
			ViewType = "APPS"
		};
		JArray jArray = templateManager.GetLayout(request, currentCultureNam) as JArray;
		EpMetaFxTemplateRequest request2 = new EpMetaFxTemplateRequest
		{
			TemplateType = "TEMPLATE",
			ViewType = "APPS"
		};
		JObject jObject = templateManager.GetTemplate(request2, currentCultureNam) as JObject;
		JObject jObject2 = Clone((jArray ?? throw new InvalidOperationException()).First((JToken ob) => ob["name"].ToString() == "Details") as JObject);
		UpdateLayoutDetails(jObject2, text + "Form", string.Empty, "Apps");
		JObject jObject3 = Clone(jObject);
		jObject3["layout"] = jObject2;
		helperMethods.CreateJsonFile(jObject3, Path.Combine(rootPath, viewID), "layout.designer.draft.jsonc");
		epMetaFxResponse.Details = Upgrade(viewID, jArray, jObject, layoutDraftPageContent, layoutPageContent);
		if (Directory.Exists(Path.Combine(rootPath, viewID, "views")))
		{
			string[] files = Directory.GetFiles(Path.Combine(rootPath, viewID, "views"));
			for (int num = 0; num < files.Length; num++)
			{
				List<MessageDetail> list = new List<MessageDetail>();
				string path = files[num];
				list = UpgradeViews(viewID, jObject, path, num);
				epMetaFxResponse.Details.AddRange(list);
			}
		}
		if (epMetaFxResponse.Details.Any())
		{
			epMetaFxResponse.Message = "The tabs which are not converted.";
		}
		epMetaFxResponse.IsSuccess = true;
		epMetaFxResponse.Data = new ReportResponse
		{
			FileName = viewID,
			Id = viewID
		};
		return epMetaFxResponse;
	}

	public EpMetaFxResponse<ReportResponse> UpgradeView(string viewPageId, Func<string> currentCultureName)
	{
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		if (!Enumerable.Contains(viewPageId, char.Parse(":")))
		{
			epMetaFxResponse.Message = "Invalid view definition.";
			return epMetaFxResponse;
		}
		string text = viewPageId.Split(new char[1] { char.Parse(":") }).Last();
		string text2 = viewPageId.Split(new char[1] { char.Parse(":") }).First();
		if (string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(text))
		{
			epMetaFxResponse.Message = "Invalid view definition.";
			return epMetaFxResponse;
		}
		Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			TemplateType = "TEMPLATE",
			ViewType = "APPS"
		};
		JObject template = templateManager.GetTemplate(request, currentCultureName) as JObject;
		string[] files = Directory.GetFiles(Path.Combine(rootPath, text2, "views"));
		string text3 = Path.Combine(rootPath, text2, "views", text + ".jsonc");
		int index = Array.IndexOf(files, text3);
		if (Enumerable.Contains(files, text3))
		{
			List<MessageDetail> list = new List<MessageDetail>();
			list = UpgradeViews(text2, template, text3, index);
			epMetaFxResponse.Details.AddRange(list);
			epMetaFxResponse.Message = "View upgraded successfully.";
			epMetaFxResponse.IsSuccess = true;
		}
		else
		{
			epMetaFxResponse.Message = "View doesn't exist.";
		}
		return epMetaFxResponse;
	}

	private List<MessageDetail> UpgradeViews(string viewId, JObject template, string path, int index)
	{
		JObject layoutPageContent = ReadJsonFile(path);
		return UpgradeAdditionalView(viewId, path, layoutPageContent, template, index);
	}

	private void UpdateLayoutDetails(JObject layout, string appName, string parentPage, string viewType, string tabId = "")
	{
		layout["name"] = appName;
		layout["guid"] = Guid.NewGuid().ToString();
		layout["caption"] = appName;
		layout["viewType"] = viewType;
		layout["navigation"]["pageCaption"] = appName;
		layout["navigation"]["epBindingLevel"] = 0;
		layout["navigation"]["epBindingPeerOrder"] = 0;
		layout["navigation"]["parentPage"] = parentPage;
		layout["navigation"]["tabId"] = tabId;
	}

	private List<MessageDetail> UpgradeAdditionalView(string viewID, string filePath, JObject layoutPageContent, JObject emptyPageTemplate, int index)
	{
		List<MessageDetail> result = new List<MessageDetail>();
		JObject jObject = Clone(emptyPageTemplate);
		JObject jObject2 = Clone(layoutPageContent)["layout"] as JObject;
		if (jObject2 != null)
		{
			List<JToken> list = jObject2.SelectTokens("$.components.[*]", errorWhenNoMatch: false).ToList();
			List<JToken> labelControls = list.Where((JToken ctrl) => ctrl["sourceTypeId"]?.ToString() == "ep-label" && string.IsNullOrEmpty(ctrl["parentId"]?.ToString())).ToList();
			List<object> list2 = new List<object>();
			for (int num = 0; num < list.Count; num++)
			{
				JToken jToken = list[num];
				string text = "";
				string text2 = (text = jToken["id"]?.ToString());
				if (!string.IsNullOrEmpty(text2))
				{
					jToken["id"] = $"{text2}_{index}";
				}
				text2 = jToken["model"]?["id"]?.ToString();
				if (!string.IsNullOrEmpty(text2))
				{
					jToken["model"]["id"] = $"{text2}_{index}";
				}
				text2 = jToken["parentId"]?.ToString();
				if (!string.IsNullOrEmpty(text2))
				{
					jToken["parentId"] = $"{text2}_{index}";
				}
				text2 = jToken["model"]?["epiLabel"]?.ToString();
				if (!string.IsNullOrEmpty(text2))
				{
					jToken["model"]["epiLabel"] = $"{text2}_{index}";
				}
				if (!(jToken["model"]["data"] is JArray jArray))
				{
					continue;
				}
				foreach (JToken item in jArray)
				{
					string text3 = item["id"]?.ToString();
					if (!string.IsNullOrEmpty(text3))
					{
						item["id"] = $"{text}~{text3}_{index}";
					}
				}
			}
			List<JToken> list3 = new List<JToken>();
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				JToken jToken2 = list[num2];
				if (string.IsNullOrEmpty(jToken2["sourceTypeId"]?.ToString()))
				{
					continue;
				}
				string text4 = jToken2["sourceTypeId"].ToString();
				bool showWarning;
				if (!(text4 == "metafx-tabstrip"))
				{
					if (text4 == "Panel")
					{
						if (string.IsNullOrEmpty(jToken2["parentId"]?.ToString()))
						{
							jToken2["pageName"] = jObject2["caption"].ToString();
							object obj = converter.Convert(jToken2, list, out showWarning);
							if (obj != null)
							{
								list2.Add(obj);
							}
						}
					}
					else if (string.IsNullOrEmpty(jToken2["parentId"]?.ToString()))
					{
						list3.Add(jToken2);
					}
					continue;
				}
				foreach (JToken item2 in jToken2["model"]["data"] as JArray)
				{
					object obj2 = converter.Convert(item2, list, out showWarning);
					if (obj2 != null)
					{
						list2.Add(obj2);
					}
				}
			}
			List<object> collection = converter.ConvertTopControls(list3, labelControls);
			list2.AddRange(collection);
			jObject2["components"] = JArray.FromObject(list2, serializer);
		}
		if (jObject2 != null)
		{
			UpdateLayoutDetails(jObject2, jObject2["caption"].ToString(), string.Empty, "SlidingPanel");
			jObject["layout"] = flattenLayout.ConvertToFlattenLayout(jObject2);
		}
		helperMethods.CreateJsonFile(jObject, Path.Combine(rootPath, viewID, "pages"), Path.GetFileNameWithoutExtension(filePath) + ".layout.designer.draft.jsonc");
		return result;
	}

	private List<MessageDetail> Upgrade(string viewID, JArray defaultPages, JObject emptyPageTemplate, JObject layoutDraftPageContent, JObject layoutPageContent)
	{
		List<MessageDetail> list = new List<MessageDetail>();
		JObject jObject = Clone(layoutPageContent)["layout"] as JObject;
		Clone(layoutDraftPageContent);
		JObject jObject2 = layoutDraftPageContent["layout"] as JObject;
		if (jObject != null)
		{
			List<JToken> source = (from tk in jObject.SelectTokens("$.components.[*]", errorWhenNoMatch: false)
				where tk["sourceTypeId"]?.ToString() == "erp-combo-box"
				select tk).ToList();
			if (jObject2 != null)
			{
				List<JToken> list2 = jObject2.SelectTokens("$.components.[*]", errorWhenNoMatch: false).ToList();
				foreach (JToken draftToken in list2.Where((JToken tk) => tk["sourceTypeId"]?.ToString() == "erp-combo-box"))
				{
					JToken jToken = source.FirstOrDefault((JToken tk) => tk["id"]?.ToString() == draftToken["id"]?.ToString());
					if (jToken != null && !string.IsNullOrEmpty(jToken["model"]?["$ref"]?.ToString()))
					{
						draftToken["model"] = jToken["model"];
					}
				}
				Dictionary<string, List<object>> dictionary = new Dictionary<string, List<object>>();
				for (int num = 0; num < list2.Count; num++)
				{
					JToken jToken2 = list2[num];
					if (string.IsNullOrEmpty(jToken2["sourceTypeId"]?.ToString()) || !(jToken2["sourceTypeId"].ToString() == "metafx-tabstrip") || !(jToken2["model"]["data"] is JArray jArray))
					{
						continue;
					}
					foreach (JToken item in jArray)
					{
						bool flag = false;
						JToken arg = item["id"];
						item["id"] = string.Format("{0}~{1}", jToken2["id"], arg);
						object obj = converter.Convert(item, list2, out var showWarning);
						if (obj == null)
						{
							if (showWarning)
							{
								list.Add(new MessageDetail
								{
									Type = "warning",
									Message = string.Format("{0} ({1})", item["title"], arg)
								});
							}
							continue;
						}
						string key = item["pageName"].ToString();
						if (!dictionary.ContainsKey(key))
						{
							dictionary[key] = new List<object>();
						}
						string text = Convert.ToString(item["panelCardId"]);
						if (obj is MetaFxComponent<MetaFxPanelCard>)
						{
							MetaFxComponent<MetaFxPanelCard> metaFxComponent = obj as MetaFxComponent<MetaFxPanelCard>;
							foreach (object item2 in dictionary[key])
							{
								if (item2 is MetaFxComponent<MetaFxPanelCard>)
								{
									MetaFxComponent<MetaFxPanelCard> metaFxComponent2 = item2 as MetaFxComponent<MetaFxPanelCard>;
									if (metaFxComponent2.Id == text)
									{
										metaFxComponent2.Model.BodyComponents.AddRange(metaFxComponent.Model.BodyComponents);
										metaFxComponent2.Model.Title = text;
										flag = true;
										break;
									}
								}
							}
						}
						if (!flag)
						{
							dictionary[key].Add(obj);
						}
					}
				}
				foreach (KeyValuePair<string, JObject> item3 in CreatePages(dictionary, viewID, defaultPages, emptyPageTemplate))
				{
					string fullName = Directory.GetParent(Path.GetFullPath(item3.Key)).FullName;
					item3.Value["layout"] = flattenLayout.ConvertToFlattenLayout(item3.Value["layout"] as JObject);
					helperMethods.CreateJsonFile(item3.Value, fullName, Path.GetFileName(item3.Key) + ".layout.designer.draft.jsonc");
				}
			}
		}
		return list;
	}

	private Dictionary<string, JObject> CreatePages(Dictionary<string, List<object>> pageContents, string viewID, JArray defaultPages, JObject emptyPageTemplate)
	{
		Dictionary<string, JObject> dictionary = new Dictionary<string, JObject>();
		string text = Path.Combine(rootPath, viewID, "pages");
		helperMethods.EmptyDirectory(text);
		foreach (JToken defaultPage in defaultPages)
		{
			if (defaultPage["name"].ToString() == "Main")
			{
				defaultPage["navigation"]["parentPage"] = viewID.Split(new char[1] { '.' }).Last() + "Form";
				JToken? jToken = defaultPage["components"][0]["model"]["data"];
				jToken[0]["id"] = "Activity";
				jToken[0]["title"] = "Activity";
				jToken[1]["id"] = "Details";
				jToken[1]["title"] = "Details";
			}
			JObject jObject = Clone(emptyPageTemplate);
			jObject["layout"] = defaultPage;
			dictionary[Path.Combine(text, defaultPage["name"].ToString())] = jObject;
		}
		foreach (KeyValuePair<string, List<object>> pageContent in pageContents)
		{
			string[] array = pageContent.Key.Split(new char[1] { ':' });
			string key = text;
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				key = Path.Combine(text, text2);
				if (!dictionary.ContainsKey(key))
				{
					JObject jObject2 = Clone(emptyPageTemplate);
					JObject layout = (JObject)(jObject2["layout"] = Clone(defaultPages.First((JToken df) => df["name"]?.ToString() == "Details")) as JObject);
					string text3 = ((i != 0) ? array[i - 1] : "Main");
					string tabId = ((text3 == "Main") ? text2 : array[0]);
					UpdateLayoutDetails(layout, text2, text3, "TabPage", tabId);
					dictionary[key] = jObject2;
				}
			}
			JArray jArray = dictionary[key]["layout"]["components"] as JArray;
			foreach (object item in pageContent.Value)
			{
				jArray?.Add(JToken.FromObject(item, serializer));
			}
		}
		return dictionary;
	}

	private JObject ReadJsonFile(string file)
	{
		if (!contentLoader.Exists(file))
		{
			logger.Debug("Processing Error : No file found " + file);
			throw new FileNotFoundException("Processing Error : No file found " + file);
		}
		return JObject.Parse(contentLoader.ReadAllText(file), new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		});
	}

	private T Clone<T>(T source)
	{
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
	}
}
