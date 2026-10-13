using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxSavePageRequest
{
	public string viewId { get; set; }

	public JArray[] pages { get; set; }
}
