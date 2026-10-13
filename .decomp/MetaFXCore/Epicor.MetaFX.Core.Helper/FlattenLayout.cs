using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Helper;

public class FlattenLayout
{
	internal class ComponentToken
	{
		public JObject Component { get; set; }

		public bool Added { get; set; }
	}

	private readonly List<ComponentToken> components = new List<ComponentToken>();

	private readonly List<ComponentToken> orphanComponents = new List<ComponentToken>();

	private readonly string[] ignoreKeys = "PatchInfo,DraftEvents,DraftLayout,DraftRules,PatchInfo,PatchTokens,AllowVersions,References,Properties,Wasm,IsLayerDisabled,ConfiguratorInfo,ApplicationStrings".Split(new char[1] { ',' });

	private readonly List<string> allowedChildrens = "components,bodyComponents,expandedHeaderComponents,extensionComponents,data".Split(new char[1] { ',' }).ToList();

	private readonly List<string> uniqueIds = new List<string>();

	private bool IsEnableFullPath;

	public FlattenLayout()
	{
	}

	public FlattenLayout(bool isEnableFullPath)
	{
		IsEnableFullPath = isEnableFullPath;
	}

	public JObject ConvertToTreeStructure(object rowApp)
	{
		JObject jObject = JObject.FromObject(rowApp);
		JObject jObject2 = new JObject();
		JArray jArray = new JArray();
		foreach (JProperty item2 in jObject.Properties())
		{
			if (!Enumerable.Contains(ignoreKeys, item2.Name))
			{
				if (!(item2.Value is JArray) && !(item2.Value is JObject))
				{
					jObject2.Add(item2);
				}
				else if (item2.Value is JObject)
				{
					jArray.Add(AddChild(item2.Value, item2.Name, item2.Name));
				}
				else if (item2.Value is JArray)
				{
					JObject item = new JObject
					{
						{
							"Path",
							NormalizePath(item2.Path, item2.Name, item2.Name)
						},
						{ "Name", item2.Name },
						{
							"Children",
							GetChildren(item2.Value as JArray, item2.Name, item2.Name)
						}
					};
					jArray.Add(item);
				}
			}
		}
		jObject2.Add("Children", jArray);
		return jObject2;
	}

	private JArray GetChildren(JArray property, string fromPath, string toPath)
	{
		JArray jArray = new JArray();
		int num = 0;
		foreach (JToken item in property)
		{
			if (!"D".Equals((item["mode"] ?? ((JToken)string.Empty)).ToString()))
			{
				string tokenId = GetTokenId(item);
				jArray.Add(AddChild(item, $"{fromPath}.{num}", NormalizePath(item.Path, $"{fromPath}.{num}", toPath + "/" + tokenId)));
			}
			num++;
		}
		return jArray;
	}

	internal static string GetTokenId(JToken token)
	{
		object obj = token.SelectToken("name")?.ToString();
		if (obj == null)
		{
			obj = token.SelectToken("model.id")?.ToString();
			if (obj == null)
			{
				obj = token.SelectToken("id")?.ToString();
				if (obj == null)
				{
					JToken? jToken = token.SelectToken("model.guid");
					if (jToken == null)
					{
						return null;
					}
					obj = jToken.ToString();
				}
			}
		}
		return (string)obj;
	}

	private string NormalizePath(string path, string from, string to)
	{
		return path.Replace("[", ".").Replace("]", "").Replace(from, to)
			.Replace(".", "/");
	}

	private JToken AddChild(JToken property, string fromPath, string toPath)
	{
		JObject jObject = new JObject { 
		{
			"Path",
			NormalizePath(property.Path, fromPath, toPath)
		} };
		JArray jArray = new JArray();
		if (property is JObject jObject2)
		{
			foreach (JProperty item in jObject2.Properties())
			{
				if (!(item.Value is JArray) && !(item.Value is JObject))
				{
					if (!Enumerable.Contains(ignoreKeys, item.Name))
					{
						jObject.Add(item);
					}
				}
				else if (!Enumerable.Contains(ignoreKeys, item.Name))
				{
					AddRootChildren(property, jArray, item, fromPath, toPath);
					AddModelChildren(property, jObject, jArray, item, fromPath, toPath);
				}
			}
		}
		jObject.Add("Children", jArray);
		return jObject;
	}

