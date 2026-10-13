using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio;

internal class OrphansManager
{
	public static List<string> GetOrphanPagesAfterLoad(LayerRequest layerRequest, JArray pages)
	{
		HashSet<string> uniquePages = GetUniqueLayerPages(layerRequest);
		foreach (JToken page in pages)
		{
			string jPropValue = FlattenLayout.GetJPropValue(page, "name", "");
			if (uniquePages.Contains(jPropValue))
			{
				uniquePages.Remove(jPropValue);
			}
		}
		layerRequest.CustomizedLayers.Where((EpMetaFxLayer layer) => layer.ProcessedInfo != null).ToList().ForEach(delegate(EpMetaFxLayer layer)
		{
			layer.ProcessedInfo.ToList().ForEach(delegate(KeyValuePair<string, LayerProcessedInfo> p)
			{
				if (p.Value.PatchedFlag)
				{
					string[] array = p.Key.Split(new char[1] { '~' });
					string item = array[1];
					if (array[0] == "pages" && uniquePages.Contains(item))
					{
						uniquePages.Remove(item);
					}
				}
			});
		});
		return uniquePages.ToList();
	}

	public static HashSet<string> GetUniqueLayerPages(LayerRequest layerRequest, bool withDiffOnly = true)
	{
		HashSet<string> uniquePages = new HashSet<string>();
		layerRequest.CustomizedLayers.ForEach(delegate(EpMetaFxLayer layer)
		{
			if (layer.Content != null)
			{
				IEnumerable<string> source = from content in layer.Content
					where content.Key != "config" && JsonMerger.IsPageContentKey(content.Key) && !uniquePages.Contains(content.Key) && content.Value != null
					select content.Key;
				JArray emptyArray = new JArray();
				source.ToList().ForEach(delegate(string page)
				{
					bool flag = false;
					if (layer.Content[page] is JObject jObject)
					{
						flag = jObject["Diffs"] is JArray jArray && HasGridModelsPathInDiffs(jArray.ToObject<List<JToken>>());
					}
					if (withDiffOnly)
					{
						JArray jPropValue = FlattenLayout.GetJPropValue(layer.Content[page] as JObject, "Diffs", emptyArray);
						if (jPropValue != null && jPropValue.Count() > 0 && !flag)
						{
							uniquePages.Add(page);
						}
					}
					else if (!flag)
					{
						uniquePages.Add(page);
					}
				});
			}
		});
		return uniquePages;
	}

	private static bool HasGridModelsPathInDiffs(List<JToken> diffs)
	{
		return diffs?.All(delegate(JToken entry)
		{
			string text = entry["Path"]?.ToString();
			return !string.IsNullOrEmpty(text) && (text.Contains("/model/gridModel") || !text.Equals("components", StringComparison.OrdinalIgnoreCase));
		}) ?? false;
	}

	public static EpMetaFxOrphansPage CreateOrpanPage(string name, string caption)
	{
		return new EpMetaFxOrphansPage
		{
			name = name,
			guid = Guid.NewGuid().ToString(),
			caption = caption,
			isOrphanPage = true,
			components = new List<Component>()
		};
	}

	public static JToken CreateOrphanPageToken(ILogger logger, string name, string caption, JToken components)
	{
		EpMetaFxOrphansPage obj = CreateOrpanPage(name, caption);
		JObject jObject = ConvertToJObject(logger, obj);
		jObject["components"] = components;
		return jObject;
	}

	public static Dictionary<string, JToken> CreateOrphanPageContent(ILogger logger, string name)
	{
		EpMetaFxOrphansPage obj = CreateOrpanPage(name, name);
		JObject value = ConvertToJObject(logger, obj);
		return new Dictionary<string, JToken> { { name, value } };
	}

	public static JObject ConvertToJObject<T>(ILogger logger, T obj)
	{
		return new LayoutHelper(logger).ConvertToJObject(obj);
	}

