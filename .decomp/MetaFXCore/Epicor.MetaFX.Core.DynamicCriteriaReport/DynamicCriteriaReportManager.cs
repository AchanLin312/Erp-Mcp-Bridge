using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.DynamicCriteriaReport;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Epicor.MetaFX.Core.Models.MetaUI.Patch;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;
using Epicor.MetaFX.Core.Models.MetaUI.Strings;
using Epicor.MetaFX.Core.Models.MetaUI.Tools;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.DynamicCriteriaReport;

public class DynamicCriteriaReportManager
{
	private readonly HelperMethods helperMethods;

	private readonly LayoutHelper layoutHelper;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly DynamicCriteriaReportHelper reportHelper;

	private readonly string rootPath;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	private readonly Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager;

	private const string IMPORT_PATH = "../shared/imports/UIReports/dynamicCriteriaReport";

	public DynamicCriteriaReportManager(string rootPath, ILogger logger, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		helperMethods = new HelperMethods(logger, contentLoader);
		templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
		reportHelper = new DynamicCriteriaReportHelper();
		layoutHelper = new LayoutHelper(logger);
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
	}

	public EpMetaFxResponse<ReportResponse> Generate(DynamicCriteriaReportRequest request, Func<string> currentCultureName)
	{
		Epicor.MetaFX.Core.Models.DynamicCriteriaReport.ReportDefinition reportDefinition = request.ReportDefinition;
		List<ReportCriteriaDefinition> reportCriteriaDefinitions = request.ReportCriteriaDefinitions;
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		if (reportCriteriaDefinitions == null || !reportCriteriaDefinitions.Any() || reportDefinition?.ReportStyle == null || (reportDefinition != null && reportDefinition.ReportStyle.Count == 0))
		{
			logger.Debug("Invalid Report definition.");
			epMetaFxResponse.Message = "Invalid Report definition.";
			return epMetaFxResponse;
		}
		string reportID = reportDefinition.ReportStyle[0].ReportID;
		epMetaFxResponse.Data = new ReportResponse
		{
			Id = reportID
		};
		logger.Debug("Generate Dynamic Criteria Report " + reportID + ".");
		string text = (request.SystemCode.Equals("ERP", StringComparison.OrdinalIgnoreCase) ? "Erp" : "Ice") + ".UIDynRpt." + reportID;
		string text2 = rootPath + "/" + text;
		helperMethods.EmptyDirectory(text2);
		try
		{
			object commonTemplate = templateManager.GetCommonTemplate(new EpMetaFxTemplateRequest
			{
				ViewType = "DynamicCriteriaReport"
			}, currentCultureName);
			GenerateLayout(text2, request, commonTemplate);
			GenerateDataview(text2);
			GenerateRules(text2, request);
			GenerateTools(text2);
			GenerateStrings(text2);
			List<MessageDetail> collection = GenerateEvents(request, text2);
			epMetaFxResponse.Details.AddRange(collection);
			GeneratePatch(request, text2);
			if (epMetaFxResponse.Details.Any((MessageDetail r) => r.Type == "error"))
			{
				epMetaFxResponse.Message = "Report generation failed.";
				logger.Debug("Report " + reportID + " generation failed.");
			}
			else
			{
				epMetaFxResponse.IsSuccess = true;
				epMetaFxResponse.Message = "Report generated successfully.";
				epMetaFxResponse.Data.FileName = text;
				logger.Debug("Report " + reportID + " generated successfully.");
			}
		}
		finally
		{
			if (!epMetaFxResponse.IsSuccess && Directory.Exists(text2))
			{
				Directory.Delete(text2, recursive: true);
			}
		}
		return epMetaFxResponse;
	}