	private void AddRootChildren(JToken property, JArray children, JProperty jProperty, string fromPath, string toPath)
	{
		allowedChildrens.FindAll((string x) => x.Equals(jProperty.Name) && jProperty.Value.Count() > 0).ForEach(delegate(string x)
		{
			JArray property2 = jProperty.Value as JArray;
			string text = NormalizePath(property.Path + "." + x, fromPath, toPath);
			JObject item = new JObject
			{
				{ "Path", text },
				{ "Name", x },
				{
					"Children",
					GetChildren(property2, fromPath + "." + x, text)
				}
			};
			children.Add(item);
		});
	}

	private void AddModelChildren(JToken property, JObject child, JArray children, JProperty jProperty, string fromPath, string toPath)
	{
		if (jProperty.Name == "model")
		{
			JObject jPropertyValue = jProperty.Value as JObject;
			allowedChildrens.FindAll((string x) => jPropertyValue.GetValue(x) != null && jPropertyValue.GetValue(x).Count() > 0).ForEach(delegate(string x)
			{
				string text = NormalizePath(property.Path + ".model." + x, fromPath, toPath);
				JObject item = new JObject
				{
					{ "Path", text },
					{ "Name", x },
					{
						"Children",
						GetChildren(jProperty.Value[x] as JArray, fromPath + ".model." + x, text)
					}
				};
				children.Add(item);
			});
		}
		child.Add(jProperty);
	}

	public JObject ConvertToFlattenLayout(JObject layoutTree, string parentId = "")
	{
		JObject jObject = new JObject();
		foreach (JProperty item in layoutTree.Properties())
		{
			if (item.Name == "components" && item.Value is JArray componentTree)
			{
				JArray value = FlattenComponentTree(componentTree, null, parentId + "/components");
				jObject.Add(item.Name, value);
			}
			else
			{
				jObject.Add(item.Name, item.Value);
			}
		}
		if (parentId.Length > 0 && IsEnableFullPath)
		{
			jObject.Add("FullPath", parentId);
			if (jObject[parentId] is JArray)
			{
				foreach (JToken item2 in (IEnumerable<JToken>)jObject[parentId])
				{
					item2["FullPath"] = parentId + "/" + GetTokenId(item2);
				}
			}
		}
		return jObject;
	}

	public JObject ConvertToHierarchicalStructure(JObject layout, bool addEmptyComponents = true)
	{
		if (layout == null || IsHierarchicalStructure(layout))
		{
			return layout;
		}
		JObject jObject = new JObject();
		foreach (JProperty item in layout.Properties())
		{
			if (item.Name == "components" && item.Value is JArray jArray)
			{
				foreach (JToken item2 in jArray)
				{
					components.Add(new ComponentToken
					{
						Component = (JObject)item2,
						Added = false
					});
				}
				jObject.Add(item.Name, GetHierarchy("", addEmptyComponents));
			}
			else
			{
				jObject.Add(item.Name, item.Value);
			}
		}
		List<ComponentToken> list = components.FindAll((ComponentToken e) => !e.Added || OrphansManager.HasOrphanId(e.Component, "parentId"));
		if (list != null && list.Count > 0)
		{
			orphanComponents.AddRange(list);
		}
		return jObject;
	}

	internal List<ComponentToken> OrphanControlsList()
	{
		return orphanComponents;
	}

	internal ComponentToken FirstOrphanControl()
	{
		return components.FirstOrDefault((ComponentToken e) => !e.Added);
	}

