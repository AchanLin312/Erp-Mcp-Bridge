using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV30 : UpgradeTarget
{
	public UpgradeSearchV30(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		RootV30 rootV = new RootV30();
		rootV.schema = "https://schemas.developer.epicor.com/schemas/v3.0/search.json";
		rootV.contentVersion = "3.0";
		rootV.imports = schema["imports"];
		rootV.search = (object)UpgradeSearch(schema["search"]);
		rootV.patch = schema["patch"];
		RootV30 root = rootV;
		return Deserialize(root);
	}

	private object UpgradeSearch(dynamic oldSchema)
	{
		if (oldSchema["displayColumns"] == null)
		{
			return oldSchema;
		}
		SearchV30 searchV = new SearchV30();
		searchV.title = oldSchema?.title;
		searchV.width = oldSchema?.width ?? ((object)100);
		searchV.sortBy = oldSchema?.sortBy;
		searchV.criteria = oldSchema?.criteria;
		searchV.columns = fetchNewColumns(oldSchema?.displayColumns, oldSchema?.displayColumnsCaptions);
		searchV.returnColumns = oldSchema?.returnColumns;
		searchV.searchTableIndex = oldSchema?.searchTableIndex;
		searchV.preLoadSearchFilter = oldSchema?.preLoadSearchFilter;
		searchV.pageSize = oldSchema?.pageSize;
		searchV.requestOptions = oldSchema?.requestOptions;
		searchV.requestOptionsGetRows = oldSchema?.requestOptionsGetRows;
		return searchV;
	}

	private JArray fetchNewColumns(JArray displayColumns, JArray displayColumnCaptions)
	{
		JArray jArray = new JArray();
		if (displayColumns != null && displayColumns.Count > 0)
		{
			for (int i = 0; i < displayColumns.Count; i++)
			{
				JObject jObject = new JObject();
				jObject.Add("field", displayColumns[i]);
				jArray.Add(jObject);
			}
		}
		return jArray;
	}
}
