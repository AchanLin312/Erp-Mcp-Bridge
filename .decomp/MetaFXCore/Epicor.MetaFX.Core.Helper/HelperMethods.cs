using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Applications;
using Epicor.MetaFX.Core.Models.Layers;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Helper;

public class HelperMethods
{
	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	public HelperMethods(ILogger logger, IContentLoader contentLoader)
	{
		this.logger = logger;
		this.contentLoader = contentLoader;
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
	}

	public bool HasImports(ref JObject json)
	{
		JToken jToken = json["imports"];
		if (jToken != null)
		{
			return jToken.ToString() != "[]";
		}
		return false;
	}

	public string CalculateHash(string input)
	{
		using SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
		byte[] bytes = Encoding.ASCII.GetBytes(input);
		sHA1CryptoServiceProvider.ComputeHash(bytes);
		return BitConverter.ToString(sHA1CryptoServiceProvider.Hash).Replace("-", "").ToLower();
	}

	public string resolveJsonPointer(JToken obj, string currentDocumentPath, out string pointerDocumentPath)
	{
		if (obj["$ref"] == null)
		{
			throw new ArgumentException("$ref not defined as a property of the obj");
		}
		string text = obj["$ref"].ToString();
		logger.Debug("Processing Pointer " + text);
		pointerDocumentPath = currentDocumentPath;
		int num = text.IndexOf("#", StringComparison.Ordinal);
		if (num > 0)
		{
			if (text.IndexOf('.') > -1)
			{
				pointerDocumentPath = text.Substring(0, num);
				string directoryName = Path.GetDirectoryName(currentDocumentPath);
				logger.Debug("Current Document : " + pointerDocumentPath + " Base Path : " + directoryName);
				pointerDocumentPath = Path.Combine(directoryName ?? throw new InvalidOperationException(), pointerDocumentPath);
				string obj2 = pointerDocumentPath;
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				if (obj2.EndsWith(Path.Combine(directorySeparatorChar + "pages", "patch.jsonc")))
				{
					string obj3 = pointerDocumentPath;
					directorySeparatorChar = Path.DirectorySeparatorChar;
					pointerDocumentPath = obj3.Replace(directorySeparatorChar + "pages", string.Empty);
				}
				if (!contentLoader.Exists(pointerDocumentPath))
				{
					throw new ArgumentException(currentDocumentPath + " contains an invalid json Reference reference " + pointerDocumentPath);
				}
			}
			return contentLoader.ReadAllText(pointerDocumentPath);
		}
		return "{}";
	}

	public string GetFileName(string folderPath, string file)
	{
		if (contentLoader.Exists(Path.Combine(folderPath, file + ".jsonc")))
		{
			return file + ".jsonc";
		}
		return file + ".json";
	}

	public string GetFilePath(string folderPath, string type)
	{
		return Path.Combine(folderPath, GetFileName(folderPath, type));
	}

