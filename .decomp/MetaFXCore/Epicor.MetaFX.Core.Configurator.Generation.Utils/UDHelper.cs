using Epicor.MetaFX.Core.Models.Configurator;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

internal class UDHelper
{
	private readonly ConfiguratorArgs configuratorArgs;

	public UDHelper(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
	}

	public ConfiguratorUDFunction GetPcFunctionDefs()
	{
		UDGetRowsRequest uDGetRowsRequest = new UDGetRowsRequest();
		uDGetRowsRequest.WhereClausePcFunctionDef = "ConfigID = '" + configuratorArgs.Configuration.PcStatus[0].ConfigId + "'";
		return configuratorArgs.ConfiguratorService.GetUDFunction(uDGetRowsRequest);
	}
}
