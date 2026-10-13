using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Layers;
using JsonDiffPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tavis;

namespace Epicor.MetaFX.Core.AppStudio.LayerManager;

public class JsonDiff
{
	private const string ID_PROPERTY = "id";

	private const string MODE_PROPERTY = "mode";

	private const string REVERT = "R";

	private readonly ILogger logger;

	private readonly bool isEnableFullPath;

	private readonly bool enablePropertyLevelDiffs;

	public JsonDiff(ILogger logger)
		: this(logger, isEnableFullPath: false, enablePropertyLevelDiffs: false)
	{
	}

	public JsonDiff(ILogger logger, bool isEnableFullPath, bool enablePropertyLevelDiffs)
	{
		this.logger = logger;
		this.isEnableFullPath = isEnableFullPath;
		this.enablePropertyLevelDiffs = enablePropertyLevelDiffs;
	}

	public JToken ApplyModes(string type, JToken baseJson, JToken modifiedToken)
	{
		if (modifiedToken.Type != JTokenType.Array || baseJson.Type != JTokenType.Array)
		{
			logger.Debug("Invalid token for " + type + ".");
			throw new Exception("Invalid token for " + type + ".");
		}
		JArray jArray = modifiedToken as JArray;
		JArray jArray2 = baseJson as JArray;
		if (jArray != null)
		{
			for (int i = 0; i < jArray.Count; i++)
			{
				if (jArray[i]["mode"] == null || string.IsNullOrEmpty(jArray[i]["mode"].ToString()))
				{
					continue;
				}
				object obj = JsonConvert.DeserializeObject(jArray[i].ToString());
				JObject newObject = obj as JObject;
				if (newObject != null)
				{
					JToken jToken = (jArray2 ?? throw new InvalidOperationException()).FirstOrDefault((JToken arr) => arr["id"]?.ToString() == newObject["id"]?.ToString());
					if (jToken != null)
					{
						UpdateArray(jArray2, jToken, newObject, jArray2.IndexOf(jToken));
					}
					else
					{
						jArray2.Add(newObject);
					}
				}
			}
		}
		return jArray2;
	}

	public List<JsonDiffInfo> GetDiffs(JToken oldJson, JToken newJson, string pageName = "")
	{
		List<JsonDiffInfo> topLevelDiffs = GetTopLevelDiffs(oldJson, newJson, pageName);
		List<JToken> tokens = GetTokens(oldJson);
		List<JToken> tokens2 = GetTokens(newJson);
		int num = 0;
		JsonDiffer jsonDiffer = new JsonDiffer();
		for (int i = 0; i < tokens2.Count; i++)
		{
			JToken jToken = tokens2[i];
			string key = jToken.SelectToken("id")?.ToString();
			if (!string.IsNullOrEmpty(key) && tokens.FirstOrDefault((JToken tkn) => tkn["id"]?.ToString() == key) == null)
			{
				JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
				{
					Operation = OperationType.Add,
					Value = jToken,
					Id = key,
					Path = GetPath(jToken),
					Index = i
				};
				NormalizeAndAddFullPath(jsonDiffInfo, (string.Format("{0}", jToken["FullPath"]) ?? "").ToString(), new JObject());
				topLevelDiffs.Add(jsonDiffInfo);
			}
		}
		int i2;
		for (i2 = 0; i2 < tokens.Count; i2++)
		{
			JToken jToken2 = tokens[i2];
			string key2 = jToken2.SelectToken("id")?.ToString();
			if (string.IsNullOrEmpty(key2))
			{
				continue;
			}
			if (topLevelDiffs.Find((JsonDiffInfo jd) => jd.Operation == OperationType.Add && jd.Index == i2) != null)
			{
				num++;
			}
			JToken jToken3 = tokens2.FirstOrDefault((JToken tkn) => tkn["id"]?.ToString() == key2);
			if (jToken3 != null)
			{
				int num2 = tokens2.IndexOf(jToken3);
				bool num3 = num2 != i2 + num;
				PatchDocument patchDocument = jsonDiffer.Diff(jToken2, jToken3, useIdPropertyToDetermineEquality: false);
				if (patchDocument.Operations.Any())
				{
					if (enablePropertyLevelDiffs)
					{
						topLevelDiffs.AddRange(GetJsonDiffInfosFromPatchForToken(patchDocument, key2, num2, jToken2, jToken3));
					}
					else
					{
						JsonDiffInfo jsonDiffInfo2 = new JsonDiffInfo
						{
							Id = key2,
							Path = GetPath(jToken3),
							Index = num2
						};
						jsonDiffInfo2.Operation = OperationType.Replace;
						jsonDiffInfo2.Value = jToken3;
						NormalizeAndAddFullPath(jsonDiffInfo2, string.Format("{0}", jToken3["FullPath"]), jToken2);
						topLevelDiffs.Add(jsonDiffInfo2);
					}
				}
				if (num3)
				{
					AddMoveDiffInfos(topLevelDiffs, jToken3, jToken2, key2, num2);
				}
			}
			else
			{
				JsonDiffInfo jsonDiffInfo3 = new JsonDiffInfo
				{
					Id = key2,
					Path = GetPath(jToken2),
					Operation = OperationType.Remove
				};
				NormalizeAndAddFullPath(jsonDiffInfo3, (string.Format("{0}", jToken2["FullPath"]) ?? "").ToString(), jToken2);
				topLevelDiffs.Add(jsonDiffInfo3);
				num--;
			}
		}
		logger.Debug("Diffs found - " + JsonConvert.SerializeObject(topLevelDiffs));
		return topLevelDiffs.OrderBy((JsonDiffInfo info) => info.Index).ToList();
	}

