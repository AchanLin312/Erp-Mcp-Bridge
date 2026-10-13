using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models.Configurator;

namespace Epicor.MetaFX.Core.Configurator.Wasm;

internal class WasmHelper
{
	private const string RETURNTYPE_VOID = "void";

	private const string RETURNTYPE = "return";

	private const string OUTTYPE = "out";

	private const string REFTYPE = "ref";

	private const string RETURNTYPE_OBJECT = "retObj";

	private const string RETURNTYPE_UD_OBJECT = "retUDObj";

	private const string TYPE_DATETIME = "datetime";

	private const string SERVER_FUNCTION = "server";

	public const string UDMETHOD_SERVER = "ServerUDMethod";

	public const string UDMETHOD_CLIENT = "ClientUDMethod";

	public const string INPUTTYPE_DATAVIEW = "dataview";

	public const string DATATYPE_STRING = "string";

	public static Dictionary<string, string> INPUTTYPES = new Dictionary<string, string>
	{
		{ "epBinding", "string" },
		{ "dataview", "string" },
		{ "number", "double" },
		{ "boolean", "bool" },
		{ "string", "string" }
	};

	private readonly ILogger logger;

	private readonly CSharpExpressionParser expressionParser;

	private readonly IConfiguratorService configuratorService;

	private readonly string rootPath;

	private readonly HelperMethods helper;

	public WasmHelper(string rootPath, ILogger logger, IConfiguratorService configuratorService, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.configuratorService = configuratorService;
		helper = new HelperMethods(logger, contentLoader);
		expressionParser = new CSharpExpressionParser();
	}

	public ConfigurationResponse GetConfiguration(string viewId, string company)
	{
		ConfigurationResponse configurationResponse = new ConfigurationResponse();
		configurationResponse.Configuration = configuratorService.GetConfiguration(GetConfigId(viewId, company));
		UDGetRowsRequest request = new UDGetRowsRequest
		{
			WhereClausePcFunctionDef = "ConfigID = '" + GetConfigId(viewId, company) + "'"
		};
		configurationResponse.ConfiguratorUDFunction = configuratorService.GetUDFunction(request);
		HashSet<string> hashSet = new HashSet<string>(configurationResponse.Configuration.PcDynLst.Select((PcDynLst d) => d.UserDefinedName));
		foreach (PcFunctionDef item in configurationResponse.ConfiguratorUDFunction.PcFunctionDef)
		{
			if (hashSet.Contains(item.FunctionName))
			{
				item.isDynamicListUD = true;
			}
		}
		configurationResponse.WasmHash = helper.CalculateHash(string.Join(",", configurationResponse.ConfiguratorUDFunction.PcFunctionDef.Select((PcFunctionDef def) => def.SysRevID)));
		Tuple<List<Method>, List<ResponseObject>> uDFunctions = GetUDFunctions(configurationResponse.ConfiguratorUDFunction);
		configurationResponse.Methods = uDFunctions.Item1;
		configurationResponse.ResponseObjects = uDFunctions.Item2;
		return configurationResponse;
	}

	private Tuple<List<Method>, List<ResponseObject>> GetUDFunctions(ConfiguratorUDFunction pcFunctions)
	{
		List<Method> list = new List<Method>();
		List<ResponseObject> list2 = new List<ResponseObject>();
		foreach (PcFunctionDef item in pcFunctions.PcFunctionDef)
		{
			ResponseObject responseObject = GetResponseObject(pcFunctions.PcFunctionParam, item);
			if (responseObject != null)
			{
				list2.Add(responseObject);
			}
			Method method = new Method
			{
				Name = item.FunctionName,
				ReturnType = ((responseObject == null) ? GetParamType(item.ReturnType) : responseObject.ClassName),
				ParameterExpression = GetFunctionSignature(item.FunctionName, pcFunctions.PcFunctionParam, item.isDynamicListUD)
			};
			if (item.FunctionType == "server")
			{
				method.Type = "ServerUDMethod";
				method.Expression = BuildUDServerMethods(item, pcFunctions.PcFunctionParam, responseObject);
			}
			else
			{
				method.Type = "ClientUDMethod";
				method.Expression = BuildUDClientMethods(item, pcFunctions.PcFunctionParam, responseObject);
			}
			list.Add(method);
		}
		return new Tuple<List<Method>, List<ResponseObject>>(list, list2);
	}

