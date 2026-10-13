namespace Epicor.MetaFX.Core.Configurator.Wasm.Templates;

public static class CodeTemplateHelper
{
	public const string Indent1 = "\t";

	public const string Indent2 = "\t\t";

	public const string Indent3 = "\t\t\t";

	public const string Indent4 = "\t\t\t\t";

	public const string Indent5 = "\t\t\t\t\t";

	public const string Indent6 = "\t\t\t\t\t\t";

	public const string Indent7 = "\t\t\t\t\t\t\t";

	public const string Indent8 = "\t\t\t \t\t\t\t ";

	public static bool IsConfiguratorMethod(string type)
	{
		switch (type)
		{
		case "Changed":
		case "Validate":
		case "ReadOnlyExpression":
		case "Page_ReadOnlyExpression":
		case "DynamicListExpression":
		case "DynamicListExpressionUD":
		case "Leave":
		case "Load":
		case "Loaded":
		case "OnLoaded":
		case "OnComplete":
		case "ServerUDMethod":
		case "ClientUDMethod":
			return true;
		default:
			return false;
		}
	}

	public static bool IsConfiguratorUDMethod(string type)
	{
		if (!(type == "ClientUDMethod"))
		{
			return type == "ServerUDMethod";
		}
		return true;
	}

	public static string GetParamType(string dataType)
	{
		if (dataType?.ToLowerInvariant() == "datetime")
		{
			return "DateTime?";
		}
		return dataType;
	}

	public static string GetDefaultReturnValue(string returnType)
	{
		return returnType switch
		{
			"string" => "return \"\";", 
			"bool" => "return false;", 
			"DateTime?" => "return null;", 
			"int" => "return 0;", 
			"decimal" => "return 0.0M;", 
			"string[]" => "return new string[0];", 
			_ => "", 
		};
	}
}
