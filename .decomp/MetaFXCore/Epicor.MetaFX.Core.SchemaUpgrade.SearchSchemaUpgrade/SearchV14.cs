using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

public class SearchV14
{
	public string title { get; set; }

	public int width { get; set; }

	public JArray sortBy { get; set; }

	public JObject criteria { get; set; }

	public JArray displayColumns { get; set; }

	public JArray displayColumnsCaptions { get; set; }

	public JArray returnColumns { get; set; }

	public string preLoadSearchFilter { get; set; }

	public int pageSize { get; set; }

	public JObject requestOptions { get; set; }

	public JObject requestOptionsGetRows { get; set; }
}
