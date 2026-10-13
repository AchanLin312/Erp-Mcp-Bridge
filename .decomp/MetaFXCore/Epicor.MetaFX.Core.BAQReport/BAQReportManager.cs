using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
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

namespace Epicor.MetaFX.Core.BAQReport;

public class BAQReportManager
{
	private readonly HelperMethods helperMethods;

	private readonly LayoutHelper layoutHelper;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly ReportHelper reportHelper;

	private readonly string rootPath;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	private readonly Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager;

	private const string IMPORT_PATH = "../shared/imports/UIReports/base";

	public BAQReportManager(string rootPath, ILogger logger, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.contentLoader = contentLoader;
		helperMethods = new HelperMethods(logger, contentLoader);
		templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
		reportHelper = new ReportHelper();
		layoutHelper = new LayoutHelper(logger);
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
	}

	public EpMetaFxResponse<ReportResponse> Generate(BAQReportRequest request, Func<string> currentCultureName)
	{
		ReportDefinition reportDefinition = request.ReportDefinition;
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		if (reportDefinition?.BAQReport == null || reportDefinition.BAQReport.Count == 0)
		{
			logger.Debug("Invalid Report definition");
			epMetaFxResponse.Message = "Invalid Report definition";
			return epMetaFxResponse;
		}
		string text = "Ice.UIRpt." + reportDefinition.BAQReport[0].BAQRptID;
		epMetaFxResponse.Data = new ReportResponse
		{
			Id = reportDefinition.BAQReport[0].BAQRptID
		};
		logger.Debug("Generate BAQ report " + reportDefinition.BAQReport[0].BAQRptID + ".");
		string text2 = rootPath + "/" + text;
		helperMethods.EmptyDirectory(text2);
		try
		{
			GenerateDataview(text2);
			object commonTemplate = templateManager.GetCommonTemplate(new EpMetaFxTemplateRequest
			{
				ViewType = "BAQReport"
			}, currentCultureName);
			GenerateLayout(text2, reportDefinition, commonTemplate);
			GenerateRules(text2);
			GenerateTools(text2);
			GenerateStrings(text2);
			List<MessageDetail> collection = GenerateEvents(request, text2);
			epMetaFxResponse.Details.AddRange(collection);
			GeneratePatch(request, text2);
			if (epMetaFxResponse.Details.Any((MessageDetail r) => r.Type == "error"))
			{
				epMetaFxResponse.Message = "Report generation failed.";
				logger.Debug("Report " + reportDefinition.BAQReport[0].BAQRptID + " generation failed.");
			}
			else
			{
				epMetaFxResponse.IsSuccess = true;
				epMetaFxResponse.Message = "Report generated successfully.";
				epMetaFxResponse.Data.FileName = text;
				logger.Debug("Report " + reportDefinition.BAQReport[0].BAQRptID + " generated successfully.");
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

	private void GenerateLayout(string path, ReportDefinition reportDefinition, object layoutTemplate)
	{
		JToken jToken = JToken.FromObject(layoutTemplate);
		JToken jToken2 = jToken["layout"];
		JsonSerializer jsonSerializer = new JsonSerializer();
		jsonSerializer.Converters.Add(new ModelConverter());
		List<Component> list = jToken2["components"].ToObject<List<Component>>(jsonSerializer);
		if (list == null)
		{
			logger.Debug("Components is missing from layout template layout.");
			return;
		}
		jToken2["name"] = reportDefinition.BAQReport[0].BAQRptID;
		jToken2["caption"] = reportDefinition.BAQReport[0].FormTitle;
		List<Component> list2 = new List<Component>();
		for (int i = 0; i < list.Count; i++)
		{
			Component component = list[i];
			switch (component.Id)
			{
			case "ReportOptions":
				if (reportDefinition.BAQRptOptionFld.Any())
				{
					List<Component> collection = BuildOptionalControls(reportDefinition.BAQRptOptionFld);
					MetaFxPanelCard metaFxPanelCard = (MetaFxPanelCard)component.Model;
					if (metaFxPanelCard.BodyComponents == null)
					{
						metaFxPanelCard.BodyComponents = new List<Component>();
					}
					metaFxPanelCard.BodyComponents.AddRange(collection);
				}
				else
				{
					list2.Add(component);
				}
				break;
			case "FilterSummary":
				if (reportDefinition.BAQRptFilter.Any())
				{
					List<Component> collection2 = BuildFilterControls(reportDefinition.BAQRptFilter);
					MetaFxPanelCard metaFxPanelCard2 = (MetaFxPanelCard)component.Model;
					if (metaFxPanelCard2.BodyComponents == null)
					{
						metaFxPanelCard2.BodyComponents = new List<Component>();
					}
					metaFxPanelCard2.BodyComponents.AddRange(collection2);
				}
				else
				{
					list2.Add(component);
				}
				break;
			case "Advanced":
				if (reportDefinition.BAQRptSort.Count > 0)
				{
					Component item = reportHelper.CreateSortControl(reportDefinition.BAQRptSort, component.Id);
					((MetaFxPanelCard)component.Model).BodyComponents.Insert(0, item);
				}
				break;
			}
		}
		foreach (Component item2 in list2)
		{
			list.Remove(item2);
		}
		jToken2["components"] = layoutHelper.ConvertToJArray(list);
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
					Refs = "../shared/imports/UIReports/base/dataviews.jsonc#/dataviews"
				}
			}
		};
		dataViewSchema.DataViews = new List<DataView>
		{
			new DataView
			{
				Id = "ReportParam",
				Table = "BAQReportParam",
				Schema = "ICE"
			}
		};
		DataViewSchema o = dataViewSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "dataviews");
		helperMethods.CreateJsonFile(data, path, "dataviews.jsonc");
	}

	private void GenerateRules(string path)
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
					Refs = "../shared/imports/UIReports/base/rules.jsonc#/rules"
				}
			}
		};
		RulesSchema o = rulesSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "rules");
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
					Refs = "../shared/imports/UIReports/base/tools.jsonc#/tools"
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
					Refs = "../shared/imports/UIReports/base/strings.jsonc#/strings"
				}
			}
		};
		StringsSchema o = stringsSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "strings");
		helperMethods.CreateJsonFile(data, path, "strings.jsonc");
	}

	private List<MessageDetail> GenerateEvents(BAQReportRequest request, string path)
	{
		ReportDefinition reportDefinition = request.ReportDefinition;
		Epicor.MetaFX.Core.Models.BAQReport.BAQReport report = reportDefinition.BAQReport[0];
		List<MessageDetail> list = new List<MessageDetail>();
		EventSchema eventSchema = new EventSchema();
		eventSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1.1/events.json";
		eventSchema.ContentVersion = "2.0";
		eventSchema.Imports = new object[1]
		{
			new
			{
				events = new Reference
				{
					Refs = "../shared/imports/UIReports/base/events.jsonc#/events"
				}
			}
		};
		EventSchema eventSchema2 = eventSchema;
		eventSchema2.Events.Add(reportHelper.CreateGetNewParametersEvent(report));
		Event obj = reportHelper.CreateInitializeEvent(reportDefinition);
		if (obj != null)
		{
			eventSchema2.Events.Add(obj);
		}
		eventSchema2.Events.Add(reportHelper.CreatePrintPreviewEvent(report));
		eventSchema2.Events.Add(reportHelper.CreateGenerateOnlyEvent(report));
		eventSchema2.Events.Add(reportHelper.CreateSubmitEvent());
		foreach (BAQRptFilter filter in reportDefinition.BAQRptFilter)
		{
			QueryFieldDesigner queryFieldDesigner = request.BAQDefinition.QueryFieldDesigner.FirstOrDefault((QueryFieldDesigner qd) => qd.DBFieldName == filter.FieldName && qd.DBTableName == filter.DataTableID);
			if (queryFieldDesigner == null)
			{
				list.Add(new MessageDetail
				{
					Message = "Search " + filter.DataTableID + "." + filter.FieldName + " does not exists in BAQ.",
					Type = "warning"
				});
			}
			else
			{
				Tuple<Event, MessageDetail> tuple = reportHelper.CreateSearchFilterEvent(filter, rootPath, queryFieldDesigner);
				eventSchema2.Events.Add(tuple.Item1);
				if (tuple.Item2 != null)
				{
					list.Add(tuple.Item2);
				}
			}
		}
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(eventSchema2), "events");
		helperMethods.CreateJsonFile(data, path, "events.jsonc");
		return list;
	}

	private void GeneratePatch(BAQReportRequest request, string path)
	{
		Epicor.MetaFX.Core.Models.BAQReport.BAQReport report = request.ReportDefinition.BAQReport[0];
		PatchSchema patchSchema = new PatchSchema();
		patchSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/patch.json";
		patchSchema.ContentVersion = "2.0";
		patchSchema.Imports = new object[0];
		patchSchema.Patch = reportHelper.CreatePatch(report);
		PatchSchema o = patchSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "patch");
		helperMethods.CreateJsonFile(data, path, "patch.jsonc");
	}

	private List<Component> BuildOptionalControls(List<BAQRptOptionFld> baqRptOptionFld)
	{
		List<Component> list = new List<Component>();
		foreach (BAQRptOptionFld item in baqRptOptionFld)
		{
			string optionalFieldName = reportHelper.GetOptionalFieldName(item);
			switch (reportHelper.GetTypeStringFromSqlType(item.DataType).ToLower())
			{
			case "datetime":
			{
				string text = "ReportParam." + optionalFieldName;
				if (item.DataTableID == "ReportParam")
				{
					text += "Token";
				}
				list.Add(layoutHelper.CreateDateControl($"epiDate{item.Seq}", item.FieldLabel, "ReportParam." + optionalFieldName, item.Seq, text));
				break;
			}
			case "string":
				list.Add(layoutHelper.CreateTextBoxControl($"epiTextBox{item.Seq}", item.FieldLabel, "ReportParam." + optionalFieldName, item.Seq));
				break;
			case "int":
			case "int64":
			case "decimal":
				list.Add(layoutHelper.CreateNumericControl($"epiNumeric{item.Seq}", item.FieldLabel, "ReportParam." + optionalFieldName, item.Seq, nullable: false, isReadOnly: false, string.IsNullOrEmpty(item.FieldFormat) ? null : item.FieldFormat));
				break;
			case "bool":
				list.Add(layoutHelper.CreateCheckBoxControl($"epiCheckBox{item.Seq}", item.FieldLabel, "ReportParam." + optionalFieldName, item.Seq));
				break;
			}
		}
		return list;
	}

	private List<Component> BuildFilterControls(List<BAQRptFilter> baqRptFilter)
	{
		List<Component> list = new List<Component>();
		foreach (BAQRptFilter item in baqRptFilter)
		{
			list.Add(reportHelper.CreateFilterControl(item));
		}
		return list;
	}
}
