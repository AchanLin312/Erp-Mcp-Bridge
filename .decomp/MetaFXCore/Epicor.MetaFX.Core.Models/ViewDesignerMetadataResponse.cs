using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models;

public class ViewDesignerMetadataResponse : ViewMetadataResponse
{
	public object DraftLayout { get; set; }

	public object DraftRules { get; set; }

	public List<PatchInfo> PatchInfo { get; set; }

	public object DraftEvents { get; set; }

	public Dictionary<string, List<JObject>> PatchTokens { get; set; }
}