	private bool IsUnprocessedChildComponent(ComponentToken componentToken, string parentId)
	{
		if (!componentToken.Added)
		{
			string jPropValue = GetJPropValue(componentToken.Component, "parentId", null);
			if (jPropValue != null && (jPropValue == parentId || jPropValue.Split(new char[1] { '#' })[0] == parentId || jPropValue.Split(new char[1] { '~' })[0] == parentId))
			{
				return true;
			}
		}
		return false;
	}

	private JArray GetHierarchy(string parentId, bool addEmptyComponents = true)
	{
		JArray jArray = new JArray();
		foreach (ComponentToken item in components.Where((ComponentToken e) => IsUnprocessedChildComponent(e, parentId)))
		{
			item.Added = true;
			JObject jObject = new JObject();
			foreach (JProperty item2 in item.Component.Properties())
			{
				jObject.Add(item2.Name, item2.Value);
			}
			JArray hierarchy = GetHierarchy(item.Component.GetValue("id").ToString(), addEmptyComponents);
			if ((hierarchy?.HasValues ?? false) | addEmptyComponents)
			{
				if (hierarchy.Count > 0 && hierarchy[0]["parentId"].ToString().Contains("#"))
				{
					for (int num = 0; num < hierarchy.Count; num++)
					{
						string[] array = hierarchy[num]["parentId"].ToString().Split(new char[1] { '#' });
						string text = "bodyComponents";
						if (array.Length > 1)
						{
							text = array[1];
						}
						hierarchy[num]["parentId"] = array[0];
						if (jObject["model"][text] == null && text == "expandedHeaderComponents")
						{
							jObject["model"]["expandedHeaderComponents"] = new JArray();
						}
						if (jObject["model"][text] == null && text == "extensionComponents")
						{
							jObject["model"]["extensionComponents"] = new JArray();
						}
						if (jObject["model"][text] == null || jObject["model"][text].Type != JTokenType.Array)
						{
							jObject["model"][text] = new JArray();
						}
						(jObject["model"][text] as JArray).Add(hierarchy[num]);
					}
				}
				else if (hierarchy.Count > 0 && hierarchy[0]["parentId"].ToString().Contains("~"))
				{
					for (int num2 = 0; num2 < hierarchy.Count; num2++)
					{
						string componentName = hierarchy[num2]["parentId"].ToString().Split(new char[1] { '~' })[1];
						hierarchy[num2]["parentId"] = hierarchy[num2]["parentId"].ToString().Split(new char[1] { '~' })[1];
						(jObject["model"]["data"].Where((JToken o) => o["id"].ToString() == componentName).FirstOrDefault() as JObject).Add("components", new JArray(hierarchy[num2]));
					}
				}
				else if (jObject["sourceTypeId"] != null && jObject["sourceTypeId"].ToString() == "metafx-panel-card")
				{
					jObject["model"]["bodyComponents"] = new JArray();
					for (int num3 = 0; num3 < hierarchy.Count; num3++)
					{
						(jObject["model"]["bodyComponents"] as JArray).Add(hierarchy[num3]);
					}
				}
				else
				{
					jObject.Add("components", hierarchy);
				}
			}
			jArray.Add(jObject);
		}
		return jArray;
	}