	public void CreateJsonFile(dynamic data, string path, string fileName)
	{
		string path2 = Path.Combine(path, fileName);
		StringBuilder stringBuilder = new StringBuilder();
		JsonSerializer jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			Formatting = Formatting.Indented
		});
		using (StringWriter stringWriter = new StringWriter(stringBuilder))
		{
			jsonSerializer.Serialize(stringWriter, data);
		}
		contentLoader.WriteAllText(path2, stringBuilder.ToString());
	}

	public void Validate<T>(T obj)
	{
		ValidationContext validationContext = new ValidationContext(obj);
		List<ValidationResult> list = new List<ValidationResult>();
		if (Validator.TryValidateObject(obj, validationContext, list, validateAllProperties: true))
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ValidationResult item in list)
		{
			stringBuilder.AppendLine(item.ErrorMessage);
		}
		throw new ValidationException(stringBuilder.ToString());
	}

	public void EmptyDirectory(string directoryPath)
	{
		contentLoader.EmptyDirectory(directoryPath);
	}

	public void CheckDuplicateId(JToken content, string path)
	{
		if (content == null || !content.Any() || !(content is JArray) || content[0]["id"] == null)
		{
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (JToken item in (IEnumerable<JToken>)content)
		{
			if (item["id"] != null)
			{
				string text = item["id"].ToString();
				if (list.Contains(text))
				{
					list2.Add(path + " has duplicate id " + text);
				}
				list.Add(text);
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		throw new DataMisalignedException(string.Join("\n", list2));
	}

	public void CheckDuplicateId(ref List<string> ids, JToken content, string path, ref List<string> duplicateIds)
	{
		if (content == null || !content.Any() || !(content is JArray) || content[0]["id"] == null)
		{
			return;
		}
		foreach (JToken item in (IEnumerable<JToken>)content)
		{
			AddIds(ref ids, item, path, ref duplicateIds);
		}
	}

	public (bool shouldUpdateInDB, List<PageInfo> upgradedContent) CheckAndUpgradeLayer(Dictionary<string, JToken> content, string layerName)
	{
		bool item = false;
		List<PageInfo> list = new List<PageInfo>();
		if (content != null)
		{
			foreach (KeyValuePair<string, JToken> item2 in content.Where((KeyValuePair<string, JToken> kvp) => kvp.Key != "config"))
			{
				PageInfo pageInfo = JsonConvert.DeserializeObject<PageInfo>(item2.Value.ToString());
				string text = pageInfo.Version ?? string.Empty;
				string text2 = ((item2.Key == "rules" || item2.Key == "events" || item2.Key == "tools" || item2.Key == "dataviews") ? item2.Key : "layout");
				if (string.IsNullOrEmpty(text))
				{
					text = item2.Key switch
					{
						"layout" => "1.5", 
						"rules" => "1.2", 
						"events" => "1.2", 
						"tools" => "1.2", 
						"dataviews" => "1.1", 
						_ => "1.5", 
					};
				}
				pageInfo.Version = text;
				if (text2 == "layout" && text != "3.0")
				{
					if (text == "1.5")
					{
						pageInfo.Diffs = FixData(pageInfo.Name, pageInfo.Diffs);
					}
					(bool, List<JsonDiffInfo>) tuple = UpgradeLayer(pageInfo.Diffs, layerName, text, "3.0", text2);
					if (tuple.Item1)
					{
						item = true;
						pageInfo.Version = "3.0";
						pageInfo.Diffs = new List<JsonDiffInfo>(tuple.Item2);
					}
				}
				else if (text2 == "rules" && text != "3.0")
				{
					(bool, List<JsonDiffInfo>) tuple2 = UpgradeLayer(pageInfo.Diffs, layerName, text, "3.0", text2);
					if (tuple2.Item1)
					{
						item = true;
						pageInfo.Version = "3.0";
						pageInfo.Diffs = new List<JsonDiffInfo>(tuple2.Item2);
					}
				}
				else if (text2 == "events" && text != "5.0")
				{
					(bool, List<JsonDiffInfo>) tuple3 = UpgradeLayer(pageInfo.Diffs, layerName, text, "5.0", text2);
					if (tuple3.Item1)
					{
						item = true;
						pageInfo.Version = "5.0";
						pageInfo.Diffs = new List<JsonDiffInfo>(tuple3.Item2);
					}
				}
				else if (text2 == "tools" && text != "2.0")
				{
					(bool, List<JsonDiffInfo>) tuple4 = UpgradeLayer(pageInfo.Diffs, layerName, text, "2.0", text2);
					if (tuple4.Item1)
					{
						item = true;
						pageInfo.Version = "2.0";
						pageInfo.Diffs = new List<JsonDiffInfo>(tuple4.Item2);
					}
				}
				else if (text2 == "dataviews" && text != "4.0")
				{
					(bool, List<JsonDiffInfo>) tuple5 = UpgradeLayer(pageInfo.Diffs, layerName, text, "4.0", text2);
					if (tuple5.Item1)
					{
						item = true;
						pageInfo.Version = "4.0";
						pageInfo.Diffs = new List<JsonDiffInfo>(tuple5.Item2);
					}
				}
				list.Add(pageInfo);
			}
		}
		return (shouldUpdateInDB: item, upgradedContent: list);
	}

	private (bool isUpgraded, List<JsonDiffInfo> upgradedLayerContent) UpgradeLayer(List<JsonDiffInfo> diffs, string layerName, string currentSchemaVersion, string latestSchemaVersion, string _type)
	{
		bool flag = false;
		List<JsonDiffInfo> list = new List<JsonDiffInfo>();
		if (diffs != null)
		{
			bool flag2 = false;
			try
			{
				foreach (JsonDiffInfo diff in diffs)
				{
					bool flag3 = true;
					JObject jObject = new JObject
					{
						{ "$schema", "" },
						{ "contentVersion", currentSchemaVersion }
					};
					if (string.IsNullOrEmpty(diff.Value.ToString()))
					{
						if (_type == "layout")
						{
							JObject value = new JObject { ["components"] = new JArray() };
							jObject.Add(_type, value);
						}
						else
						{
							jObject.Add(_type, new JArray());
						}
					}
					else if (_type == "layout")
					{
						if (diff.Value.Type != JTokenType.String && diff.Value.Type != JTokenType.Integer)
						{
							JObject jObject2 = JObject.Parse(diff.Value.ToString());
							if (jObject2["components"] != null && jObject2["components"].HasValues)
							{
								jObject.Add(_type, jObject2);
								flag2 = true;
							}
							else
							{
								JObject value2 = new JObject { ["components"] = new JArray(jObject2) };
								jObject.Add(_type, value2);
							}
						}
						else
						{
							flag3 = false;
						}
					}
					else
					{
						JObject item = JObject.Parse(diff.Value.ToString());
						JArray value3 = new JArray { item };
						jObject.Add(_type, value3);
					}
					while ((jObject["contentVersion"].ToString() != latestSchemaVersion) & flag3)
					{
						jObject = schemaUpgrade.Upgrade(jObject, _type);
						logger.Debug("Upgrading schema " + _type + " of layer " + layerName + ". Diff " + diff.Id);
						flag = true;
					}
					if (flag)
					{
						if (_type == "layout")
						{
							if (flag2)
							{
								diff.Value = JObject.Parse(jObject[_type].ToString());
							}
							else if (jObject[_type]["components"].HasValues)
							{
								diff.Value = JObject.Parse(jObject[_type]["components"][0].ToString());
							}
						}
						else
						{
							diff.Value = JObject.Parse(jObject[_type][0].ToString());
						}
					}
					list.Add(diff);
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Unable to upgrade layer " + layerName, innerException);
			}
		}
		return (isUpgraded: flag, upgradedLayerContent: list);
	}

	private List<JsonDiffInfo> FixData(string name, List<JsonDiffInfo> diffs)
	{
		List<JsonDiffInfo> list = new List<JsonDiffInfo>();
		if (diffs == null)
		{
			return list;
		}
		foreach (JsonDiffInfo diff in diffs)
		{
			JsonDiffInfo jsonDiffInfo = diff;
			if (jsonDiffInfo.Value.Type == JTokenType.Object)
			{
				if (name == "Main" && jsonDiffInfo.Value["sourceTypeId"] != null && jsonDiffInfo.Value["sourceTypeId"].ToString() == "metafx-tab")
				{
					continue;
				}
				if (jsonDiffInfo.Value["targetTypeId"] != null && jsonDiffInfo.Value["targetTypeId"].ToString() == "metafx-panel-card-grid")
				{
					if (jsonDiffInfo.Value["model"]["expandedHeaderComponents"] != null)
					{
						foreach (JToken item3 in jsonDiffInfo.Value["model"]["expandedHeaderComponents"] as JArray)
						{
							item3["parentId"] = string.Format("{0}#expandedHeaderComponents", item3["parentId"]);
							JsonDiffInfo item = new JsonDiffInfo
							{
								Id = item3["id"].ToString(),
								Index = diffs.Count + 1,
								Operation = diff.Operation,
								Path = "components",
								Value = item3
							};
							list.Add(item);
						}
						jsonDiffInfo.Value["model"]["expandedHeaderComponents"] = new JArray();
					}
					list.Add(jsonDiffInfo);
				}
				else if (jsonDiffInfo.Value["targetTypeId"] != null && jsonDiffInfo.Value["targetTypeId"].ToString() == "metafx-panel-card")
				{
					if (jsonDiffInfo.Value["model"]["expandedHeaderComponents"] != null)
					{
						foreach (JToken item4 in jsonDiffInfo.Value["model"]["expandedHeaderComponents"] as JArray)
						{
							item4["parentId"] = string.Format("{0}#expandedHeaderComponents", item4["parentId"]);
							JsonDiffInfo item2 = new JsonDiffInfo
							{
								Id = item4["id"].ToString(),
								Index = diffs.Count + 1,
								Operation = diff.Operation,
								Path = "components",
								Value = item4
							};
							list.Add(item2);
						}
						jsonDiffInfo.Value["model"]["expandedHeaderComponents"] = new JArray();
					}
					list.Add(jsonDiffInfo);
				}
				else
				{
					list.Add(jsonDiffInfo);
				}
			}
			else
			{
				list.Add(jsonDiffInfo);
			}
		}
		return list;
	}

	private void AddIds(ref List<string> ids, JToken content, string path, ref List<string> duplicateIds, bool isPanelCard = false)
	{
		foreach (JProperty item in (IEnumerable<JToken>)content)
		{
			if (item.Name == "id" && item.Value != null && !isPanelCard)
			{
				string text = item.Value.ToString();
				if (ids.Contains(text))
				{
					duplicateIds.Add(path + " has duplicate id " + text);
				}
				if (!string.IsNullOrEmpty(text))
				{
					ids.Add(text);
				}
			}
			else if (item.Value is JObject && IsPanelCard(content))
			{
				AddIds(ref ids, item.Value, path, ref duplicateIds, isPanelCard: true);
			}
			else
			{
				if (!(item.Value is JArray) || !(item.Name != "actionData"))
				{
					continue;
				}
				foreach (JToken item2 in (IEnumerable<JToken>)item.Value)
				{
					AddIds(ref ids, item2, path, ref duplicateIds);
				}
			}
		}
	}

	private bool IsPanelCard(JToken content)
	{
		List<string> list = new List<string> { "metafx-panel-card", "metafx-panel-card-grid" };
		if (!list.Contains(content["sourceTypeId"]?.ToString()))
		{
			return list.Contains(content["targetTypeId"]?.ToString());
		}
		return true;
	}

	public Dictionary<string, PageInfo> GetUpdatedContent(List<PageInfo> pages)
	{
		Dictionary<string, PageInfo> dictionary = new Dictionary<string, PageInfo>();
		foreach (PageInfo page in pages)
		{
			dictionary.Add(page.Name, page);
		}
		return dictionary;
	}

	public UserDefinedApplicationInfo GetUserDefinedAppInfo(UserDefinedApplicationInfo[] applicationDetails, string folder)
	{
		if (applicationDetails == null || !applicationDetails.Any())
		{
			return GetDefaultUdAppInfo(folder);
		}
		UserDefinedApplicationInfo userDefinedApplicationInfo = applicationDetails.Where((UserDefinedApplicationInfo x) => x.Key1 == folder).FirstOrDefault();
		if (userDefinedApplicationInfo == null)
		{
			return GetDefaultUdAppInfo(folder);
		}
		userDefinedApplicationInfo.SystemFlag = false;
		return userDefinedApplicationInfo;
	}

	public UserDefinedApplicationInfo GetDefaultUdAppInfo(string folder)
	{
		return new UserDefinedApplicationInfo
		{
			Key1 = folder,
			SystemFlag = true,
			LastUpdatedBy = string.Empty,
			SysCharacter01 = string.Empty,
			SysCharacter02 = string.Empty
		};
	}

	public string CustomProgramFromKey(string keyName)
	{
		string text = "App";
		string text2 = "UI";
		string text3 = "";
		string text4 = "";
		switch (keyName)
		{
		case "App.AlertMaintEntry.AlertMaintForm":
			text3 = "Erp.UI.AlertMaint.dll";
			break;
		case "App.CustGrup.CustGrupForm":
			text3 = "Erp.UI.CustGrupEntry.dll";
			break;
		case "App.CustomReportEntry.CustomReportForm":
			text3 = "Erp.UI.CustomReport.dll";
			break;
		case "App.JobStatusMaintenanceEntryEntry.JobStatusMaintenanceForm":
			text3 = "Erp.UI.JobStatusMaintenance.dll";
			break;
		case "App.JrnlCdEntry.JrnlCodeForm":
			text3 = "Erp.UI.JrnlCodeEntry.dll";
			break;
		case "App.MktgAdvertismentEntry.MktgAdvertismentForm":
			text3 = "Erp.UI.MktgAdvEntry.dll";
			break;
		case "App.MultiResourceSchedBoardEntry.ResourceSchedForm":
			text3 = "Erp.UI.MultiResourceSchedulingBoard.dll";
			break;
		case "App.OverrideSchedOrdersEntry.OverrideSchedOrdersForm":
			text3 = "Erp.UI.OverrideSchedOrders.dll";
			break;
		case "App.ResourceGroup.ResourceGroupForm":
			text3 = "Erp.UI.ResourceGroupEntry.dll";
			break;
		case "App.PriceListEntry.PriceListForm":
			text3 = "Erp.UI.PriceLstEntry.dll";
			break;
		case "Rpt.LaborEditEntry.LaborEditForm":
			text3 = "Erp.UIRpt.LaborEdit.dll";
			break;
		case "App.ClockIn.EmployeeSelectForm":
			text3 = "Erp.UI.ClockIn.dll";
			break;
		case "App.CompanyEntry.CompanyEntryForm":
			text3 = "Erp.UI.CompanyConfiguration.dll";
			break;
		case "App.CompanyMaintenance.CompanyMaintenanceForm":
			text3 = "Ice.UI.CompanyMaintenance.dll";
			break;
		case "App.PlantEntry.PlantForm":
			text3 = "Erp.UI.PlantEntry.dll";
			break;
		}
		string[] array = keyName.Split(new char[1] { '.' });
		if (text3.Length == 0 && array.Length >= 3)
		{
			text = array[0];
			text4 = array[1] + ".dll";
			switch (text)
			{
			case "App":
				text2 = ((!(array[2] == "MainController")) ? "UI" : "UI.App");
				break;
			case "Rpt":
				text2 = "UIRpt";
				break;
			case "Proc":
				text2 = "UIProc";
				break;
			}
			text3 = text2 + "." + text4;
		}
		if (keyName.StartsWith("BAQReport", StringComparison.OrdinalIgnoreCase))
		{
			text3 = "Ice.UIRpt.BAQReport.dll";
		}
		if (keyName.StartsWith("Menu.Mes.MESMenu", StringComparison.OrdinalIgnoreCase))
		{
			text3 = "Erp.Menu.Mes.dll";
		}
		return text3;
	}

	public bool ValidateCompanyID(string companyID, string sessionCompanyId, bool GlobalSecurityManager, bool IsMultiTenant, string TenantID, bool IsCurrentUserCompany)
	{
		if (companyID.Equals(sessionCompanyId, StringComparison.Ordinal))
		{
			return true;
		}
		if (string.IsNullOrEmpty(companyID) && (GlobalSecurityManager || !IsMultiTenant))
		{
			return true;
		}
		if (companyID.Equals("T_" + TenantID) || (GlobalSecurityManager & IsMultiTenant))
		{
			return true;
		}
		if (string.IsNullOrEmpty(companyID) && (GlobalSecurityManager || !IsMultiTenant))
		{
			return true;
		}
		if (IsCurrentUserCompany)
		{
			return true;
		}
		return false;
	}
}
