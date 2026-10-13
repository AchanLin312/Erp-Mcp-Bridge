using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.ReusableComponents;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;

internal abstract class BaseMetadataType
{
	protected readonly ILogger logger;

	protected readonly string rootPath;

	protected readonly string draft_file_name;

	protected readonly string base_file;

	protected const string MODE = "mode";

	protected const string ADD = "A";

	protected const string UPDATE = "U";

	protected const string DELETE = "D";

	protected readonly string _type;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private string control = string.Empty;

	protected BaseMetadataType(string rootPath, ILogger logger, string _type, IContentLoader contentLoader, ICacheService cacheService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this._type = _type;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		draft_file_name = _type + ".draft.jsonc";
		base_file = _type + ".jsonc";
	}

	public virtual void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		logger.Debug("Saving the draft content of " + _type + " the " + request.id);
		JArray jArray = (JArray)(request.GetType().GetProperty(_type)?.GetValue(request, null));
		if (jArray != null && jArray.HasValues)
		{
			string path = Path.Combine(rootPath, request.id, draft_file_name);
			JArray deletePatches = new JArray();
			JObject jObject = LoadFileContent(request.id, request.viewType, currentCultureName, isDraft: true);
			jObject[_type] = ProcessModifiedFlags(jObject[_type] as JArray, jArray, ref deletePatches, isDraft: true);
			string contents = JsonConvert.SerializeObject(jObject, new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore
			});
			contentLoader.WriteAllText(path, contents);
		}
	}

	public virtual void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool inBulkPublish = false)
	{
		logger.Debug("Publishing the draft content of " + _type + " the " + request.id);
		JArray jArray = (JArray)(request.GetType().GetProperty(_type)?.GetValue(request, null));
		if (jArray == null)
		{
			string path = Path.Combine(rootPath, request.id, draft_file_name);
			if (contentLoader.Exists(path))
			{
				IEnumerable<JToken> enumerable = JObject.Parse(contentLoader.ReadAllText(path))?.SelectTokens("$." + _type + "[?(@.mode)]");
				if (enumerable != null)
				{
					jArray = new JArray(enumerable);
				}
			}
		}
		if (jArray != null && jArray.HasValues)
		{
			string text = Path.Combine(rootPath, request.id, base_file);
			logger.Debug("Saving " + _type + " into " + text);
			JArray deletePatches = new JArray();
			JObject jObject = LoadFileContent(request.id, request.viewType, currentCultureName);
			jObject[_type] = ProcessModifiedFlags(jObject[_type] as JArray, jArray, ref deletePatches);
			if (jObject["patch"] == null && deletePatches.Count > 0)
			{
				jObject["patch"] = new JArray();
			}
			foreach (JToken patch in deletePatches)
			{
				if ((jObject["patch"] as JArray)?.FirstOrDefault((JToken x) => x["op"].Value<string>() == "delete" && x["value"].Value<string>() == patch["value"].Value<string>()) == null && HasJToken(request.id, patch["value"].Value<string>()))
				{
					(jObject["patch"] as JArray)?.Add(patch);
				}
			}
			string contents = JsonConvert.SerializeObject(jObject, new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore
			});
			contentLoader.WriteAllText(text, contents);
		}
		DeleteDraftFile(request.id);
	}

	public virtual bool HasActiveDraft(string viewId)
	{
		string path = Path.Combine(rootPath, viewId, draft_file_name);
		return contentLoader.Exists(path);
	}

	protected void ResetReusableComboRef(JObject layout, bool isPage)
	{
		foreach (JToken item in (IEnumerable<JToken>)layout["layout"]["components"])
		{
			UpdateReusableComboRef(item as JObject, isPage);
		}
	}

	private void UpdateReusableComboRef(JObject model, bool isPage)
	{
		if (model == null)
		{
			return;
		}
		foreach (JProperty item in model.Properties())
		{
			if (item.Name == "subType" && !string.IsNullOrEmpty(item.Value.ToString()))
			{
				RemoveExpandedReusableComboProps(model, isPage);
				break;
			}
			if (item.Value is JObject)
			{
				UpdateReusableComboRef(item.Value as JObject, isPage);
			}
			if (!(item.Value is JArray))
			{
				continue;
			}
			foreach (JToken item2 in item.Value as JArray)
			{
				UpdateReusableComboRef(item2 as JObject, isPage);
			}
		}
	}

	private void RemoveExpandedReusableComboProps(JObject model, bool isPage)
	{
		List<string> requiredProperties = new List<string> { "epBindingTextColumn", "filtersAppend", "filtersParams", "hiddenColumnsAppend", "$ref" };
		List<string> reusableComboProperties = new List<string>
		{
			"comboId", "svc", "svcPath", "tableName", "dropDownStyle", "valueField", "textField", "viewName", "sort", "columns",
			"filters", "returnObj", "required", "isEpiReadOnly", "appendList", "isDelimited", "parameterName", "retrieveColumns", "hiddenColumns", "type",
			"subType"
		};
		if (!string.IsNullOrEmpty(model["subType"]?.ToString()) && !string.IsNullOrEmpty(model["type"]?.ToString()))
		{
			string text = "../shared/combo/" + model["type"].ToString() + "/" + model["subType"].ToString() + "/combo.jsonc#/combo";
			JToken value = (model["$ref"] = (isPage ? ("../" + text) : text));
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

	protected JObject LoadFileContent(string viewId, string templateType, Func<string> currentCultureName, bool isDraft = false)
	{
		string text = Path.Combine(rootPath, viewId, base_file);
		string path = Path.Combine(rootPath, viewId, draft_file_name);
		logger.Debug("Saving " + _type + " into " + text);
		if (isDraft && contentLoader.Exists(path))
		{
			return JObject.Parse(contentLoader.ReadAllText(path));
		}
		if (contentLoader.Exists(text))
		{
			return JObject.Parse(contentLoader.ReadAllText(text));
		}
		EpMetaFxTemplateRequest request = new EpMetaFxTemplateRequest
		{
			TemplateType = templateType
		};
		return JObject.FromObject(new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader).LoadTypesDefaultContent(request, _type, currentCultureName));
	}

	private JArray ProcessModifiedFlags(JArray contentArray, JArray modifiedContent, ref JArray deletePatches, bool isDraft = false)
	{
		logger.Debug("Applying A, D, U Flags to  " + _type);
		foreach (JToken jToken in modifiedContent)
		{
			if (jToken["mode"] != null && (jToken["mode"].Value<string>() == "A" || jToken["mode"].Value<string>() == "U"))
			{
				if (!isDraft)
				{
					(jToken as JObject)?.Property("mode").Remove();
				}
				JToken jToken2 = contentArray.FirstOrDefault((JToken x) => x["id"].Value<string>() == jToken["id"].Value<string>());
				if (jToken2 == null)
				{
					contentArray.Add(jToken);
					continue;
				}
				int index = contentArray.IndexOf(jToken2);
				contentArray.RemoveAt(index);
				contentArray.Insert(index, jToken);
			}
			else
			{
				if (jToken["mode"] == null || !(jToken["mode"].Value<string>() == "D"))
				{
					continue;
				}
				JToken jToken3 = contentArray.FirstOrDefault((JToken x) => x["id"].Value<string>() == jToken["id"].Value<string>());
				if (jToken3 != null && jToken3.HasValues)
				{
					if (isDraft)
					{
						int index2 = contentArray.IndexOf(jToken3);
						contentArray.RemoveAt(index2);
						contentArray.Insert(index2, jToken);
					}
					else
					{
						contentArray.Remove(contentArray.FirstOrDefault((JToken x) => x["id"].Value<string>() == jToken["id"].Value<string>()));
					}
				}
				else if (!isDraft)
				{
					dynamic val = new JObject();
					val.op = "delete";
					val.value = jToken.SelectToken("id").Value<string>();
					deletePatches.Add(val);
				}
				else
				{
					contentArray.Add(jToken);
				}
			}
		}
		return contentArray;
	}

	private void DeleteDraftFile(string viewId)
	{
		logger.Debug("Deleting the draft " + _type + " after publish for the " + viewId);
		string path = Path.Combine(rootPath, viewId, draft_file_name);
		if (contentLoader.Exists(path))
		{
			contentLoader.Delete(path);
		}
	}

	private bool HasJToken(string viewId, string tokenId)
	{
		bool result = false;
		string filePath = GetFilePath(viewId);
		JToken jToken = new JsonMerger(logger, contentLoader, cacheService).Load(filePath, _type)?[_type];
		if (jToken != null && jToken.FirstOrDefault((JToken x) => x["id"].Value<string>() == tokenId) != null)
		{
			result = true;
		}
		return result;
	}

	private string GetFilePath(string viewid)
	{
		string result = string.Empty;
		string path = Path.Combine(rootPath, viewid);
		if (contentLoader.UseDbNotFileSystem() || File.Exists(Path.Combine(path, _type + ".jsonc")))
		{
			result = Path.Combine(path, _type + ".jsonc");
		}
		return result;
	}

	protected void ResetReusableComponentRef(JObject layout)
	{
		foreach (JObject item in from j in layout.Properties().Descendants().OfType<JObject>()
			where (string?)j["sourceTypeId"] == "metafx-udc"
			select j)
		{
			UpdateReusableComponentRef(item);
		}
	}

	private void UpdateReusableComponentRef(JObject obj)
	{
		if (obj == null || string.IsNullOrEmpty((string?)obj["model"]["appDesignerUdcType"]))
		{
			return;
		}
		string text = obj["model"]["appDesignerUdcType"].ToString();
		string[] separator = new string[1] { "/layout.jsonc#" };
		string text2 = text.Split(separator, StringSplitOptions.RemoveEmptyEntries)[0].Split(new char[1] { '/' }).Last();
		string text3 = "/shared/components/" + text2 + "/properties.jsonc";
		string path = rootPath + text3;
		if (!File.Exists(path))
		{
			return;
		}
		JObject jObject = JObject.Parse(File.ReadAllText(path));
		if (jObject == null)
		{
			return;
		}
		List<ComponentProperties> list = new List<ComponentProperties>();
		List<JToken> list2 = jObject["properties"].ToList();
		List<JObject> list3 = (from j in obj.Properties().Descendants().OfType<JObject>()
			where (string?)j["referencedControlId"] != null
			select j).ToList();
		foreach (JObject item4 in list3)
		{
			control = item4["referencedControlId"].ToString();
			ComponentProperties componentProperties = new ComponentProperties
			{
				ControlId = control,
				Data = new List<Epicor.MetaFX.Core.Models.ReusableComponents.Properties>()
			};
			if (!list.Exists((ComponentProperties resp) => resp.ControlId == control))
			{
				list.Add(componentProperties);
				Epicor.MetaFX.Core.Models.ReusableComponents.Properties item = new Epicor.MetaFX.Core.Models.ReusableComponents.Properties
				{
					Key = "id",
					Value = item4["id"]
				};
				componentProperties.Data.Add(item);
				Epicor.MetaFX.Core.Models.ReusableComponents.Properties item2 = new Epicor.MetaFX.Core.Models.ReusableComponents.Properties
				{
					Key = "guid",
					Value = item4["guid"]
				};
				componentProperties.Data.Add(item2);
			}
		}
		foreach (JToken exposedProperty in list2)
		{
			foreach (JObject item5 in list3.FindAll((JObject udc) => (string?)udc["referencedControlId"] == exposedProperty["controlId"].ToString()))
			{
				if (item5 != null && item5["referencedControlId"] != null)
				{
					control = item5?["referencedControlId"]?.ToString();
					ComponentProperties componentProperties2 = list.FirstOrDefault((ComponentProperties x) => x.ControlId == control);
					if (componentProperties2 != null)
					{
						Epicor.MetaFX.Core.Models.ReusableComponents.Properties item3 = new Epicor.MetaFX.Core.Models.ReusableComponents.Properties
						{
							Key = exposedProperty["controlPropertyName"].ToString(),
							Value = (item5.ContainsKey(exposedProperty["controlPropertyName"].ToString()) ? item5[exposedProperty["controlPropertyName"].ToString()] : ((JToken)string.Empty))
						};
						componentProperties2.Data.Add(item3);
					}
				}
			}
		}
		JObject obj2 = obj["model"] as JObject;
		obj2.Property("components").Remove();
		obj2.Add("$ref", text);
		if (string.IsNullOrEmpty(obj["model"]?["properties"]?.ToString()))
		{
			obj["model"]["properties"] = JArray.FromObject(list);
		}
	}
}
