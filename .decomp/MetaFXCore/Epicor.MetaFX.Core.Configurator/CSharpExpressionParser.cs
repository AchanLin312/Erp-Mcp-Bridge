using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Epicor.MetaFX.Core.Configurator;

public class CSharpExpressionParser
{
	private const string RETURNTYPE = "return";

	private const string OUTTYPE = "out";

	private const string REFTYPE = "ref";

	public string RemoveComments(string value)
	{
		return InternalRemoveComments(value);
	}

	public static string RemoveTextBetweenQuotes(string value)
	{
		string pattern = "\"[^\"]*\"";
		return Regex.Replace(value, pattern, "\"\"");
	}

	public string ReplaceInExpression(Dictionary<string, string> replaceList, string expression, bool removeTextInQuotes = true, bool removeComments = true)
	{
		if (removeComments)
		{
			expression = RemoveComments(expression);
		}
		if (removeTextInQuotes)
		{
			expression = RemoveTextBetweenQuotes(expression);
		}
		foreach (KeyValuePair<string, string> replace in replaceList)
		{
			if (Regex.IsMatch(expression, replace.Key))
			{
				expression = Regex.Replace(expression, replace.Key, replace.Value);
			}
		}
		return expression;
	}

	private string InternalRemoveComments(string value)
	{
		List<string> list = new List<string>();
		string text = "/\\*(.*?)\\*/";
		string text2 = "//(.*?)(\\r?\\n|$)";
		string text3 = "\"((\\\\[^\\n]|[^\"\\n])*)\"";
		string text4 = "@(\"[^\"]*\")+";
		return Regex.Replace(value, text + "|" + text2 + "|" + text3 + "|" + text4, delegate(Match me)
		{
			if (!me.Value.StartsWith("/*") && !me.Value.StartsWith("//"))
			{
				return me.Value;
			}
			list.Add(me.Groups[1].Value + me.Groups[2].Value);
			return me.Value.StartsWith("//") ? me.Groups[3].Value : "";
		}, RegexOptions.Singleline);
	}

	public string RemoveRefAndOut(string expression)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("\\bref\\s", string.Empty);
		dictionary.Add("\\bout\\s", string.Empty);
		return ReplaceInExpression(dictionary, expression, removeTextInQuotes: false);
	}

	public bool MatchReturn(string expression)
	{
		return Regex.IsMatch(expression, "\\breturn(?:\\s|;|$)");
	}
}
