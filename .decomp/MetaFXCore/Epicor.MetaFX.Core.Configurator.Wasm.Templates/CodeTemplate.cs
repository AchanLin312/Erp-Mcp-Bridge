using System.CodeDom.Compiler;
using System.Linq;
using Epicor.MetaFX.Core.Models.Configurator;

namespace Epicor.MetaFX.Core.Configurator.Wasm.Templates;

[GeneratedCode("Microsoft.VisualStudio.TextTemplating", "16.0.0.0")]
public class CodeTemplate : CodeTemplateBase
{
	private readonly CodeTemplateArgs args;

	public virtual string TransformText()
	{
		Write("\r\nusing System;\r\nusing System.Text;\r\nusing System.Collections.Generic;\r\nusing System.IO;\r\nusing System.Linq;\r\nusing System.Drawing;\r\nusing System.Net.Http;\r\nusing System.Reflection;\r\nusing System.Threading.Tasks;\r\nusing Newtonsoft.Json;\r\nusing Newtonsoft.Json.Linq;\r\nusing Epicor.MetaFx.Wasm;\r\nusing Epicor.MetaFx.Wasm.Configurator;\r\nusing WebAssembly;\r\nusing Epicor.MetaFx.Wasm.Configurator.Arguments;\r\nusing Epicor.MetaFx.Wasm.Configurator.Models;\r\nusing Epicor.MetaFx.Wasm.Services;\r\n\r\nnamespace Epicor.MetaFx.Wasm.Dynamic\r\n{\r\n ");
		foreach (ResponseObject responseObject in args.ResponseObjects)
		{
			WriteLine("\tpublic class {0} ", responseObject.ClassName);
			WriteLine("{");
			foreach (ResponseParameter parameter in responseObject.Parameters)
			{
				WriteLine("\t\tpublic {1} {0} {{get; set;}}", parameter.Name, parameter.Type);
			}
			WriteLine("\t}");
		}
		Write("\r\n  public class PcPageCollection : DomObject\r\n  {\r\n    public PcPageCollection(JSObject jsObject) : base(jsObject)\r\n    {\r\n    }\r\n\r\n    public static List<string> GetProperties(){\r\n      var properties = new List<string>();\r\n       ");
		foreach (PcPage pcPage in args.PcPages)
		{
			WriteLine("\tproperties.Add(\"Page{0}\");", pcPage.PageSeq);
		}
		Write("      return properties;\r\n    }\r\n\r\n    public static PcPageCollection CreateEmptyObject(JSObject handler)\r\n    {\r\n     var pageCollection = Activator.CreateInstance(typeof(PcPageCollection), handler);\r\n     var properties = typeof(PcPageCollection).GetProperties(System.Reflection.BindingFlags.Public\r\n    | System.Reflection.BindingFlags.Instance\r\n    | System.Reflection.BindingFlags.DeclaredOnly);\r\n     \r\n     foreach (var property in properties)\r\n     {\r\n      var valueProperty = Activator.CreateInstance(property.PropertyType, handler);\r\n      property.SetValue(pageCollection, valueProperty);\r\n      var valuePropertyInfo = property.PropertyType.GetProperty(\"PageName\");\r\n      valuePropertyInfo.SetValue(valueProperty, property.Name);\r\n     }\r\n     return (PcPageCollection)pageCollection;\r\n    }   \r\n   ");
		foreach (PcPage pcPage2 in args.PcPages)
		{
			WriteLine("\tpublic Page Page{0} {{get; set;}}", pcPage2.PageSeq);
		}
		Write("  } \r\n\r\n\r\n  public class InputObject : DomObject\r\n  {\r\n   public InputObject(JSObject jsObject) : base(jsObject)\r\n   {\r\n   }\r\n\r\n   public static Dictionary<string, string> GetProperties(){\r\n      var properties = new Dictionary<string,string>();\r\n       ");
		foreach (PcInput pcInput in args.PcInputs)
		{
			WriteLine("\tproperties.Add(\"{0}\",\"{1}\");", pcInput.InputName, pcInput.ControlType);
		}
		Write("      return properties;\r\n   }\r\n\r\n  ");
		foreach (PcInput pcInput2 in args.PcInputs)
		{
			WriteLine("\tpublic InputControlValueBound<{1}> {0} {{get;  set;}}", pcInput2.InputName, CodeTemplateHelper.GetParamType(pcInput2.DataType));
		}
		Write("  \r\n   public static InputObject CreateEmptyInputObject(JSObject inputHandler)\r\n    {\r\n     var input = Activator.CreateInstance(typeof(InputObject), inputHandler);\r\n     var inputProperties = typeof(InputObject).GetProperties(System.Reflection.BindingFlags.Public\r\n    | System.Reflection.BindingFlags.Instance\r\n    | System.Reflection.BindingFlags.DeclaredOnly);\r\n     \r\n     foreach (var inputProperty in inputProperties)\r\n     {\r\n      var valueProperty = Activator.CreateInstance(inputProperty.PropertyType, inputHandler, inputProperty.Name);\r\n      inputProperty.SetValue(input, valueProperty);\r\n     }\r\n     return (InputObject)input;\r\n    }\r\n  }\r\n\r\n  public class UDMethodsObject\r\n  {\r\n    private static InputObject Inputs;\r\n    private static ConfiguratorContext Context;\r\n    private static MessageBox MessageBox;\r\n    private static LookUp PCLookUp;\r\n    private static UserDefinedFunctionsLib ServerUD;\r\n    private static PcValueDataSet pcValueDS;\r\n    private static PageRefresh Refresh;\r\n    \r\n    public UDMethodsObject(InputObject inputs, ConfiguratorContext context, MessageBox messageBox, LookUp pCLookUp, UserDefinedFunctionsLib serverUD, PageRefresh refresh)\r\n    {\r\n     Inputs = inputs;\r\n     Context = context;\r\n     MessageBox = messageBox;\r\n     PCLookUp = pCLookUp;\r\n     ServerUD = serverUD;\r\n     Refresh = refresh;\r\n    }  \r\n    ");
		foreach (Method item in args.Methods.Where((Method method) => CodeTemplateHelper.IsConfiguratorUDMethod(method.Type)))
		{
			if (item.ReturnType != "void")
			{
				WriteLine("public async Task<" + item.ReturnType + "> " + item.Name + item.ParameterExpression);
			}
			else
			{
				WriteLine("public async Task " + item.Name + item.ParameterExpression);
			}
			WriteLine("{");
			WriteLine("\t\t" + item.Expression);
			if (item.ReturnType != "void" && string.IsNullOrEmpty(item.Expression))
			{
				WriteLine("\t\t" + CodeTemplateHelper.GetDefaultReturnValue(item.ReturnType));
			}
			WriteLine("\t\t}");
		}
		Write("\r\n  }\r\n\r\n  public class RunClass\r\n  {\r\n    protected static InputObject Inputs;\r\n    protected static ConfiguratorContext Context;\r\n    protected static MessageBox MessageBox;\r\n    protected static LookUp PCLookUp;\r\n    protected static UDMethodsObject UDMethods;\r\n    protected static UserDefinedFunctionsLib ServerUD;\r\n    protected static PageLeaveFunctionsLib PageLeaveFunctions;\r\n    protected static PcPageCollection Pages;\r\n    protected static PageRefresh Refresh;\r\n    protected static PcValueDataSet pcValueDS;\r\n    protected static SystemFunctionsLib SystemFunctions;\r\n\r\n    public static string GetProperties(){\r\n        var returnObject = new { Inputs = InputObject.GetProperties(), Pages = PcPageCollection.GetProperties() } ;\r\n        return WASMHelper.Serialize(returnObject, true);\r\n    }\r\n    \r\n    public static void SetHandlers(JSObject contextJSHandler,\r\n                                    JSObject eventJSHandler,\r\n                                    JSObject pageJSHandler,\r\n                                    JSObject pageCollectionJSHandler,\r\n                                    JSObject commonJSHandler,\r\n                                    JSObject inputJSHandler,\r\n                                    JSObject pcConfigSmartStringJSHandler)\r\n    {\r\n      Context = new ConfiguratorContext(contextJSHandler);\r\n      MessageBox = new MessageBox(eventJSHandler);\r\n      PCLookUp = new LookUp(Context, commonJSHandler);\r\n      PageLeaveFunctions = new PageLeaveFunctionsLib(pageJSHandler);\r\n      Inputs = InputObject.CreateEmptyInputObject(inputJSHandler);\r\n      Pages = PcPageCollection.CreateEmptyObject(pageCollectionJSHandler);\r\n      Refresh = new PageRefresh(eventJSHandler);\r\n      ServerUD = new UserDefinedFunctionsLib(Context, commonJSHandler);\r\n      UDMethods = new UDMethodsObject(Inputs, Context, MessageBox, PCLookUp, ServerUD, Refresh);\r\n      SystemFunctions = new SystemFunctionsLib(Context, pcConfigSmartStringJSHandler, inputJSHandler);\r\n      LogService.Log(\"SetHandlers completed.\");\r\n    }\r\n\r\n    public static void DisposeHandlers()\r\n    {\r\n      Context?.Dispose();\r\n      MessageBox?.Dispose();\r\n      PCLookUp?.Dispose();\r\n      PageLeaveFunctions?.Dispose();\r\n      Inputs?.Dispose();\r\n      Pages?.Dispose();\r\n      Refresh?.Dispose();\r\n      ServerUD?.Dispose();\r\n      SystemFunctions?.Dispose();\r\n      LogService.Log(\"DisposeHandlers completed.\");\r\n    }\r\n    ");
		if (args.IsConfigurator)
		{
			foreach (Method item2 in args.Methods.Where((Method method) => CodeTemplateHelper.IsConfiguratorMethod(method.Type)))
			{
				switch (item2.Type)
				{
				case "Leave":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var Args = new PageLeaveArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "Load":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var LoadArgs = new PageLoadArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "Loaded":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var Args = new PageChangeArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "OnLoaded":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var OnLoadedArgs = new ConfiguratorOnLoadedArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "OnComplete":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var OnCompleteArgs = new ConfiguratorOnCompleteArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "Changed":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var ChangedArgs = new InputChangedArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "Validate":
					WriteLine("\tpublic async static Task Configurator_" + item2.Name + "(JSObject argsHandler)");
					WriteLine("\t{");
					WriteLine("\t\tusing(var ValidateArgs = new InputValidateArgs(argsHandler)){");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t\t}");
					WriteLine("\t}");
					break;
				case "Page_ReadOnlyExpression":
				case "ReadOnlyExpression":
				case "DynamicListExpression":
					WriteLine("\tpublic async static Task<bool> Configurator_" + item2.Name + "()");
					WriteLine("\t{");
					WriteLine("\t\t" + item2.Expression);
					WriteLine("\t}");
					break;
				case "ClientUDMethod":
				case "ServerUDMethod":
					if (item2.ReturnType != "void")
					{
						WriteLine("\tpublic async static Task<" + item2.ReturnType + "> " + item2.Name + item2.ParameterExpression);
					}
					else
					{
						WriteLine("\tpublic async static Task " + item2.Name + item2.ParameterExpression);
					}
					WriteLine("\t{");
					WriteLine("\t\t" + item2.Expression);
					if (item2.ReturnType != "void" && string.IsNullOrEmpty(item2.Expression))
					{
						WriteLine("\t\t" + CodeTemplateHelper.GetDefaultReturnValue(item2.ReturnType));
					}
					WriteLine("\t}");
					break;
				}
			}
		}
		foreach (Method item3 in args.Methods.Where((Method method) => !CodeTemplateHelper.IsConfiguratorMethod(method.Type)))
		{
			WriteLine("\tpublic async static Task<object> " + item3.Name + item3.ParameterExpression);
			WriteLine("\t{");
			WriteLine("\t\t" + item3.Expression);
			WriteLine("\t}");
		}
		Write("    \r\n  }\r\n}\r\n");
		return base.GenerationEnvironment.ToString();
	}

	public CodeTemplate(CodeTemplateArgs args)
	{
		this.args = args;
	}
}