	private void GenerateLayout(string path, DynamicCriteriaReportRequest request, object layoutTemplate)
	{
		JToken jToken = JToken.FromObject(layoutTemplate);
		JToken jToken2 = jToken["layout"];
		JsonSerializer jsonSerializer = new JsonSerializer();
		jsonSerializer.Converters.Add(new ModelConverter());
		List<Component> list = jToken2["components"].ToObject<List<Component>>(jsonSerializer);
		List<Component> criteriaPanels = new List<Component>();
		if (list == null)
		{
			logger.Debug("Components is missing from layout template layout.");
			return;
		}
		jToken2["name"] = request.ReportDefinition.DynamicCriteriaParam[0].ReportID;
		jToken2["caption"] = request.ReportDefinition.DynamicCriteriaParam[0].ReportTitle;
		request.ReportCriteriaDefinitions.ForEach(delegate(ReportCriteriaDefinition def)
		{
			string rptDefId = def.RptDataDef[0].RptDefID;
			def.RptCriteriaSet.ForEach(delegate(RptCriteriaSet criteria)
			{
				List<RptCriteriaPrompt> list2 = def.RptCriteriaPrompt.Where((RptCriteriaPrompt c) => c.IsVisible && reportHelper.IsEqual(c.RptCriteriaSetID, criteria.RptCriteriaSetID)).ToList();
				if (list2.Any())
				{
					List<Component> collection = BuildOptionalControls(list2);
					string uniqueName = reportHelper.RemoveWhitespace("pnlOption" + rptDefId + "_" + criteria.RptCriteriaSetID);
					Component component = layoutHelper.CreatePanelCard("Options", uniqueName, expandPanelBar: true);
					MetaFxPanelCard metaFxPanelCard = (MetaFxPanelCard)component.Model;
					metaFxPanelCard.EpBinding = reportHelper.RemoveWhitespace("ReportParamStyles." + rptDefId + "_" + criteria.RptCriteriaSetID);
					if (metaFxPanelCard.BodyComponents == null)
					{
						metaFxPanelCard.BodyComponents = new List<Component>();
					}
					metaFxPanelCard.BodyComponents.AddRange(collection);
					criteriaPanels.Add(component);
				}
				List<RptCriteriaFilter> list3 = def.RptCriteriaFilter.Where((RptCriteriaFilter c) => reportHelper.IsEqual(c.RptCriteriaSetID, criteria.RptCriteriaSetID)).ToList();
				if (list3.Any())
				{
					List<Component> collection2 = BuildFilterControls(list3);
					string uniqueName2 = reportHelper.RemoveWhitespace("pnlFilter" + rptDefId + "_" + criteria.RptCriteriaSetID);
					Component component2 = layoutHelper.CreatePanelCard("Filters", uniqueName2, expandPanelBar: false);
					MetaFxPanelCard metaFxPanelCard2 = (MetaFxPanelCard)component2.Model;
					metaFxPanelCard2.EpBinding = reportHelper.RemoveWhitespace("ReportParamStyles." + rptDefId + "_" + criteria.RptCriteriaSetID);
					if (metaFxPanelCard2.BodyComponents == null)
					{
						metaFxPanelCard2.BodyComponents = new List<Component>();
					}
					metaFxPanelCard2.BodyComponents.AddRange(collection2);
					criteriaPanels.Add(component2);
				}
			});
		});
		foreach (Component item in list)
		{
			string id = item.Id;
			if (!(id == "Submission"))
			{
				if (id == "Advanced")
				{
					criteriaPanels.Insert(criteriaPanels.Count, item);
				}
			}
			else
			{
				criteriaPanels.Insert(0, item);
			}
		}
		jToken2["components"] = layoutHelper.ConvertToJArray(criteriaPanels);
		FlattenLayout flattenLayout = new FlattenLayout();
		jToken["layout"] = flattenLayout.ConvertToFlattenLayout(jToken["layout"] as JObject);
		jToken = schemaUpgrade.UpgradeToLatestSchema(jToken as JObject, "layout");
		helperMethods.CreateJsonFile(jToken, path, "layout.jsonc");
	}

