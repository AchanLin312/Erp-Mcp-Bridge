using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV11 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public SearchV11 search { get; set; }

		public JArray patch { get; set; }
	}

	public class SearchV11
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
	}

	public UpgradeSearchV11(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.1/search.json";
		root.contentVersion = "1.1";
		root.imports = schema["imports"];
		root.search = UpgradeSearch(schema["search"]);
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private object UpgradeSearch(dynamic oldSchema)
	{
		SearchV11 searchV = new SearchV11();
		searchV.title = "Search";
		searchV.width = 100;
		searchV.sortBy = oldSchema.sortBy;
		searchV.criteria = oldSchema.criteria;
		searchV.displayColumns = oldSchema.displayColumns;
		searchV.displayColumnsCaptions = oldSchema.displayColumnsCaptions;
		searchV.returnColumns = oldSchema.returnColumns;
		searchV.preLoadSearchFilter = oldSchema.preLoadSearchFilter;
		searchV.pageSize = oldSchema.pageSize;
		searchV.requestOptions = oldSchema.requestOptions;
		return searchV;
	}
}