	public static JArray GetOrphanPagesFromPages(ILogger logger, JArray pages)
	{
		JArray ret = new JArray();
		pages.ToList().ForEach(delegate(JToken page)
		{
			JArray jArray = JComponents(page);
			JToken jToken = jArray.FirstOrDefault((JToken comp) => HasOrphanId(comp));
			if (jToken != null)
			{
				JToken jToken2 = CreateOrphanPageToken(logger, FlattenLayout.GetJPropValue(page, "name", ""), FlattenLayout.GetJPropValue(page, "caption", ""), new JArray(jToken));
				jToken2["isOrphanPage"] = false;
				ret.Add(jToken2);
				jArray.Remove(jToken);
			}
		});
		return ret;
	}

	public static bool AddOrphanComponentsPanel(ILogger logger, JObject hierarchicalStructure, List<JObject> orphanControls, string mode, JObject layout, EpMetaFxLayer diffLayer)
	{
		if (IsOrphanPage(hierarchicalStructure))
		{
			foreach (JToken item in (from component in JComponents(layout)
				where FlattenLayout.GetJPropValue(component, "layerName", null) == null
				select component).ToList())
			{
				item["layerName"] = diffLayer.LayerName;
			}
		}
		if (orphanControls.Any())
		{
			hierarchicalStructure["properties"] = new JObject
			{
				new JProperty("orphanControlExists", true)
			};
			if (mode != "AppStudio")
			{
				return false;
			}
			foreach (JObject item2 in orphanControls.Where((JObject component) => HasOrphanId(component, "parentId")).ToList())
			{
				item2["parentId"] = FlattenLayout.GetJPropValue(item2, "originalParentId", "");
			}
			foreach (JObject item3 in orphanControls.Where((JObject component) => FlattenLayout.GetJPropValue(component, "layerName", null) == null).ToList())
			{
				item3["layerName"] = diffLayer.LayerName;
			}
			bool result = false;
			if (hierarchicalStructure?["components"] != null)
			{
				JToken jToken = (from c in JComponents(hierarchicalStructure)
					where HasOrphanId(c)
					select c).FirstOrDefault();
				string text = "";
				if (jToken == null)
				{
					text = "Orphans_" + Guid.NewGuid();
					Component obj = new Component
					{
						Id = text,
						ParentId = null,
						SourceTypeId = "metafx-panel-card",
						Model = new MetaFxPanelCard
						{
							Id = text,
							Guid = Guid.NewGuid().ToString(),
							Hidden = false,
							Title = "Orphan Components",
							Expanded = true
						}
					};
					jToken = ConvertToJObject(logger, obj);
					JComponents(layout, createMissingProperty: true).Add(jToken);
				}
				else
				{
					text = FlattenLayout.GetJPropValue(jToken, "id", "");
				}
				foreach (JObject orphanControl in orphanControls)
				{
					string itemParentId = FlattenLayout.GetJPropValue(orphanControl, "parentId", "");
					if (itemParentId != "")
					{
						itemParentId = itemParentId.Split(new char[1] { '#' })[0].Split(new char[1] { '~' })[0];
						if (orphanControls.Find((JObject c) => FlattenLayout.GetJPropValue(c, "id", "").Equals(itemParentId)) != null)
						{
							continue;
						}
					}
					if (FlattenLayout.GetJPropValue(orphanControl, "originalParentId", null) == null)
					{
						orphanControl["originalParentId"] = orphanControl["parentId"];
					}
					orphanControl["parentId"] = text;
					result = true;
				}
			}
			return result;
		}
		if (hierarchicalStructure["properties"]?["orphanControlExists"] != null)
		{
			((JObject)hierarchicalStructure["properties"]).Remove("orphanControlExists");
		}
		return false;
	}

