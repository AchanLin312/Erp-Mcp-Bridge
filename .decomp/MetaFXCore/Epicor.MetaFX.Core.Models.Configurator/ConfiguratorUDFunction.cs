using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class ConfiguratorUDFunction
{
	public List<PcFunctionDef> PcFunctionDef { get; set; }

	public List<PcFunctionDefAttch> PcFunctionDefAttch { get; set; }

	public List<PcFunctionParam> PcFunctionParam { get; set; }

	public List<object> ExtensionTables { get; set; }
}
