namespace Epicor.MetaFX.Core.Models.Configurator;

public class UDGetRowsRequest
{
	public string WhereClausePcFunctionDef { get; set; }

	public string WhereClausePcFunctionDefAttch { get; set; }

	public string WhereClausePcFunctionParam { get; set; }

	public int PageSize { get; set; }

	public int AbsolutePage { get; set; }
}