	private void AddMoveDiffInfos(List<JsonDiffInfo> jsonDiffInfos, JToken newToken, JToken oldToken, string key, int newIndex)
	{
		if (isEnableFullPath)
		{
			bool flag = jsonDiffInfos.Any((JsonDiffInfo x) => x.Id == key && x.Path.EndsWith("parentId"));
			if (isEnableFullPath & flag)
			{
				jsonDiffInfos.Add(new JsonDiffInfo
				{
					Id = key,
					Path = GetPath(newToken),
					Index = -1,
					Operation = OperationType.Remove,
					FullPath = NormalizePath(string.Format("{0}", oldToken["FullPath"])),
					Value = null,
					PreValue = oldToken
				});
				jsonDiffInfos.Add(new JsonDiffInfo
				{
					Id = key,
					Path = GetPath(newToken),
					Index = -1,
					Operation = OperationType.Add,
					FullPath = NormalizePath(string.Format("{0}", newToken["FullPath"])),
					Value = newToken,
					PreValue = null
				});
			}
		}
		else
		{
			JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
			{
				Id = key,
				Path = GetPath(newToken),
				Index = newIndex
			};
			jsonDiffInfo.Operation = OperationType.Move;
			NormalizeAndAddFullPath(jsonDiffInfo, string.Format("{0}", newToken["FullPath"]), oldToken);
			jsonDiffInfos.Add(jsonDiffInfo);
		}
	}

	internal List<JsonDiffInfo> GetJsonDiffInfosFromPatchForToken(PatchDocument patchDoc, string tokenKey, int tokenNewIndex, JToken oldJson, JToken newJson)
	{
		JsonDiffer jdp = new JsonDiffer();
		string text = string.Format("{0}", newJson["FullPath"]);
		List<JsonDiffInfo> list = new List<JsonDiffInfo>();
		foreach (Operation operation in patchDoc.Operations)
		{
			if (isEnableFullPath && operation.Path.ToString().Contains("FullPath"))
			{
				continue;
			}
			if (operation is ReplaceOperation replaceOperation && replaceOperation.Value is JArray && new JsonPointer(replaceOperation.Path.ToString()).Find(oldJson) is JArray)
			{
				RerunDiffsForArrayValue(tokenKey, tokenNewIndex, oldJson, newJson, jdp, list, replaceOperation);
				continue;
			}
			JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
			{
				Id = tokenKey,
				Path = operation.Path.ToString(),
				Index = tokenNewIndex
			};
			if (!(operation is ReplaceOperation replaceOperation2))
			{
				if (!(operation is RemoveOperation))
				{
					if (operation is AddOperation addOperation)
					{
						jsonDiffInfo.Operation = OperationType.Add;
						jsonDiffInfo.Value = addOperation.Value;
					}
				}
				else
				{
					jsonDiffInfo.Operation = OperationType.Remove;
				}
			}
			else
			{
				jsonDiffInfo.Operation = OperationType.Replace;
				jsonDiffInfo.Value = replaceOperation2.Value;
			}
			NormalizeAndAddFullPath(jsonDiffInfo, text + jsonDiffInfo.Path, oldJson, findPrevalueUsingPath: true);
			list.Add(jsonDiffInfo);
		}
		return list;
	}

