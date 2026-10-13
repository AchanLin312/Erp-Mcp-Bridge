using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class UpdateConfiguratorRequest
{
	public EpMetaFxSaveRequestEx Request { get; set; }

	public Dictionary<string, JObject> Pages { get; set; }

	public UpdateConfiguratorRequest()
	{
		Pages = new Dictionary<string, JObject>();
	}
}
