using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Configurator.Generation.Utils;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Epicor.MetaFX.Core.Models.MetaUI.Patch;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;
using Epicor.MetaFX.Core.Models.MetaUI.Strings;
using Epicor.MetaFX.Core.Models.MetaUI.Tools;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

public class ConfiguratorGenerator
{
	private readonly ILogger logger;

	private readonly string rootPath;

	private readonly HelperMethods helperMethods;

	private readonly ConfiguratorGeneratorHelper configuratorHelper;

	private readonly CSharpExpressionParser expressionParser;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	private readonly ConfiguratorLayoutHelper configuratorLayoutHelper;

	private readonly LayoutHelper LayoutHelper;

	private readonly IConfiguratorService configuratorService;

	private readonly IContentLoader contentLoader;

	private readonly Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager;

	private const string IMPORT_PATH = "../shared/imports/UIConfigurator/base";

	public ConfiguratorGenerator(string rootPath, ILogger logger, IConfiguratorService configuratorService, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		helperMethods = new HelperMethods(logger, contentLoader);
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
		configuratorLayoutHelper = new ConfiguratorLayoutHelper(logger);
		templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
		this.configuratorService = configuratorService;
		this.contentLoader = contentLoader;
		configuratorHelper = new ConfiguratorGeneratorHelper();
		LayoutHelper = new LayoutHelper(logger);
		expressionParser = new CSharpExpressionParser();
	}

	public EpMetaFxResponse<ReportResponse> Generate(ConfiguratorRequest configuratorRequest, Func<string> currentCultureName)
	{
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		string text = "Erp.UICfg." + configuratorRequest.ConfiguratorId + "_" + configuratorRequest.Company;
		epMetaFxResponse.Data = new ReportResponse
		{
			Id = text
		};
		string text2 = rootPath + "/" + text;
		if (contentLoader.DirectoryExists(text2) && contentLoader.GetFiles(text2).Length != 0)
		{
			epMetaFxResponse.IsSuccess = true;
			epMetaFxResponse.Data.FileName = text;
			epMetaFxResponse.Message = "Configurator already exists.";
			return epMetaFxResponse;
		}
		try
		{
			Configuration configuration = configuratorService.UpdateMethodsAndCreatePage(configuratorRequest.ConfiguratorId);
			ConfiguratorArgs configuratorArgs = CreateConfiguratorArgs(configuration, configuratorRequest, text2);
			helperMethods.EmptyDirectory(text2);
			object commonTemplate = templateManager.GetCommonTemplate(new EpMetaFxTemplateRequest
			{
				ViewType = "Configurator"
			}, currentCultureName);
			RowRulesManager rowRulesManager = new RowRulesManager(configuratorArgs);
			DataviewManager dataviewManager = new DataviewManager(configuratorArgs);
			PatchManager patchManager = new PatchManager(configuratorArgs);
			LayoutManager layoutManager = new LayoutManager(configuratorArgs);
			EventManager eventManager = new EventManager(configuratorArgs);
			DataViewSchema dataViewSchema = dataviewManager.GenerateSchema();
			RulesSchema rulesSchema = rowRulesManager.GenerateSchema();
			rulesSchema.RowRules = rowRulesManager.GenerateRules();
			EventSchema eventSchema = eventManager.GenerateSchema();
			PatchSchema patchSchema = patchManager.GenerateSchema();
			dataViewSchema.DataViews = dataviewManager.GenerateDataViews();
			Dictionary<PcPage, List<Component>> pageComponents = layoutManager.GenerateComponents();
			eventSchema.Events = eventManager.GenerateInitialEvents();
			eventSchema.Events.AddRange(eventManager.GenerateEvents());
			GenerateTools(text2);
			GenerateStrings(text2);
			dataviewManager.CreateFile(dataViewSchema);
			rowRulesManager.CreateFile(rulesSchema);
			patchManager.CreateFile(patchSchema);
			layoutManager.CreateLayoutAndPages(commonTemplate, pageComponents);
			eventManager.CreateFile(eventSchema);
			epMetaFxResponse.IsSuccess = true;
			epMetaFxResponse.Data.FileName = text;
			epMetaFxResponse.Message = "Configurator generated successfully.";
		}
		catch (Exception ex)
		{
			helperMethods.EmptyDirectory(text2);
			epMetaFxResponse.IsSuccess = false;
			logger.Debug(ex.Message);
			epMetaFxResponse.Message = "Configurator generation failed.";
			epMetaFxResponse.Details.Add(new MessageDetail
			{
				Type = "error",
				Message = ex.ToString()
			});
		}
		return epMetaFxResponse;
	}

	private ConfiguratorArgs CreateConfiguratorArgs(Configuration configuration, ConfiguratorRequest configuratorRequest, string outputPath)
	{
		ConfiguratorArgs configuratorArgs = new ConfiguratorArgs();
		configuratorArgs.ConfiguratorId = configuratorRequest.ConfiguratorId;
		if (configuratorRequest.LayoutType?.ToLowerInvariant() == "Absolute".ToLowerInvariant())
		{
			configuratorArgs.LayoutType = "Absolute";
		}
		else
		{
			configuratorArgs.LayoutType = "Responsive";
		}
		configuratorArgs.HelperMethods = helperMethods;
		configuratorArgs.ConfiguratorLayoutHelper = configuratorLayoutHelper;
		configuratorArgs.LayoutHelper = LayoutHelper;
		configuratorArgs.SchemaUpgrade = schemaUpgrade;
		configuratorArgs.TemplateManager = templateManager;
		configuratorArgs.Logger = logger;
		configuratorArgs.ImportPath = "../shared/imports/UIConfigurator/base";
		configuratorArgs.OutputPath = outputPath;
		configuratorArgs.RootPath = rootPath;
		configuratorArgs.Configuration = configuration;
		configuratorArgs.ConfiguratorHelper = configuratorHelper;
		configuratorArgs.ConfiguratorService = configuratorService;
		configuratorArgs.ExpressionParser = expressionParser;
		if (!string.IsNullOrEmpty(configuration.PcStatus[0].AttrClassId))
		{
			configuratorArgs.DynamicAttrClass = configuratorService.GetDynamicAttrClass(configuration.PcStatus[0].AttrClassId);
		}
		PopulateExtendendProperties(configuratorArgs);
		List<PcPage> list = configuration.PcPage.OrderBy((PcPage page) => page.DisplaySeq).ToList();
		if (list.Any())
		{
			configuratorArgs.LayoutPage = list[0];
		}
		return configuratorArgs;
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
					Refs = "../shared/imports/UIConfigurator/base/tools.jsonc#/tools"
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
					Refs = "../shared/imports/UIConfigurator/base/strings.jsonc#/strings"
				}
			}
		};
		StringsSchema o = stringsSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "strings");
		helperMethods.CreateJsonFile(data, path, "strings.jsonc");
	}

	private void PopulateExtendendProperties(ConfiguratorArgs args)
	{
		args.ExtendedConfiguration = new ExtendedConfiguration();
		args.ExtendedConfiguration.IsMultiPage = args.Configuration.PcPage.Count() > 1;
	}
}
