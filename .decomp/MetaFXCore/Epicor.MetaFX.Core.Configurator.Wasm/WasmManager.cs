using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Configurator.Wasm.Templates;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Configurator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Wasm;

public class WasmManager
{
	private readonly ILogger logger;

	private readonly WasmHelper configurationHelper;

	private readonly IWasmCompiler compiler;

	private readonly string rootPath;

	private readonly IUserDefinedApplicationService userDefinedApplicationSvc;

	public WasmManager(string rootPath, ILogger logger, IConfiguratorService configuratorService, IContentLoader contentLoader, IWasmCompiler wasmCompiler = null, IUserDefinedApplicationService userDefinedApplicationService = null)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		configurationHelper = new WasmHelper(rootPath, logger, configuratorService, contentLoader);
		if (wasmCompiler == null)
		{
			compiler = new Compiler(rootPath, logger);
		}
		else
		{
			compiler = wasmCompiler;
		}
		userDefinedApplicationSvc = userDefinedApplicationService;
	}

	public WasmResponse CreateWasm(EpMetaFxRequest request, List<JToken> allExecuteActions)
	{
		List<Method> list = new List<Method>();
		foreach (JToken allExecuteAction in allExecuteActions)
		{
			if (string.IsNullOrEmpty(allExecuteAction["param"]?.ToString()) || string.IsNullOrEmpty(allExecuteAction["param"]["method"]?.ToString()) || string.IsNullOrEmpty(allExecuteAction["expression"]?.ToString()))
			{
				continue;
			}
			Method method = new Method
			{
				Name = allExecuteAction["param"]["method"].ToString(),
				Expression = allExecuteAction["expression"].ToString(),
				Type = allExecuteAction["param"]["type"]?.ToString(),
				Control = allExecuteAction["param"]["control"]?.ToString(),
				ReturnType = allExecuteAction["param"]["returnType"]?.ToString(),
				ParameterExpression = "()",
				ClientMethodId = allExecuteAction["param"]["type"]?.ToString() + "_" + allExecuteAction["param"]["control"]
			};
			if (string.IsNullOrEmpty(method.Type))
			{
				List<string> list2 = new List<string>();
				if (allExecuteAction["param"]["inputs"] is JArray { Count: >0 } jArray)
				{
					List<string> list3 = new List<string>();
					foreach (JToken item in jArray)
					{
						string text = item["type"]?.ToString().ToLowerInvariant();
						string text2 = (string.IsNullOrEmpty(text) ? "string" : WasmHelper.INPUTTYPES[text]);
						string text3 = item["name"].ToString();
						if (text == "dataview")
						{
							text3 += "dv";
							list3.Add(string.Format("var {0} = WASMHelper.DeserializeToJArray({1});  ", item["name"], text3));
						}
						list2.Add(" " + text2 + " " + text3);
					}
					method.Expression = string.Join(Environment.NewLine, list3) + Environment.NewLine + method.Expression;
					method.ParameterExpression = "(" + string.Join(",", list2) + ")";
				}
				list.Add(method);
			}
			else if (allExecuteAction["param"]["type"]?.ToString() != "DynamicListExpressionUD")
			{
				list.Add(method);
			}
		}
		ConfigurationResponse configuration = configurationHelper.GetConfiguration(request.id, request.properties.company);
		if (configuration != null)
		{
			list.AddRange(configuration.Methods);
		}
		if (!list.Any())
		{
			return null;
		}
		CodeTemplateArgs codeTemplateArgs = new CodeTemplateArgs();
		codeTemplateArgs.Methods = list;
		codeTemplateArgs.ResponseObjects = configuration?.ResponseObjects;
		if (configuration != null)
		{
			codeTemplateArgs.PcInputs = configuration.Configuration.PcInputs;
			codeTemplateArgs.PcPages = configuration.Configuration.PcPage;
			codeTemplateArgs.IsConfigurator = true;
			foreach (Method method2 in codeTemplateArgs.Methods)
			{
				method2.Expression = configurationHelper.ParseExpression(method2, codeTemplateArgs.ResponseObjects, codeTemplateArgs.Methods);
			}
		}
		string text4 = new CodeTemplate(codeTemplateArgs).TransformText();
		IUserDefinedApplicationService userDefinedApplicationService = userDefinedApplicationSvc;
		if (userDefinedApplicationService != null && userDefinedApplicationService.IsStoringConfigDumpAllowed())
		{
			string text5 = Path.Combine(Path.GetTempPath(), "ConfigDump", "Server");
			Directory.CreateDirectory(text5);
			File.WriteAllText(Path.Combine(text5, request.id + ".cs"), text4);
		}
		EpMetaFxResponse<WasmResponse> epMetaFxResponse = compiler.Compile(request.id, text4, list, out var _);
		userDefinedApplicationSvc?.CreateNewRowForConversionLog(request.id, JsonConvert.SerializeObject(epMetaFxResponse.Details), epMetaFxResponse.IsSuccess);
		return epMetaFxResponse.Data;
	}
}
