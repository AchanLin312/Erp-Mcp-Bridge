using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using JsonDiffPatch;
using Newtonsoft.Json.Linq;
using Tavis;

namespace Epicor.MetaFX.Core.Helper;

public class PatchDiff
{
	private readonly ILogger logger;

	public PatchDiff(ILogger logger)
	{
		this.logger = logger;
	}

	public IEnumerable<PatchInfo> GetPatchInfo(string _type, JToken oldjson, JToken newjson)
	{
		string text = "id";
		PatchDocument patchDocument = null;
		JsonDiffer jsonDiffer = new JsonDiffer();
		if (_type == "layout")
		{
			text = "guid";
			patchDocument = jsonDiffer.DiffwithProp(oldjson, newjson, text);
		}
		else
		{
			patchDocument = jsonDiffer.Diff(oldjson, newjson, useIdPropertyToDetermineEquality: true);
		}
		List<PatchInfo> list = new List<PatchInfo>();
		foreach (Operation operation in patchDocument.Operations)
		{
			string text2 = Convert.ToString((operation.Path.Find(oldjson) as JValue)?.Value);
			JToken jToken = GetbasePropties(operation.Path.ToString(), newjson, text);
			string patchPath = Convert.ToString(operation.Path);
			string property = patchProperty(patchPath);
			Regex regex = new Regex(".*%.*%");
			if (!string.IsNullOrEmpty(text2) && regex.IsMatch(text2))
			{
				PatchInfo item = new PatchInfo
				{
					Name = text2,
					Value = Convert.ToString((((ReplaceOperation)operation).Value as JValue)?.Value),
					Id = ((jToken is JArray) ? null : Convert.ToString((jToken[text] as JValue)?.Value)),
					Type = _type,
					Property = property
				};
				list.Add(item);
			}
		}
		logger.Debug("JsonDifference calculated for type " + _type);
		return list;
	}

	public string patchProperty(string patchPath)
	{
		if (patchPath.Contains("model"))
		{
			return patchPath.Remove(0, patchPath.LastIndexOf("model") + 6).Replace('/', '.');
		}
		if (patchPath.Contains("navigation"))
		{
			return patchPath.Remove(0, 1).Replace('/', '.');
		}
		return patchPath.Substring(patchPath.LastIndexOf('/') + 1);
	}

	private JToken GetbasePropties(string path, JToken json, string id)
	{
		int num = path.LastIndexOf('/');
		string text = "";
		if (num != 0)
		{
			text = path.Substring(0, num);
		}
		JToken jToken = new JsonPointer(text).Find(json);
		if (!(jToken is JArray) && jToken[id] == null)
		{
			jToken = GetbasePropties(text, json, id);
		}
		return jToken;
	}
}