	internal void RerunDiffsForArrayValue(string tokenKey, int tokenNewIndex, JToken oldJson, JToken newJson, JsonDiffer jdp, List<JsonDiffInfo> jsonDiffInfos, ReplaceOperation replaceOperation)
	{
		if (!(replaceOperation.Value is JArray) || replaceOperation == null)
		{
			return;
		}
		string text = string.Format("{0}", newJson["FullPath"]);
		string text2 = replaceOperation.Path.ToString();
		JsonPointer jsonPointer = new JsonPointer(text2);
		JToken jToken = jsonPointer.Find(oldJson);
		JToken jToken2 = jsonPointer.Find(newJson);
		if (!(jToken is JArray) || !(jToken2 is JArray))
		{
			return;
		}
		JToken[] array = jToken.ToArray();
		JToken[] array2 = jToken2.ToArray();
		int num = Math.Min(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			foreach (Operation operation in jdp.Diff(array[i], array2[i], useIdPropertyToDetermineEquality: false).Operations)
			{
				JToken oldJson2 = oldJson;
				string text3 = operation.Path.ToString();
				string path = ((text3 == "/") ? $"{text2}/{i}" : $"{text2}/{i}{text3}");
				JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
				{
					Id = tokenKey,
					Path = path,
					Index = tokenNewIndex
				};
				if (!(operation is ReplaceOperation replaceOperation2))
				{
					if (!(operation is RemoveOperation))
					{
						if (operation is AddOperation addOperation)
						{
							oldJson2 = new JObject();
							jsonDiffInfo.Operation = OperationType.Add;
							jsonDiffInfo.Value = addOperation.Value;
						}
					}
					else
					{
						jsonDiffInfo.Operation = OperationType.Remove;
						oldJson2 = new JsonPointer(operation.Path.ToString()).Find(array[i]);
					}
				}
				else
				{
					jsonDiffInfo.Operation = OperationType.Replace;
					jsonDiffInfo.Value = replaceOperation2.Value;
				}
				NormalizeAndAddFullPath(jsonDiffInfo, text + jsonDiffInfo.Path, oldJson2);
				jsonDiffInfos.Add(jsonDiffInfo);
			}
		}
		JToken[] array3 = array.Skip(num).ToArray();
		foreach (JToken oldJson3 in array3)
		{
			JsonDiffInfo jsonDiffInfo2 = new JsonDiffInfo
			{
				Id = tokenKey,
				Path = $"{text2}/{num}",
				Index = tokenNewIndex
			};
			jsonDiffInfo2.Operation = OperationType.Remove;
			jsonDiffInfo2.Value = null;
			NormalizeAndAddFullPath(jsonDiffInfo2, text + jsonDiffInfo2.Path, oldJson3);
			jsonDiffInfos.Add(jsonDiffInfo2);
		}
		JToken[] array4 = array2.Skip(num).ToArray();
		for (int k = 0; k < array4.Length; k++)
		{
			JsonDiffInfo jsonDiffInfo3 = new JsonDiffInfo
			{
				Id = tokenKey,
				Path = $"{text2}/{k + num}",
				Index = tokenNewIndex
			};
			jsonDiffInfo3.Operation = OperationType.Add;
			jsonDiffInfo3.Value = array4[k];
			NormalizeAndAddFullPath(jsonDiffInfo3, text + jsonDiffInfo3.Path, new JObject());
			jsonDiffInfos.Add(jsonDiffInfo3);
		}
	}

