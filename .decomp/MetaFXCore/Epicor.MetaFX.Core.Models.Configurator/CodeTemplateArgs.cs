using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class CodeTemplateArgs
{
	public List<PcInput> PcInputs { get; set; }

	public List<PcPage> PcPages { get; set; }

	public List<Method> Methods { get; set; }

	public List<ResponseObject> ResponseObjects { get; set; }

	public bool IsConfigurator { get; set; }
}
