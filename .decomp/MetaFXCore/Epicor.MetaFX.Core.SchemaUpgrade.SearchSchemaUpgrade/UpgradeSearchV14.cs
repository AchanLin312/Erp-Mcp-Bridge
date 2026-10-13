using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV14 : UpgradeTarget
{
	public UpgradeSearchV14(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		RootV14 rootV = new RootV14();
		rootV.schema = "https://schemas.developer.epicor.com/schemas/v1.4/search.json";
		rootV.contentVersion = "1.4";
		rootV.imports = schema["imports"];
		rootV.search = UpgradeSearch(schema["search"]);
		rootV.patch = schema["patch"];
		RootV14 root = rootV;
		return Deserialize(root);
	}

	private object UpgradeSearch(dynamic oldSchema)
	{
		SearchV14 searchV = new SearchV14();
		searchV.title = oldSchema?.title;
		searchV.width = oldSchema?.width ?? ((object)100);
		searchV.sortBy = fetchNewSortBy(oldSchema?.sortBy);
		searchV.criteria = fetchNewcriteria(oldSchema?.criteria);
		searchV.displayColumns = oldSchema?.displayColumns;
		searchV.displayColumnsCaptions = oldSchema?.displayColumnsCaptions;
		searchV.returnColumns = oldSchema?.returnColumns;
		searchV.preLoadSearchFilter = oldSchema?.preLoadSearchFilter;
		searchV.pageSize = oldSchema?.pageSize;
		searchV.requestOptions = oldSchema?.requestOptions;
		searchV.requestOptionsGetRows = oldSchema?.requestOptionsGetRows;
		return searchV;
	}

	private JArray fetchNewSortBy(dynamic sortBy)
	{
		if (sortBy != null)
		{
			JArray jArray = sortBy as JArray;
			JArray jArray2 = new JArray();
			if (jArray != null && jArray.Count > 0)
			{
				foreach (JObject item in jArray)
				{
					JObject jObject = new JObject();
					JObject jObject2 = new JObject();
					JObject jObject3 = new JObject();
					foreach (KeyValuePair<string, JToken> item2 in item)
					{
						if (item2.Key == "controlType")
						{
							jObject2.Add("sourceTypeId", item2.Value);
						}
						else if (item2.Key == "StartingAt")
						{
							jObject3.Add("labelText", item2.Value);
						}
						else if (item2.Key == "epBinding")
						{
							jObject3.Add("epBinding", item2.Value);
						}
						else
						{
							jObject.Add(item2.Key, item2.Value);
						}
					}
					jObject2.Add("model", jObject3);
					jObject.Add("component", jObject2);
					jArray2.Add(jObject);
				}
				sortBy = jArray2;
			}
		}
		return sortBy;
	}

	private JObject fetchNewcriteria(dynamic criteria)
	{
		if ((criteria != null) && ((criteria.and != null) ? true : false))
		{
			JArray jArray = criteria.and as JArray;
			JArray jArray2 = new JArray();
			if (jArray != null)
			{
				foreach (JObject item in jArray)
				{
					JObject jObject = new JObject();
					JObject jObject2 = new JObject();
					JObject jObject3 = new JObject();
					foreach (KeyValuePair<string, JToken> item2 in item)
					{
						if (item2.Key == "controlType")
						{
							jObject2.Add("sourceTypeId", item2.Value);
						}
						else if (item2.Key == "epBinding")
						{
							jObject3.Add("epBinding", item2.Value);
						}
						else if (item2.Key == "enableSearch")
						{
							jObject3.Add("enableSearch", item2.Value);
						}
						else if (item2.Key == "epBindingSearchDesc")
						{
							jObject3.Add("epBindingSearchDesc", item2.Value);
						}
						else if (item2.Key == "labelTextSearchDesc")
						{
							jObject3.Add("labelTextSearchDesc", item2.Value);
						}
						else if (item2.Key == "label")
						{
							jObject3.Add("labelText", item2.Value);
						}
						else if (item2.Key == "componentProps")
						{
							foreach (KeyValuePair<string, JToken> item3 in (JObject)item2.Value)
							{
								if (item3.Key == "columns")
								{
									jObject3.Add("retrieveColumns", item3.Value);
								}
								else
								{
									jObject3.Add(item3.Key, item3.Value);
								}
							}
						}
						else
						{
							jObject.Add(item2.Key, item2.Value);
						}
					}
					jObject2.Add("model", jObject3);
					jObject.Add("component", jObject2);
					jArray2.Add(jObject);
				}
				criteria.and = jArray2;
			}
		}
		return criteria;
	}
}
