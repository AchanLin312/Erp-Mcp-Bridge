using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator;

public class ConfiguratorHelper
{
	public int GetPageNumber(string name)
	{
		return Convert.ToInt32(name?.Replace("Page", ""));
	}

	public string GetControlId(JToken control)
	{
		return control["model"]?["id"]?.ToString();
	}

	public List<JToken> GetAllExecuteCharpEventActions(JArray events)
	{
		events = new JArray(events.Where((JToken elem) => filterDeletedEvents(elem)));
		IEnumerable<JToken> first = events.SelectTokens("$..actions", errorWhenNoMatch: false);
		IEnumerable<JToken> second = events.SelectTokens("$..onSuccess", errorWhenNoMatch: false);
		return (from action in Enumerable.Union(second: events.SelectTokens("$..onFailure", errorWhenNoMatch: false), first: first.Union(second)).SelectMany((JToken action) => action)
			where action is JObject && action["type"]?.ToString() == "execute-csharp"
			select action).ToList();
	}

	private bool filterDeletedEvents(JToken elem)
	{
		if (elem["mode"] == null)
		{
			return true;
		}
		return elem["mode"].ToString() != "D";
	}

	public string UpdateExpression(string expression, List<string> methods, out bool modified)
	{
		modified = false;
		if (expression.Contains("await"))
		{
			return expression;
		}
		List<string> list = new List<string>();
		list.Add("PCLookUp");
		list.Add("UDMethods");
		if (methods != null)
		{
			list.AddRange(methods);
		}
		bool modified2 = false;
		foreach (string item in list)
		{
			expression = UpdateExpression(item, expression, out modified2);
			if (modified2)
			{
				modified = true;
			}
		}
		return expression;
	}

	private string UpdateExpression(string pattern, string expression, out bool modified)
	{
		if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(expression))
		{
			modified = false;
			return expression;
		}
		string text = Regex.Escape(pattern);
		bool[] array = BuildInQuotesMap(expression);
		MatchCollection matchCollection = Regex.Matches(expression, "\\b" + text + "\\b");
		modified = matchCollection.Count > 0;
		List<(string, string, int)> list = new List<(string, string, int)>();
		foreach (Match item2 in matchCollection)
		{
			if (item2 == null)
			{
				continue;
			}
			int index = item2.Index;
			int length = item2.Length;
			if (IsRangeInsideQuotes(array, index, length))
			{
				continue;
			}
			int num = index + length;
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = string.Empty;
			for (int i = num; i < expression.Length && !array[i]; i++)
			{
				char c = expression[i];
				stringBuilder.Append(c);
				if (c == ')')
				{
					if (i + 1 < expression.Length && !array[i + 1])
					{
						text2 = expression[i + 1].ToString();
					}
					break;
				}
			}
			string text3 = stringBuilder.ToString();
			string text4 = pattern + text3;
			string item = ((text2 == ".") ? (" ( await " + text4 + " )") : ("   await " + text4 + "  "));
			list.Add((text4, item, index));
		}
		foreach (var item3 in list.OrderByDescending<(string, string, int), int>(((string search, string replace, int index) x) => x.index))
		{
			expression = ReplaceFirst(expression, item3.Item1, item3.Item2, item3.Item3);
		}
		return expression;
	}

	private bool[] BuildInQuotesMap(string s)
	{
		bool[] array = new bool[s.Length];
		bool flag = false;
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] == '"')
			{
				int num = 0;
				int num2 = i - 1;
				while (num2 >= 0 && s[num2] == '\\')
				{
					num++;
					num2--;
				}
				if (num % 2 == 0)
				{
					flag = (array[i] = !flag);
					continue;
				}
			}
			array[i] = flag;
		}
		return array;
	}

	private bool IsRangeInsideQuotes(bool[] inQuotes, int start, int length)
	{
		if (start < 0 || start >= inQuotes.Length)
		{
			return false;
		}
		int num = Math.Min(inQuotes.Length, start + length);
		for (int i = start; i < num; i++)
		{
			if (inQuotes[i])
			{
				return true;
			}
		}
		return false;
	}

	private string ReplaceFirst(string text, string search, string replace, int startIndex)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
		{
			return text;
		}
		if (startIndex < 0 || startIndex >= text.Length)
		{
			return text;
		}
		string text2 = text.Substring(0, startIndex);
		string text3 = text.Substring(startIndex);
		int num = text3.IndexOf(search, StringComparison.Ordinal);
		if (num < 0)
		{
			return text2 + text3;
		}
		return text2 + text3.Substring(0, num) + replace + text3.Substring(num + search.Length);
	}

	private string EmptyTextBetweenQuotes(string value)
	{
		string pattern = "\"[^\"]*\"";
		foreach (object item in Regex.Matches(value, pattern))
		{
			if (item is Match match)
			{
				value = ReplaceFirst(value, match.Value, "\"" + "".PadRight(match.Value.Length - 2) + "\"", 0);
			}
		}
		return value;
	}

	public bool IsEqual(string str1, string str2)
	{
		if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	public string GetConfigId(string viewId, string companyId)
	{
		string text = viewId.Replace("Erp.UICfg.", "");
		if (string.IsNullOrEmpty(companyId))
		{
			return text.Remove(text.IndexOf("_"));
		}
		return text.Remove(text.Length - companyId.Length - 1);
	}

	public string UpdateConfigApp(KeyValuePair<string, string> item, string viewId, string company)
	{
		JObject jObject = JsonConvert.DeserializeObject<JObject>(item.Value);
		foreach (JToken item2 in jObject["patch"].Children())
		{
			if (item2["id"].ToString().Equals("configID", StringComparison.OrdinalIgnoreCase))
			{
				item2["value"] = GetConfigId(viewId, company);
			}
		}
		return jObject.ToString();
	}
}