	private void GenerateDataview(string path)
	{
		DataViewSchema dataViewSchema = new DataViewSchema();
		dataViewSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1.0/dataviews.json";
		dataViewSchema.ContentVersion = "1.0";
		dataViewSchema.Imports = new object[1]
		{
			new
			{
				dataviews = new Reference
				{
					Refs = "../shared/imports/UIReports/dynamicCriteriaReport/dataviews.jsonc#/dataviews"
				}
			}
		};
		dataViewSchema.DataViews = new List<DataView>();
		DataViewSchema o = dataViewSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "dataviews");
		helperMethods.CreateJsonFile(data, path, "dataviews.jsonc");
	}

	private void GenerateRules(string path, DynamicCriteriaReportRequest request)
	{
		RulesSchema rulesSchema = new RulesSchema();
		rulesSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1/rules.json";
		rulesSchema.ContentVersion = "1.0";
		rulesSchema.Imports = new object[1]
		{
			new
			{
				rules = new Reference
				{
					Refs = "../shared/imports/UIReports/dynamicCriteriaReport/rules.jsonc#/rules"
				}
			}
		};
		RulesSchema rulesSchema2 = rulesSchema;
		if (request.ReportCriteriaDefinitions.Count() > 1 || request.ReportCriteriaDefinitions[0].RptCriteriaSet.Count > 1)
		{
			List<RowRuleBase> list = reportHelper.CreateHidePanelsRule(request);
			if (list != null)
			{
				rulesSchema2.RowRules.AddRange(list);
			}
		}
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(rulesSchema2), "rules");
		helperMethods.CreateJsonFile(data, path, "rules.jsonc");
	}

	private void GenerateTools(string path)
	{
		ToolsSchema toolsSchema = new ToolsSchema();
		toolsSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1.0/tools.json";
		toolsSchema.ContentVersion = "1.0";
		toolsSchema.Imports = new object[1]
		{
			new
			{
				tools = new Reference
				{
					Refs = "../shared/imports/UIReports/dynamicCriteriaReport/tools.jsonc#/tools"
				}
			}
		};
		ToolsSchema o = toolsSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "tools");
		helperMethods.CreateJsonFile(data, path, "tools.jsonc");
	}

	private void GenerateStrings(string path)
	{
		StringsSchema stringsSchema = new StringsSchema();
		stringsSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1/strings.json";
		stringsSchema.ContentVersion = "1.0.0";
		stringsSchema.Imports = new object[1]
		{
			new
			{
				@default = new Reference
				{
					Refs = "../shared/imports/UIReports/dynamicCriteriaReport/strings.jsonc#/strings"
				}
			}
		};
		StringsSchema o = stringsSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "strings");
		helperMethods.CreateJsonFile(data, path, "strings.jsonc");
	}

	private List<MessageDetail> GenerateEvents(DynamicCriteriaReportRequest request, string path)
	{
		List<MessageDetail> messages = new List<MessageDetail>();
		EventSchema eventSchema = new EventSchema
		{
			Schema = "https://schemas.developer.epicor.com/schemas/v1.1/events.json",
			ContentVersion = "2.0",
			Imports = new object[1]
			{
				new
				{
					events = new Reference
					{
						Refs = "../shared/imports/UIReports/dynamicCriteriaReport/events.jsonc#/events"
					}
				}
			}
		};
		Event obj = reportHelper.CreateInitializeEvent(request);
		if (obj != null)
		{
			eventSchema.Events.Add(obj);
		}
		request.ReportCriteriaDefinitions.ForEach(delegate(ReportCriteriaDefinition def)
		{
			List<RptCriteriaFilterAdapterXRef> rptCriteriaFilterAdapterXRef = def.RptCriteriaFilterAdapterXRef;
			foreach (RptCriteriaFilter item in def.RptCriteriaFilter)
			{
				Tuple<Event, MessageDetail> tuple = reportHelper.CreateSearchFilterEvent(item, rootPath, item.LookupField, rptCriteriaFilterAdapterXRef);
				eventSchema.Events.Add(tuple.Item1);
				if (tuple.Item2 != null)
				{
					messages.Add(tuple.Item2);
				}
			}
		});
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events");
		helperMethods.CreateJsonFile(data, path, "events.jsonc");
		return messages;
	}

	private void GeneratePatch(DynamicCriteriaReportRequest request, string path)
	{
		PatchSchema patchSchema = new PatchSchema();
		patchSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/patch.json";
		patchSchema.ContentVersion = "2.0";
		patchSchema.Imports = new object[0];
		patchSchema.Patch = CreatePatch(request.SystemCode, request.ReportDefinition.ReportStyle[0].ReportID, request.ReportCriteriaDefinitions[0].RptDataDef[0].RptDefID);
		PatchSchema o = patchSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "patch");
		helperMethods.CreateJsonFile(data, path, "patch.jsonc");
	}

	private object CreatePatch(string systemCode, string reportId, string rptDefId)
	{
		string text = (systemCode.Equals("ERP", StringComparison.OrdinalIgnoreCase) ? "Erp" : "Ice");
		return new object[6]
		{
			new
			{
				id = "svc",
				value = "Ice.RPT.DynamicCriteriaSvc"
			},
			new
			{
				id = "param",
				value = "DynamicCriteriaParam"
			},
			new
			{
				id = "paramModel",
				value = "ReportParam"
			},
			new
			{
				id = "maintProgram",
				value = text + ".UIDynRpt." + reportId
			},
			new
			{
				id = "reportId",
				value = reportId
			},
			new
			{
				id = "reportDataDefId",
				value = rptDefId
			}
		};
	}

	private List<Component> BuildOptionalControls(List<RptCriteriaPrompt> rptCriteriaPrompts)
	{
		List<Component> list = new List<Component>();
		foreach (RptCriteriaPrompt item in rptCriteriaPrompts.OrderBy((RptCriteriaPrompt c) => c.RptCriteriaSetID).ThenBy((RptCriteriaPrompt o) => o.DisplayOrder))
		{
			string text = reportHelper.BuildPromptControlName(item);
			string text2 = reportHelper.BuildEpBindingName(item.PromptName);
			string typeStringFromSqlType = reportHelper.GetTypeStringFromSqlType(item.DataType);
			if (reportHelper.IsEqual(item.EditorType, "DropDown"))
			{
				list.Add(reportHelper.CreateComboBoxControl(item));
				continue;
			}
			switch (typeStringFromSqlType.ToLower())
			{
			case "datetime":
			{
				string epBindingToken = text2 + "Token";
				list.Add(layoutHelper.CreateDateControl("epiDate" + text, item.Label, text2, item.DisplayOrder, epBindingToken));
				break;
			}
			case "string":
				list.Add(layoutHelper.CreateTextBoxControl("epiTextBox" + text, item.Label, text2, item.DisplayOrder));
				break;
			case "int":
			case "int64":
			case "decimal":
			{
				string format = (reportHelper.IsEqual(typeStringFromSqlType, "decimal") ? "-nnnnnnnnnn.nnnnn" : "-nnnnnnnnnnnnnnnn");
				list.Add(layoutHelper.CreateNumericControl("epiNumeric" + text, item.Label, text2, item.DisplayOrder, nullable: false, isReadOnly: false, format));
				break;
			}
			case "bool":
				list.Add(layoutHelper.CreateCheckBoxControl("epiCheckBox" + text, item.Label, text2, item.DisplayOrder));
				break;
			}
		}
		return list;
	}

	private List<Component> BuildFilterControls(List<RptCriteriaFilter> rptCriteriaFilter)
	{
		List<Component> list = new List<Component>();
		foreach (RptCriteriaFilter item in rptCriteriaFilter)
		{
			list.Add(reportHelper.CreateFilterControl(item));
		}
		return list;
	}
}