	public static void RemoveOrphanContainerComponents(JArray components)
	{
		foreach (JToken item in components.Where((JToken item) => HasOrphanId(item, "parentId")))
		{
			string jPropValue = FlattenLayout.GetJPropValue(item, "originalParentId", "");
			item["parentId"] = jPropValue;
			if (jPropValue != "")
			{
				((JObject)item).Remove("originalParentId");
			}
		}
		foreach (JToken item2 in components.Where((JToken item) => HasOrphanId(item)).ToList())
		{
			components.Remove(item2);
		}
		foreach (JObject item3 in (IEnumerable<JToken>)components.Where((JToken component) => FlattenLayout.GetJPropValue(component, "layerName", null) != null).ToList())
		{
			item3.Remove("layerName");
		}
	}

	public static void RemovePreviousLayerOrphanComponents(JObject hierarchicalObject)
	{
		if (!(hierarchicalObject["components"] is JArray jArray))
		{
			return;
		}
		foreach (JToken item in jArray.Where((JToken item) => HasOrphanId(item)).ToList())
		{
			jArray.Remove(item);
		}
		foreach (JToken item2 in jArray)
		{
			if (FlattenLayout.GetJPropValue(item2, "parentId", "") == FlattenLayout.GetJPropValue(item2, "originalParentId", null))
			{
				((JObject)item2).Remove("originalParentId");
				if (FlattenLayout.GetJPropValue(item2, "layerName", "") != "")
				{
					((JObject)item2).Remove("layerName");
				}
			}
		}
	}

	public static bool HasPagesFlaggedWithOrphans(LayerRequest layerRequest, JArray pages)
	{
		if (pages != null)
		{
			bool flag = pages.FirstOrDefault((JToken page) => page["properties"]?["orphanControlExists"] != null) != null;
			if (!flag && layerRequest != null)
			{
				List<EpMetaFxLayer> customizedLayers = layerRequest.CustomizedLayers;
				if (customizedLayers != null && customizedLayers.Any())
				{
					flag = GetOrphanPagesAfterLoad(layerRequest, pages).Any();
				}
			}
			return flag;
		}
		return false;
	}

	public static int AddOrphanPageToBase(JToken page, Dictionary<string, JToken> baseContent, string key)
	{
		if (IsOrphanPage(page))
		{
			if (!JComponents(page).Any())
			{
				return -1;
			}
			JToken jToken = page.DeepClone();
			jToken["components"] = new JArray();
			baseContent.Add(key, jToken);
			return 1;
		}
		return 0;
	}

	public static void PreProcessSaveRequest(EpMetaFxSaveRequestEx request, EpMetaFxLayer layerInfo)
	{
		if (request.orphans == null || !request.orphans.Any())
		{
			return;
		}
		request.orphans.ToList().ForEach(delegate(JToken page)
		{
			string jPropValue = FlattenLayout.GetJPropValue(page, "name", "");
			if (jPropValue.Length > 0)
			{
				if (IsOrphanPage(page))
				{
					layerInfo.Content.Add(jPropValue, page);
				}
				else if (layerInfo.Content.ContainsKey(jPropValue))
				{
					JToken token = layerInfo.Content[jPropValue];
					JArray jArray = JComponents(token);
					JToken jToken = jArray.FirstOrDefault((JToken c) => HasOrphanId(c));
					if (jToken != null)
					{
						jArray.Remove(jToken);
					}
					JArray jArray2 = JComponents(page);
					if (jArray2.Any())
					{
						jArray = JComponents(token, createMissingProperty: true);
						jArray.Add(jArray2[0]);
					}
				}
			}
		});
	}

	public static bool HasOrphanId(JToken node, string propName = "id")
	{
		return FlattenLayout.GetJPropValue(node, propName, "").StartsWith("Orphans_");
	}

	public static bool IsOrphanPage(JToken page)
	{
		if (page != null && page.GetType().Name == "JObject")
		{
			return FlattenLayout.GetJPropValue((JObject)page, "isOrphanPage", defaultValue: false);
		}
		return false;
	}

	public static JArray JComponents(JToken token, bool createMissingProperty = false)
	{
		JArray jArray = token["components"] as JArray;
		if (jArray == null)
		{
			jArray = new JArray();
			if (createMissingProperty)
			{
				token["components"] = jArray;
			}
		}
		return jArray;
	}
}
