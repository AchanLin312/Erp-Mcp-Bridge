using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV13 : UpgradeTarget
{
	public UpgradeSearchV13(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		RootV13 rootV = new RootV13();
		rootV.schema = "https://schemas.developer.epicor.com/schemas/v1.3/search.json";
		rootV.contentVersion = "1.3";
		rootV.imports = schema["imports"];
		rootV.search = UpgradeSearch(schema["search"]);
		rootV.patch = schema["patch"];
		RootV13 root = rootV;
		return Deserialize(root);
	}

	private object UpgradeSearch(dynamic oldSchema)
	{
		SearchV13 searchV = new SearchV13();
		searchV.title = oldSchema?.title;
		searchV.width = oldSchema?.width;
		searchV.sortBy = fetchNewSortBy(oldSchema.sortBy);
		searchV.criteria = fetchNewcriteria(oldSchema.criteria);
		searchV.displayColumns = oldSchema?.displayColumns;
		searchV.displayColumnsCaptions = oldSchema?.displayColumnsCaptions;
		searchV.returnColumns = oldSchema?.returnColumns;
		searchV.preLoadSearchFilter = oldSchema?.preLoadSearchFilter;
		searchV.pageSize = ((!((oldSchema?.pageSize != null) ? true : false)) ? ((object)0) : oldSchema?.pageSize);
		searchV.requestOptions = oldSchema?.requestOptions;
		searchV.requestOptionsGetRows = oldSchema?.requestOptionsGetRows;
		return searchV;
	}

	private JArray fetchNewSortBy(dynamic sortBy)
	{
		if ((null != sortBy) && sortBy is JToken source)
		{
			source.ToList().ForEach(delegate(JToken sortByItem)
			{
				if (sortByItem is JObject jObject && jObject["id"] == null)
				{
					jObject.Add("id", jObject["column"]);
				}
			});
		}
		return sortBy;
	}

	private JObject fetchNewcriteria(dynamic criteria)
	{
		if ((null != criteria) && (criteria.and != null) && criteria.and is JToken source)
		{
			source.ToList().ForEach(delegate(JToken criteriaItem)
			{
				if (criteriaItem is JObject jObject && jObject["id"] == null)
				{
					jObject.Add("id", jObject["column"]);
				}
			});
		}
		return criteria;
	}
}
