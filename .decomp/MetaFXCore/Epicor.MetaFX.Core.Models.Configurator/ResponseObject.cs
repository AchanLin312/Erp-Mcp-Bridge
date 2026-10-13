using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class ResponseObject
{
	public string ClassName => FunctionName + "Response";

	public string FunctionName { get; set; }

	public List<ResponseParameter> Parameters { get; set; }

	public ResponseObject()
	{
		Parameters = new List<ResponseParameter>();
	}
}