	public JArray FlattenComponentTree(JArray componentTree, string parentId, string parentPath = "")
	{
		JArray jArray = new JArray();
		int num = 0;
		foreach (JToken item in componentTree)
		{
			if (!(item is JObject jObject))
			{
				continue;
			}
			JObject jObject2 = new JObject();
			string currentId;
			if (jObject.ContainsKey("id"))
			{
				currentId = (string?)jObject.GetValue("id");
				currentId = getUniqueId(currentId);
			}
			else
			{
				currentId = getUniqueId((string?)jObject.GetValue("uniqueName"));
				jObject2.Add("id", currentId);
			}
			JToken arg = jObject["model"]?["id"] ?? ((JToken)currentId);
			AddFullPath(jObject2, $"{parentPath}/{arg}");
			if (!jObject.ContainsKey("parentId"))
			{
				jObject2.Add("parentId", parentId);
			}
			foreach (JProperty item2 in jObject.Properties())
			{
				if (item2.Name == "components" && item2.Value is JArray componentTree2)
				{
					foreach (JToken item3 in FlattenComponentTree(componentTree2, currentId, $"{parentPath}/{arg}/components"))
					{
						jArray.Add(item3);
					}
				}
				else if (item2.Name == "model" && Convert.ToString(jObject["sourceTypeId"]) == "metafx-tabstrip")
				{
					FlattenMetaFxTabStrip(jArray, jObject2, item2, currentId, $"{parentPath}/{arg}/model");
				}
				else if (item2.Name == "model" && IsPanelCard(jObject))
				{
					FlattenMetaFxPanelCard(jArray, jObject2, item2, $"{parentPath}/{arg}/model");
				}
				else if (item2.Name == "id")
				{
					jObject2.Add(item2.Name, currentId);
				}
				else if (item2.Name == "parentId")
				{
					if (item2.Value != null)
					{
						parentId = (string.IsNullOrEmpty(item2.Value.ToString()) ? null : item2.Value.ToString());
					}
					jObject2.Add(item2.Name, parentId);
				}
				else
				{
					jObject2.Add(item2.Name, item2.Value);
				}
			}
			jArray.Add(jObject2);
			num++;
		}
		return jArray;
	}

	private void FlattenMetaFxTabStrip(JArray flattenComponents, JObject component, JProperty jProperty, string id, string parentPath = "")
	{
		JObject jObject = jProperty.Value as JObject;
		foreach (JProperty item in jObject.Properties())
		{
			JArray jArray = jProperty.Value["data"] as JArray;
			if (item.Name == "id")
			{
				if (component["id"]?.ToString() != jObject["id"]?.ToString())
				{
					jObject["id"] = component["id"].ToString();
				}
			}
			else
			{
				if (!(item.Name == "data") || jArray == null)
				{
					continue;
				}
				foreach (JToken item2 in jArray)
				{
					string parentId = string.Empty;
					JObject jObject2 = item2 as JObject;
					if (jObject2["components"] != null)
					{
						parentId = string.Format("{0}~{1}", id, jObject2["id"]);
					}
					if (jObject2["components"] != null && jObject2["components"].Any())
					{
						jObject2["components"][0]["parentId"] = string.Format("{0}~{1}", jObject.Parent.Parent["id"], jObject2["id"]);
					}
					foreach (JProperty item3 in jObject2.Properties())
					{
						if (!(item3.Name == "components") || !(item3.Value is JArray componentTree))
						{
							continue;
						}
						foreach (JToken item4 in FlattenComponentTree(componentTree, parentId, parentPath + "/data/" + id + "/components"))
						{
							flattenComponents.Add(item4);
						}
					}
					if (jObject2.Property("components")?.Value != null)
					{
						jObject2.Property("components").Remove();
					}
				}
			}
		}
		component.Add(jProperty.Name, jProperty.Value);
	}