	public void ApplyDiffs(string type, ref JToken json, List<JsonDiffInfo> diffs)
	{
		if (diffs == null || !diffs.Any())
		{
			return;
		}
		JArray rootToken = GetRootToken(json);
		JsonPatcher jsonPatcher = new JsonPatcher();
		foreach (IGrouping<string, JsonDiffInfo> diffGroup in from x in diffs
			group x by x.Id)
		{
			if (IsRevertEvent(type, diffGroup))
			{
				continue;
			}
			foreach (JsonDiffInfo item in diffGroup)
			{
				if (!string.IsNullOrEmpty(diffGroup.Key))
				{
					JToken jToken = rootToken.FirstOrDefault((JToken tkn) => tkn["id"]?.ToString() == diffGroup.Key);
					switch (item.Operation)
					{
					case OperationType.Move:
						if (jToken != null)
						{
							UpdateArray(rootToken, jToken, jToken, item.Index);
						}
						break;
					case OperationType.Add:
						if (jToken == null && item.Path.IndexOf("/") == -1)
						{
							UpdateArray(rootToken, jToken, item.Value, item.Index);
							break;
						}
						if (jToken != null)
						{
							if (!ApplyAddDiffsForArrays(item, jToken, jsonPatcher) && !ApplyAddDiffsForObjects(item, jToken, jsonPatcher))
							{
								jToken = ApplyPatchOperation(jsonPatcher, item, jToken);
							}
							break;
						}
						logger.Debug("Token for id = '" + diffGroup.Key + "' not found, this diff with Path= '" + item.Path + "' cannot be applied");
						break;
					case OperationType.Remove:
						if (jToken != null)
						{
							if (item.Path == "components")
							{
								rootToken.Remove(jToken);
							}
							else
							{
								jToken = ApplyPatchOperation(jsonPatcher, item, jToken);
							}
						}
						break;
					case OperationType.Replace:
						jToken = ApplyPatchOperation(jsonPatcher, item, jToken);
						break;
					}
				}
				else
				{
					json = ApplyPatchOperation(jsonPatcher, item, json);
				}
			}
		}
	}