	public string ParseExpression(Method method, List<ResponseObject> responseObjects, List<Method> allMethods)
	{
		if (method.Type == "ServerUDMethod")
		{
			return method.Expression;
		}
		string[] array = Regex.Split(method.Expression, Environment.NewLine);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = text;
			foreach (Method mth in allMethods)
			{
				if (Regex.IsMatch(text, "\\b" + mth.Name + "\\b"))
				{
					ResponseObject responseObject = responseObjects.FirstOrDefault((ResponseObject ro) => ro.FunctionName == mth.Name);
					if (responseObject != null)
					{
						text2 = ParseRefAndOutExpression(text2, responseObject);
					}
					break;
				}
			}
			list.Add(text2);
		}
		return string.Join(Environment.NewLine, list);
	}

	private string GetConfigId(string viewId, string companyId)
	{
		string text = viewId.Replace("Erp.UICfg.", "");
		return text.Remove(text.Length - companyId.Length - 1);
	}

	private string GetFunctionSignature(string functionName, List<PcFunctionParam> pcFunctionParams, bool isDynamicListUD)
	{
		string text = string.Empty;
		foreach (PcFunctionParam item in from x in pcFunctionParams
			where x.FunctionName == functionName
			orderby x.ParamSeq
			select x)
		{
			string text2 = (((item.ParamType == "decimal") & isDynamicListUD) ? "double" : GetParamType(item.ParamType));
			string parameterName = item.ParameterName;
			text = ((!(text == string.Empty)) ? (text + ", " + text2 + " " + parameterName) : (" " + text2 + " " + parameterName));
		}
		return "(" + text + ")";
	}

	private ResponseObject GetResponseObject(List<PcFunctionParam> pcFunctionParams, PcFunctionDef function)
	{
		List<PcFunctionParam> list = (from pcFunc in pcFunctionParams
			where pcFunc.FunctionName == function.DispFunctionName
			orderby pcFunc.ParamSeq
			select pcFunc).ToList();
		if (!list.Any() || list.All((PcFunctionParam pcFunc) => string.IsNullOrEmpty(pcFunc.Modifier)))
		{
			return null;
		}
		ResponseObject responseObject = new ResponseObject
		{
			FunctionName = function.DispFunctionName
		};
		for (int num = 0; num < list.Count; num++)
		{
			PcFunctionParam pcFunctionParam = list[num];
			if (!string.IsNullOrEmpty(pcFunctionParam.Modifier))
			{
				responseObject.Parameters.Add(new ResponseParameter
				{
					Name = pcFunctionParam.ParameterName,
					Type = GetParamType(pcFunctionParam.ParamType),
					Sequence = num,
					IsRef = (pcFunctionParam.Modifier == "ref")
				});
			}
		}
		if (function.ReturnType != "void")
		{
			responseObject.Parameters.Add(new ResponseParameter
			{
				Name = "retObj",
				Type = function.ReturnType,
				IsReturnParameter = true
			});
		}
		return responseObject;
	}

	private string BuildUDClientMethods(PcFunctionDef functionDef, List<PcFunctionParam> pcFunctionParams, ResponseObject responseObject)
	{
		functionDef.Expression = expressionParser.RemoveComments(functionDef.Expression);
		if (responseObject == null)
		{
			return functionDef.Expression;
		}
		string[] array = Regex.Split(functionDef.Expression, Environment.NewLine);
		List<string> list = new List<string>();
		string expression = string.Empty;
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length != 0)
			{
				expression = text2;
				if (expressionParser.MatchReturn(text2))
				{
					list.Add(ParseReturnExpression(text.Trim(), responseObject));
				}
				else
				{
					list.Add(text2);
				}
			}
		}
		if (!expressionParser.MatchReturn(expression))
		{
			list.Add(ParseReturnExpression("return;", responseObject));
		}
		return string.Join(Environment.NewLine, list);
	}

	private string BuildUDServerMethods(PcFunctionDef functionDef, List<PcFunctionParam> functionParams, ResponseObject responseObject)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		StringBuilder stringBuilder = new StringBuilder();
		string text3 = string.Empty;
		foreach (PcFunctionParam item in functionParams.Where((PcFunctionParam funcParam) => funcParam.FunctionName == functionDef.FunctionName))
		{
			text3 = ((text3 != string.Empty) ? (text3 + ", " + item.ParameterName) : item.ParameterName);
			if (item.Modifier == "out")
			{
				text = ((!item.ParamType.Equals("string")) ? (text + Environment.NewLine + item.ParameterName + " = new " + item.ParamType + "();") : (text + Environment.NewLine + item.ParameterName + " = string.Empty;"));
			}
			switch (item.ParamType.ToUpperInvariant())
			{
			case "STRING":
				text2 = $"{text2}{Environment.NewLine}transPcValue = ServerUD.SetUDMethodParamString(transPcValue, \"{item.FunctionName}\", \"{item.ParameterName}\", {item.ParamSeq}, {item.ParameterName});";
				if (item.Modifier != string.Empty)
				{
					stringBuilder.AppendLine(item.ParameterName + " = ServerUD.GetUDMethodParamString(transPcValue, \"" + item.FunctionName + "\", \"" + item.ParameterName + "\");");
				}
				break;
			case "INT":
				text2 = $"{text2}{Environment.NewLine}transPcValue = ServerUD.SetUDMethodParamInt(transPcValue, \"{item.FunctionName}\", \"{item.ParameterName}\", {item.ParamSeq}, {item.ParameterName});";
				if (item.Modifier != string.Empty)
				{
					stringBuilder.AppendLine(item.ParameterName + " = ServerUD.GetUDMethodParamInt(transPcValue, \"" + item.FunctionName + "\", \"" + item.ParameterName + "\");");
				}
				break;
			case "DECIMAL":
				text2 = $"{text2}{Environment.NewLine}transPcValue = ServerUD.SetUDMethodParamDecimal(transPcValue, \"{item.FunctionName}\", \"{item.ParameterName}\", {item.ParamSeq}, Convert.ToDecimal({item.ParameterName}));";
				if (item.Modifier != string.Empty)
				{
					stringBuilder.AppendLine(item.ParameterName + " = ServerUD.GetUDMethodParamDecimal(transPcValue, \"" + item.FunctionName + "\", \"" + item.ParameterName + "\");");
				}
				break;
			case "DATETIME":
				text2 = $"{text2}{Environment.NewLine}transPcValue = ServerUD.SetUDMethodParamDateTime(transPcValue, \"{item.FunctionName}\", \"{item.ParameterName}\", {item.ParamSeq}, {item.ParameterName});";
				if (item.Modifier != string.Empty)
				{
					stringBuilder.AppendLine(item.ParameterName + " = ServerUD.GetUDMethodParamDateTime(transPcValue, \"" + item.FunctionName + "\", \"" + item.ParameterName + "\");");
				}
				break;
			case "BOOL":
				text2 = $"{text2}{Environment.NewLine}transPcValue = ServerUD.SetUDMethodParamBool(transPcValue, \"{item.FunctionName}\", \"{item.ParameterName}\", {item.ParamSeq}, {item.ParameterName});";
				if (item.Modifier != string.Empty)
				{
					stringBuilder.AppendLine(item.ParameterName + " = ServerUD.GetUDMethodParamBool(transPcValue, \"" + item.FunctionName + "\", \"" + item.ParameterName + "\");");
				}
				break;
			}
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		stringBuilder2.Append(Environment.NewLine + "pcValueDS = ServerUD.GetValueDataSet();");
		stringBuilder2.Append(Environment.NewLine + "pcValueDS.PcUserDefinedMethodParameters.Clear();");
		stringBuilder2.Append(Environment.NewLine + text);
		stringBuilder2.Append(Environment.NewLine + "var transPcValue = pcValueDS;");
		stringBuilder2.Append(Environment.NewLine + text2);
		stringBuilder2.Append(Environment.NewLine + "var retUDObj = await ServerUD.ExecuteUserDefined(\"" + functionDef.FunctionName + "\", " + functionDef.NoInputs.ToString().ToLowerInvariant() + ", transPcValue);");
		stringBuilder2.Append(Environment.NewLine + "var retObj = retUDObj.ReturnObj;");
		stringBuilder2.Append(Environment.NewLine + "transPcValue = retUDObj.PcValueDataSet;");
		stringBuilder2.Append(Environment.NewLine + "pcValueDS = transPcValue;");
		stringBuilder2.Append(Environment.NewLine + "ServerUD.SetValueDataSet(pcValueDS);");
		stringBuilder2.Append(Environment.NewLine + stringBuilder.ToString());
		if (responseObject != null)
		{
			if (responseObject.Parameters.Any())
			{
				stringBuilder2.Append(Environment.NewLine + "var response" + responseObject.ClassName + " = new " + responseObject.ClassName + "();");
				foreach (ResponseParameter parameter in responseObject.Parameters)
				{
					if (parameter.IsReturnParameter)
					{
						if (parameter.Type.ToLowerInvariant() == "datetime")
						{
							stringBuilder2.Append(Environment.NewLine + "response" + responseObject.ClassName + "." + parameter.Name + " = WASMHelper.ConvertToDateTime(retObj);");
						}
						else
						{
							stringBuilder2.Append(Environment.NewLine + "response" + responseObject.ClassName + "." + parameter.Name + " = " + ConvertDataType(parameter.Type, parameter.Name) + ";");
						}
					}
					else
					{
						stringBuilder2.Append(Environment.NewLine + "response" + responseObject.ClassName + "." + parameter.Name + " = " + parameter.Name + ";");
					}
				}
				stringBuilder2.Append(Environment.NewLine + "return response" + responseObject.ClassName + ";");
			}
		}
		else if (functionDef.ReturnType != "void")
		{
			if (functionDef.ReturnType.ToLowerInvariant() == "datetime")
			{
				stringBuilder2.Append(Environment.NewLine + "return WASMHelper.ConvertToDateTime(retObj);");
			}
			else
			{
				stringBuilder2.Append(Environment.NewLine + "return " + ConvertDataType(functionDef.ReturnType, "retObj") + ";");
			}
		}
		return stringBuilder2.ToString();
	}

	private string ParseReturnExpression(string line, ResponseObject responseObject)
	{
		bool flag = false;
		if (!Regex.IsMatch(line, "\\breturn(?:\\s|;|$)"))
		{
			return line;
		}
		if (line == "return;")
		{
			flag = true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = null;
		if (line != "return")
		{
			text = line.Substring("return".Length);
			text = text.Remove(text.LastIndexOf(';'));
		}
		stringBuilder.AppendLine("return new " + responseObject.ClassName + " { ");
		for (int i = 0; i < responseObject.Parameters.Count; i++)
		{
			ResponseParameter responseParameter = responseObject.Parameters[i];
			if (!responseParameter.IsReturnParameter)
			{
				stringBuilder.Append(responseParameter.Name + " = " + responseParameter.Name);
			}
			else if (!flag && text != "")
			{
				stringBuilder.Append("retObj = " + text);
			}
			if (i != responseObject.Parameters.Count - 1)
			{
				stringBuilder.Append(",");
			}
		}
		stringBuilder.Append("};");
		return stringBuilder.ToString();
	}

	private string ParseRefAndOutExpression(string line, ResponseObject responseObject)
	{
		string text = null;
		line = expressionParser.RemoveRefAndOut(line);
		if (line.IndexOf("=", StringComparison.Ordinal) >= 0)
		{
			text = line.Substring(0, line.IndexOf("=", StringComparison.Ordinal));
			line = line.Substring(text.Length).Trim().Substring(1)
				.Trim();
		}
		int num = line.IndexOf('(');
		int num2 = line.LastIndexOf(')');
		if (num < 0 || num2 < 0 || num2 <= num)
		{
			return line;
		}
		string[] array = (from p in line.Substring(num + 1, num2 - num - 1).Split(new char[1] { ',' })
			select p.Trim() into p
			where !string.IsNullOrEmpty(p)
			select p).ToArray();
		string text2 = Guid.NewGuid().ToString("N");
		line = Environment.NewLine + " var response" + text2 + " = " + line + ";";
		foreach (ResponseParameter parameter in responseObject.Parameters)
		{
			if (parameter.IsReturnParameter)
			{
				if (!string.IsNullOrEmpty(text))
				{
					line = line + Environment.NewLine + " " + text + " = response" + text2 + ".retObj;";
				}
			}
			else
			{
				line = ((parameter.Sequence < 0 || parameter.Sequence >= array.Length) ? $"{line}{Environment.NewLine} // WARNING: param.Sequence {parameter.Sequence} out of bounds for splittedParameters.Length {array.Length}" : (line + Environment.NewLine + " " + array[parameter.Sequence] + " = response" + text2 + "." + parameter.Name + ";"));
			}
		}
		return line;
	}

	private string ConvertDataType(string dataType, string parameterName)
	{
		string text = dataType.ToLowerInvariant();
		switch (text)
		{
		case "double":
		case "string":
		case "int32":
		case "int64":
		case "decimal":
		case "int":
		case "bool":
			return "(" + text + ")Convert.ChangeType(" + parameterName + ", typeof(" + text + "))";
		case "string[]":
			return "Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(" + parameterName + ".ToString())";
		default:
			throw new Exception("Invalid data type " + dataType + ".");
		}
	}

	private string GetParamType(string dataType)
	{
		if (dataType?.ToLowerInvariant() == "datetime")
		{
			return "DateTime?";
		}
		return dataType;
	}
}