	private void FlattenMetaFxPanelCard(JArray flattenComponents, JObject component, JProperty jProperty, string parentPath = "")
	{
		JObject obj = jProperty.Value as JObject;
		List<string> list = new List<string> { "headerComponents", "bodyComponents", "expandedHeaderComponents", "extensionComponents" };
		foreach (JProperty item in obj.Properties())
		{
			JArray jArray = jProperty.Value[item.Name] as JArray;
			if (!list.Contains(item.Name) || jArray == null)
			{
				continue;
			}
			int num = 0;
			foreach (JToken item2 in jArray.ToList())
			{
				string uniqueId = getUniqueId((string?)item2["id"]);
				item2["id"] = uniqueId;
				JObject jObject = item2 as JObject;
				if (IsMetafxPanelCard(jObject))
				{
					JObject component2 = new JObject();
					FlattenMetaFxPanelCard(flattenComponents, component2, jObject.Property("model"), parentPath + "/" + item.Name + "/" + uniqueId + "/model");
				}
				else
				{
					JProperty jProperty2 = jObject.Property("components");
					if (jProperty2 != null && jProperty2.Value is JArray jArray2)
					{
						JArray jArray3 = FlattenComponentTree(jArray2, uniqueId, string.Format("{0}/{1}/{2}/components", parentPath, item.Name, item2["model"]?["id"] ?? ((JToken)uniqueId)));
						if (jArray2.Count > 0)
						{
							num++;
						}
						foreach (JToken item3 in jArray3)
						{
							flattenComponents.Add(item3);
						}
						jObject.Property("components").Remove();
					}
				}
				string text = jProperty.Parent["id"].ToString();
				string text2 = jObject["parentId"]?.ToString();
				if (text2 != null && text2 != text)
				{
					text2 = text;
				}
				jObject["parentId"] = text2 + "#" + item.Name;
				AddFullPath(jObject, string.Format("{0}/{1}/{2}", parentPath, item.Name, item2["model"]?["id"] ?? ((JToken)uniqueId)));
				flattenComponents.Add(jObject);
				jObject.Remove();
			}
		}
		component.Add(jProperty.Name, jProperty.Value);
	}

	private string getUniqueId(string currentId)
	{
		if (uniqueIds.Contains(currentId))
		{
			string text = string.Concat(Enumerable.Reverse(currentId.ToArray()).TakeWhile(char.IsNumber).Reverse());
			if (text.Length > 0)
			{
				currentId = currentId.Substring(0, currentId.Length - text.Length);
			}
			for (int i = 2; i < 100; i++)
			{
				string text2 = $"{currentId}{i}";
				if (!uniqueIds.Contains(text2))
				{
					uniqueIds.Add(text2);
					return text2;
				}
			}
		}
		else
		{
			uniqueIds.Add(currentId);
		}
		return currentId;
	}

	private bool IsHierarchicalStructure(JObject layout)
	{
		if (layout.ContainsKey("components"))
		{
			foreach (JObject item in layout.GetValue("components") as JArray)
			{
				if (item.ContainsKey("components"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsMetafxPanelCard(JObject comp)
	{
		if (!(Convert.ToString(comp["targetTypeId"]) == "metafx-panel-card"))
		{
			return Convert.ToString(comp["sourceTypeId"]) == "metafx-panel-card";
		}
		return true;
	}

	public static bool IsPanelCard(JObject comp)
	{
		if (!IsMetafxPanelCard(comp) && !(Convert.ToString(comp["targetTypeId"]) == "metafx-panel-card-grid") && !(Convert.ToString(comp["sourceTypeId"]) == "metafx-panel-card-grid") && !(Convert.ToString(comp["targetTypeId"]) == "metafx-panel-card-gantt"))
		{
			return Convert.ToString(comp["sourceTypeId"]) == "metafx-panel-card-gantt";
		}
		return true;
	}

	private void AddFullPath(JObject component, string path)
	{
		if (IsEnableFullPath)
		{
			component["FullPath"] = path;
		}
	}

	public static string GetJPropValue(JObject node, string propName, string defaultValue)
	{
		if (node == null)
		{
			return defaultValue;
		}
		JToken value = null;
		if (node.TryGetValue(propName, out value))
		{
			return value.ToString();
		}
		return defaultValue;
	}

	public static string GetJPropValue(JToken node, string propName, string defaultValue)
	{
		return GetJPropValue(node as JObject, propName, defaultValue);
	}

	public static T GetJPropValue<T>(JObject node, string propName, T defaultValue)
	{
		JToken value = null;
		if (node.TryGetValue(propName, out value))
		{
			return (dynamic)value;
		}
		return defaultValue;
	}
}