	private static bool IsRevertEvent(string type, IGrouping<string, JsonDiffInfo> diffGroup)
	{
		if (type == "events")
		{
			return diffGroup.Any((JsonDiffInfo di) => di.Path == "/mode" && di.Value.ToString().Equals("R", StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	private static JToken ApplyPatchOperation(JsonPatcher jsonPatcher, JsonDiffInfo diffInfo, JToken token)
	{
		if (token != null)
		{
			Operation patchDocument = GetPatchDocument(diffInfo, token);
			if (patchDocument != null)
			{
				jsonPatcher.Patch(ref token, new PatchDocument(patchDocument));
			}
		}
		return token;
	}

	public void ApplyDiffs_Orig(string type, ref JToken json, List<JsonDiffInfo> diffs)
	{
		if (diffs == null || !diffs.Any())
		{
			return;
		}
		JsonPatcher jsonPatcher = new JsonPatcher();
		foreach (JsonDiffInfo diff in diffs)
		{
			if (!string.IsNullOrEmpty(diff.Id))
			{
				JArray jArray = json.SelectToken(diff.Path) as JArray;
				if (jArray == null)
				{
					jArray = new JArray();
					json[diff.Path] = jArray;
				}
				JToken jToken = jArray.FirstOrDefault((JToken tkn) => tkn["id"].ToString() == diff.Id);
				switch (diff.Operation)
				{
				case OperationType.Move:
					if (jToken != null)
					{
						UpdateArray(jArray, jToken, jToken, diff.Index);
					}
					break;
				case OperationType.Add:
					UpdateArray(jArray, jToken, diff.Value, diff.Index);
					break;
				case OperationType.Remove:
					if (jToken != null)
					{
						jArray.Remove(jToken);
					}
					break;
				case OperationType.Replace:
					if (jToken != null && (type != "events" || !"R".Equals(diff.Value["mode"].Value<string>(), StringComparison.OrdinalIgnoreCase)))
					{
						UpdateArray(jArray, jToken, diff.Value, jArray.IndexOf(jToken));
					}
					break;
				}
			}
			else
			{
				Operation patchDocument = GetPatchDocument(diff, json);
				if (patchDocument != null)
				{
					jsonPatcher.Patch(ref json, new PatchDocument(patchDocument));
				}
			}
		}
	}

	internal static bool ApplyAddDiffsForObjects(JsonDiffInfo diffInfo, JToken token, JsonPatcher jsonPatcher)
	{
		if (diffInfo.Operation != OperationType.Add || !(diffInfo.Value is JObject))
		{
			return false;
		}
		JsonPointer jsonPointerForPath = GetJsonPointerForPath(diffInfo.Path, token);
		if (jsonPointerForPath != null)
		{
			JToken target = jsonPointerForPath.Find(token);
			IEnumerable<Operation> source = new JsonDiffer().Diff(target, diffInfo.Value, useIdPropertyToDetermineEquality: false).Operations.Where((Operation o) => !(o is RemoveOperation));
			jsonPatcher.Patch(ref target, new PatchDocument(source.ToArray()));
			return true;
		}
		return false;
	}

	internal static bool ApplyAddDiffsForArrays(JsonDiffInfo diffInfo, JToken token, JsonPatcher jsonPatcher)
	{
		if (diffInfo.Operation != OperationType.Add)
		{
			return false;
		}
		if (diffInfo.Value is JArray)
		{
			int? countIfArray = GetCountIfArray(diffInfo.Path, token);
			if (countIfArray.HasValue)
			{
				JArray jArray = (JArray)diffInfo.Value;
				for (int i = 0; i < jArray.Count; i++)
				{
					Operation patchDocument = GetPatchDocument(diffInfo.Path + "/" + (i + countIfArray), jArray[i], OperationType.Add, token);
					jsonPatcher.Patch(ref token, new PatchDocument(patchDocument));
				}
				return true;
			}
			ApplyPatchOperation(jsonPatcher, diffInfo, token);
			return true;
		}
		try
		{
			if (int.TryParse(diffInfo.Path.Substring(diffInfo.Path.LastIndexOf("/") + 1), out var result))
			{
				JToken jToken = GetParentToken(new JsonPointer(diffInfo.Path), token, out var parentPointer);
				if (jToken == null)
				{
					string text = parentPointer.ToString();
					string key = text.Substring(text.LastIndexOf("/") + 1);
					JToken parentToken = GetParentToken(parentPointer, token, out var _);
					if (parentToken != null)
					{
						jToken = (parentToken[key] = new JArray());
					}
				}
				UpdateArray((JArray)jToken, null, diffInfo.Value, result);
				return true;
			}
		}
		catch (ArgumentException)
		{
			return false;
		}
		return false;
	}

	private static JToken GetParentToken(JsonPointer currentPointer, JToken token, out JsonPointer parentPointer)
	{
		parentPointer = currentPointer.ParentPointer;
		if (parentPointer == null)
		{
			return null;
		}
		try
		{
			return parentPointer.Find(token);
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static int? GetCountIfArray(string path, JToken json)
	{
		try
		{
			JToken jToken = new JsonPointer(path).Find(json);
			if (jToken != null && jToken is JArray)
			{
				return ((JArray)jToken).Count;
			}
			return null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static void UpdateArray(JArray array, JToken oldToken, JToken modifiedToken, int index)
	{
		if (oldToken != null)
		{
			array.Remove(oldToken);
		}
		if (array.Count > index)
		{
			array.Insert(index, modifiedToken);
		}
		else
		{
			array.Add(modifiedToken);
		}
	}

	private List<JToken> GetTokens(JToken token)
	{
		string path = "$.*.[*]";
		if (token is JArray)
		{
			path = "$.[*]";
		}
		return token.SelectTokens(path, errorWhenNoMatch: false).ToList();
	}

	private JArray GetRootToken(JToken token)
	{
		string path = ((token is JArray) ? "" : "components");
		return token.SelectToken(path) as JArray;
	}

	private JObject GetToplevelObject(JToken json)
	{
		List<JToken> list = json.Where((JToken p) => (p is JProperty jProperty && !(jProperty.Value is JArray)) ? true : false).ToList();
		JObject jObject = new JObject();
		foreach (JToken item in list)
		{
			jObject.Add(item);
		}
		return jObject;
	}

	private string GetPath(JToken token)
	{
		if (token.Parent.Type == JTokenType.Array)
		{
			if (token.Parent.Parent != null)
			{
				return token.Parent.Parent.Path;
			}
			return token.Parent.Path;
		}
		return token.Path;
	}

	private List<JsonDiffInfo> GetTopLevelDiffs(JToken oldJson, JToken newJson, string parentPath = "")
	{
		JObject toplevelObject = GetToplevelObject(oldJson);
		JObject toplevelObject2 = GetToplevelObject(newJson);
		PatchDocument patchDocument = new JsonDiffer().Diff(toplevelObject, toplevelObject2, useIdPropertyToDetermineEquality: false);
		List<JsonDiffInfo> list = new List<JsonDiffInfo>();
		foreach (Operation operation in patchDocument.Operations)
		{
			if (!enablePropertyLevelDiffs && operation.Path.ToString().Contains("componentModel"))
			{
				string text = (from x in operation.Path.ToString().Split(new char[1] { '/' })
					where !string.IsNullOrEmpty(x)
					select x).FirstOrDefault();
				if (text != null)
				{
					AddComponentModelDiff(list, text, operation, newJson, parentPath, oldJson);
				}
				continue;
			}
			string path = ((parentPath.Contains("Pages") || parentPath.Contains("Layout")) ? (parentPath + operation.Path) : operation.Path.ToString().Substring(1));
			JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
			{
				Path = operation.Path.ToString(),
				Id = string.Empty
			};
			if (!(operation is ReplaceOperation replaceOperation))
			{
				if (!(operation is RemoveOperation))
				{
					if (operation is AddOperation addOperation)
					{
						jsonDiffInfo.Value = addOperation.Value;
						jsonDiffInfo.Operation = OperationType.Add;
					}
				}
				else
				{
					jsonDiffInfo.Operation = OperationType.Remove;
				}
			}
			else
			{
				jsonDiffInfo.Value = replaceOperation.Value;
				jsonDiffInfo.Operation = OperationType.Replace;
			}
			NormalizeAndAddFullPath(jsonDiffInfo, path, oldJson);
			list.Add(jsonDiffInfo);
		}
		return list;
	}

	private void AddComponentModelDiff(List<JsonDiffInfo> jsonDiffInfos, string path, Operation patch, JToken newJson, string parentPath, JToken oldJson)
	{
		if (jsonDiffInfos.Any((JsonDiffInfo x) => x.Path.Equals(path)))
		{
			return;
		}
		JsonDiffInfo jsonDiffInfo = new JsonDiffInfo
		{
			Path = "/" + path
		};
		NormalizeAndAddFullPath(jsonDiffInfo, parentPath + "/" + path, oldJson);
		if (!(patch is ReplaceOperation))
		{
			if (!(patch is RemoveOperation))
			{
				if (patch is AddOperation)
				{
					jsonDiffInfo.Operation = OperationType.Add;
				}
			}
			else
			{
				jsonDiffInfo.Operation = OperationType.Remove;
			}
		}
		else
		{
			jsonDiffInfo.Operation = OperationType.Replace;
		}
		jsonDiffInfo.Value = newJson[path];
		jsonDiffInfos.Add(jsonDiffInfo);
	}

	private static Operation GetPatchDocument(JsonDiffInfo diffInfo, JToken targetToken)
	{
		return GetPatchDocument(diffInfo.Path, diffInfo.Value, diffInfo.Operation, targetToken);
	}

	private static Operation GetPatchDocument(string diffPath, JToken diffValue, OperationType diffOperation, JToken targetToken)
	{
		Operation result = null;
		switch (diffOperation)
		{
		case OperationType.Add:
		{
			JsonPointer jsonPointer = new JsonPointer(diffPath);
			if (GetParentToken(jsonPointer, targetToken, out var _) != null)
			{
				result = new AddOperation(jsonPointer, diffValue);
			}
			break;
		}
		case OperationType.Remove:
		{
			JsonPointer jsonPointerForPath2 = GetJsonPointerForPath(diffPath, targetToken);
			if (jsonPointerForPath2 != null)
			{
				result = new RemoveOperation(jsonPointerForPath2);
			}
			break;
		}
		case OperationType.Replace:
		{
			JsonPointer jsonPointerForPath = GetJsonPointerForPath(diffPath, targetToken);
			if (jsonPointerForPath != null)
			{
				result = new ReplaceOperation(jsonPointerForPath, diffValue);
			}
			break;
		}
		}
		return result;
	}

	private static JsonPointer GetJsonPointerForPath(string path, JToken json)
	{
		try
		{
			if (!string.IsNullOrEmpty(path) && path.EndsWith("/", StringComparison.InvariantCultureIgnoreCase))
			{
				path = path.TrimEnd(new char[1] { '/' });
			}
			JsonPointer jsonPointer = new JsonPointer(path);
			return (jsonPointer.Find(json) != null) ? jsonPointer : null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private void NormalizeAndAddFullPath(JsonDiffInfo jsonDiffInfo, string path, JToken oldJson, bool findPrevalueUsingPath = false)
	{
		if (!isEnableFullPath)
		{
			return;
		}
		jsonDiffInfo.FullPath = NormalizePath(path);
		if (!findPrevalueUsingPath && jsonDiffInfo.Operation == OperationType.Remove)
		{
			jsonDiffInfo.PreValue = oldJson;
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = jsonDiffInfo.Path.Split(new char[1] { '/' });
		foreach (string text in array)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (int.TryParse(text, out var result))
				{
					stringBuilder.Append($"[{result}].");
				}
				else
				{
					stringBuilder.Append(text + ".");
				}
			}
		}
		string text2 = stringBuilder.ToString();
		jsonDiffInfo.PreValue = oldJson.SelectToken(text2.Remove(text2.Length - 1));
	}

	private string NormalizePath(string path)
	{
		return path.Replace(".", "/").Replace("[", "/").Replace("]", "");
	}
}
