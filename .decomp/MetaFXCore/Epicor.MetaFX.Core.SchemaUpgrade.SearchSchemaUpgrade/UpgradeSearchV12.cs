using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV12 : UpgradeTarget
{
	public UpgradeSearchV12(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		UpgradeSearchV11.Root root = new UpgradeSearchV11.Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.2/search.json";
		root.contentVersion = "1.2";
		root.imports = schema["imports"];
		root.search = UpgradeSearch(schema["search"]);
		root.patch = schema["patch"];
		UpgradeSearchV11.Root root2 = root;
		return Deserialize(root2);
	}

	private object UpgradeSearch(dynamic oldSchema)
	{
		UpgradeSearchV11.SearchV11 searchV = new UpgradeSearchV11.SearchV11();
		searchV.title = "Search";
		searchV.width = 100;
		searchV.sortBy = fetchNewSortBy(oldSchema.sortBy);
		searchV.criteria = fetchNewCriteria(oldSchema.criteria);
		searchV.displayColumns = oldSchema?.displayColumns;
		searchV.displayColumnsCaptions = oldSchema?.displayColumnsCaptions;
		searchV.returnColumns = oldSchema?.returnColumns;
		searchV.preLoadSearchFilter = oldSchema?.preLoadSearchFilter;
		searchV.pageSize = ((!((oldSchema?.pageSize != null) ? true : false)) ? ((object)0) : oldSchema?.pageSize);
		searchV.requestOptions = oldSchema?.requestOptions;
		return searchV;
	}

	private dynamic ConvertToErpControlTypes(dynamic controlType)
	{
		if (controlType == "textbox")
		{
			return "erp-text-box";
		}
		if (controlType == "ep-combo" || controlType == "dropdown")
		{
			return "erp-combo-box";
		}
		if (controlType == "date")
		{
			return "erp-date-picker";
		}
		if (controlType == "numeric")
		{
			return "erp-numeric-box";
		}
		return controlType;
	}

	private JArray fetchNewSortBy(dynamic sortBy)
	{
		if ((null != sortBy) && sortBy is JToken source)
		{
			source.ToList().ForEach(delegate(JToken sortByItem)
			{
				if (sortByItem is JObject jObject)
				{
					if (jObject["epBinding"] == null)
					{
						jObject.Add("epBinding", "");
					}
					jObject["controlType"] = ConvertToErpControlTypes(jObject["controlType"]);
				}
			});
		}
		return sortBy;
	}

	private JObject fetchNewCriteria(dynamic criteria)
	{
		if (null != criteria)
		{
			((criteria as JObject)?["and"])?.ToList().ForEach(delegate(JToken criteriaAnditem)
			{
				if (criteriaAnditem is JObject jObject)
				{
					jObject["controlType"] = ConvertToErpControlTypes(jObject["controlType"]);
					if (jObject["epBinding"] == null)
					{
						jObject.Add("epBinding", "");
					}
				}
			});
			((criteria as JObject)?["or"])?.ToList().ForEach(delegate(JToken criteriaAnditem)
			{
				JObject jObject = criteriaAnditem as JObject;
				jObject["controlType"] = ConvertToErpControlTypes(jObject["controlType"]);
				if (jObject["epBinding"] == null)
				{
					jObject.Add("epBinding", "");
				}
			});
		}
		return criteria;
	}
}
