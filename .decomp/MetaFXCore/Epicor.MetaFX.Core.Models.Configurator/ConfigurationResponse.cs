using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class ConfigurationResponse
{
	public Configuration Configuration { get; set; }

	public ConfiguratorUDFunction ConfiguratorUDFunction { get; set; }

	public List<Method> Methods { get; set; }

	public List<ResponseObject> ResponseObjects { get; set; }

	public string WasmHash { get; set; }
}
