using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class UpdateConfiguratorResponse
{
	public Configuration Configuration { get; set; }

	public DynamicAttrClass DynamicAttrClass { get; set; }

	public Configuration PreviuosConfiguration { get; set; }

	public List<InputEntity> InputEntities { get; set; }

	public List<PageEntity> PageEntities { get; set; }

	public UpdateConfiguratorResponse()
	{
		InputEntities = new List<InputEntity>();
		PageEntities = new List<PageEntity>();
	}
}
