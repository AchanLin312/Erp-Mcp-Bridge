using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Epicor.MetaFX.Core.JSONCConverter;

internal class JSONCConverters
{
	private string[] readLineForOld;

	private int commentCounter;

	private static Stack<BraceTypes> BraceStack = new Stack<BraceTypes>();

	private const string SINGLE_LINE = "inline";

	private const string MULTI_LINE = "multi";

	private string[] receivedJSONStringArray;

	private List<string> upgradedNewArrayWithComment;

	public JSONCConverters(string[] content)
	{
		readLineForOld = content;
		upgradedNewArrayWithComment = new List<string>();
	}

	public string[] Convert()
	{
		for (int i = 1; i < readLineForOld.Length - 1; i++)
		{
			string[] array = readLineForOld[i].Split(new char[1] { ':' });
			if (array.Length > 1)
			{
				if (array[1].Trim().Equals("{"))
				{
					BraceStack.Push(BraceTypes.Curly);
				}
				else if (array[1].Trim().Equals("["))
				{
					BraceStack.Push(BraceTypes.Square);
				}
			}
			else if (readLineForOld[i].Trim().Equals("["))
			{
				BraceStack.Push(BraceTypes.Square);
			}
			else if (readLineForOld[i].Trim().Equals("{"))
			{
				BraceStack.Push(BraceTypes.Curly);
			}
			string text = readLineForOld[i].Replace(',', ' ').Trim();
			if (text.Equals("}"))
			{
				if (BraceStack.Count > 0)
				{
					BraceStack.Pop();
				}
			}
			else if (text.Equals("]") && BraceStack.Count > 0)
			{
				BraceStack.Pop();
			}
			if (readLineForOld[i].Contains("/*"))
			{
				string value = ExtractOutComment(readLineForOld[i], "/*", "*/");
				if (BraceStack.Count == 0)
				{
					readLineForOld[i] = FormCommentAsProperty(value, "multi");
					continue;
				}
				switch (BraceStack.Peek())
				{
				case BraceTypes.Square:
					if (readLineForOld[i - 1].Trim().Equals("}"))
					{
						readLineForOld[i] = FormCommentAsObject(value, "multi", flag: true);
					}
					else
					{
						readLineForOld[i] = FormCommentAsObject(value, "multi");
					}
					break;
				case BraceTypes.Curly:
					if (readLineForOld[i].Trim().IndexOf("/*") == 0)
					{
						readLineForOld[i] = (readLineForOld[i - 1].Trim().EndsWith("}") ? ("," + FormCommentAsProperty(value, "multi")) : FormCommentAsProperty(value, "multi"));
					}
					else if (readLineForOld[i].Split(new char[1] { ':' }).Length > 1 && readLineForOld[i].Contains("/*"))
					{
						SanitizedObject sanitizedObject = ExtractKeyAndComment(readLineForOld[i], "/*", string.Empty);
						string key = sanitizedObject.SanitizedValue.Split(new char[1] { ':' })[0].Replace('"', ' ').Trim();
						string text2 = FormCommentAsProperty(sanitizedObject.Comment, "multi", key);
						string empty = string.Empty;
						sanitizedObject.SanitizedValue.Trim().IndexOf(',');
						empty = text2 + sanitizedObject.SanitizedValue;
						readLineForOld[i] = empty;
					}
					break;
				}
			}
			else if (readLineForOld[i].Trim().IndexOf('/') == 0)
			{
				SanitizedObject sanitizedObject2 = ExtractKeyAndComment(readLineForOld[i], "//", string.Empty);
				string text3 = (((readLineForOld[i - 1].Trim().EndsWith("},") && readLineForOld[i + 1].Trim().StartsWith("{")) || (readLineForOld[i - 1].Trim().EndsWith("[") && readLineForOld[i + 1].Trim().StartsWith("{")) || (readLineForOld[i - 1].Trim().EndsWith("[") && readLineForOld[i + 1].Trim().StartsWith("]"))) ? FormCommentAsObject(sanitizedObject2.Comment, "inline") : FormCommentAsProperty(sanitizedObject2.Comment, "inline"));
				if (readLineForOld[i - 1].Trim().EndsWith("}") && readLineForOld[i + 1].Trim().StartsWith("]"))
				{
					text3 = FormCommentAsObject(sanitizedObject2.Comment, "inline", flag: true);
				}
				string empty2 = string.Empty;
				empty2 = text3;
				readLineForOld[i] = empty2;
				if (readLineForOld[i + 1].Trim().EndsWith("},"))
				{
					readLineForOld[i] = "," + readLineForOld[i];
				}
			}
			else if (readLineForOld[i].Split(new char[1] { ':' }).Length > 1 && !readLineForOld[i].Contains("$schema") && !readLineForOld[i].Contains("http") && readLineForOld[i].Contains("//"))
			{
				SanitizedObject sanitizedObject3 = ExtractKeyAndComment(readLineForOld[i], "//", string.Empty);
				string key2 = sanitizedObject3.SanitizedValue.Split(new char[1] { ':' })[0].Replace('"', ' ').Trim();
				string text4 = FormCommentAsProperty(sanitizedObject3.Comment, "inline", key2);
				string empty3 = string.Empty;
				sanitizedObject3.SanitizedValue.Trim().IndexOf(',');
				empty3 = text4 + sanitizedObject3.SanitizedValue;
				readLineForOld[i] = empty3;
			}
		}
		return readLineForOld;
	}

	public string[] ConvertToJSONC(string[] content)
	{
		receivedJSONStringArray = content;
		int num = receivedJSONStringArray.Length;
		for (int i = 0; i < num; i++)
		{
			if (i != 0 && i < num - 2)
			{
				if (receivedJSONStringArray[i + 1].Contains("_comment_"))
				{
					string obj = receivedJSONStringArray[i + 2];
					int length = obj.Trim().Length;
					if (Enumerable.Contains(obj, '}') && length <= 2)
					{
						continue;
					}
				}
				if (receivedJSONStringArray[i - 1].Contains("_comment_") && receivedJSONStringArray[i].Contains("}"))
				{
					continue;
				}
			}
			if (receivedJSONStringArray[i].Contains("_comment_"))
			{
				string[] array = receivedJSONStringArray[i].Trim().Split(new char[1] { '_' });
				if (array.Length > 3)
				{
					if (array[3].ToString().Contains("multi") || (receivedJSONStringArray[i + 1].Trim() == "}" && receivedJSONStringArray[i - 1].Trim() == "{"))
					{
						string text = receivedJSONStringArray[i].Split(new char[1] { ':' })[1].Trim();
						string text2 = "/*" + text.Replace("\"", string.Empty) + "*/";
						upgradedNewArrayWithComment.Add(text2.Replace(",", string.Empty));
						continue;
					}
					if (array[3].ToString().Contains("inline") || (receivedJSONStringArray[i + 1].Trim() == "}" && receivedJSONStringArray[i - 1].Trim() == "{"))
					{
						string text3 = receivedJSONStringArray[i].Split(new char[1] { ':' })[1].Replace('"', ' ');
						upgradedNewArrayWithComment.Add(text3.Replace(',', ' '));
						continue;
					}
				}
				if (array.Length == 3)
				{
					string value = array[2].Split(new char[1] { ':' })[0].Replace('"', ' ').Trim().ToString();
					if (i != 0 && i < num - 1 && receivedJSONStringArray[i + 1].Trim().Split(new char[1] { ':' })[0].Replace('"', ' ').Trim().Equals(value))
					{
						string text4 = receivedJSONStringArray[i].Split(new char[1] { ':' })[1].Trim().Replace('"', ' ').Trim()
							.Replace(',', ' ');
						string item = receivedJSONStringArray[i + 1] + text4;
						upgradedNewArrayWithComment.Add(item);
						i++;
						continue;
					}
				}
			}
			upgradedNewArrayWithComment.Add(receivedJSONStringArray[i]);
		}
		return upgradedNewArrayWithComment.ToArray();
	}

	public SanitizedObject ExtractKeyAndComment(string strSource, string strStart, string strEnd)
	{
		SanitizedObject sanitizedObject = new SanitizedObject();
		if (strSource.Contains(strStart) && strSource.Contains(strEnd))
		{
			int num = strSource.Trim().IndexOf(strStart);
			int i = num;
			string text = string.Empty;
			char[] array = strSource.Trim().ToCharArray();
			for (int length = strSource.Trim().Length; length > i; i++)
			{
				text += array[i];
			}
			sanitizedObject.Comment = text;
			string text2 = string.Empty;
			for (int j = 0; j < num; j++)
			{
				text2 += array[j];
			}
			sanitizedObject.SanitizedValue = text2;
		}
		return sanitizedObject;
	}

	public bool Validate(string content)
	{
		string pattern = "^( *}) *\\/\\*.*\\*\\/(,.*)$";
		MatchCollection matchCollection = Regex.Matches(content, pattern, RegexOptions.Multiline);
		if (matchCollection.Count > 0)
		{
			throw new Exception($"Schema upgrade error - When comments given at the object end index is not supported. See example10 of document. Occurrence(s) {matchCollection.Count}");
		}
		pattern = "\\/\\*(.|[\\r\\n])*?\\*\\/";
		matchCollection = Regex.Matches(content, pattern, RegexOptions.Multiline);
		if (matchCollection.Count > 0)
		{
			int num = 0;
			foreach (Match item in matchCollection)
			{
				if (item.Value.Contains("{") || item.Value.Contains("}"))
				{
					num++;
				}
			}
			if (num > 0)
			{
				throw new Exception($"Schema upgrade error - When multiple lines are commented. See example18 of document. Occurrence(s) {num}");
			}
		}
		pattern = " \\/\\/(.)*";
		matchCollection = Regex.Matches(content, pattern, RegexOptions.Multiline);
		if (matchCollection.Count > 0)
		{
			int num2 = 0;
			foreach (Match item2 in matchCollection)
			{
				char c = '"';
				if (item2.Value.Contains($"{c}"))
				{
					num2++;
				}
			}
			if (num2 > 0)
			{
				throw new Exception($"Schema upgrade error - When comment includes double quotes is not supported. See example9 of document. Occurrence(s) {num2}");
			}
		}
		pattern = "\\/\\*(.|[\\r\\n])*?\\*\\/";
		matchCollection = Regex.Matches(content, pattern, RegexOptions.Multiline);
		if (matchCollection.Count > 0)
		{
			int num3 = 0;
			foreach (Match item3 in matchCollection)
			{
				char c2 = '"';
				string value = "schemas.developer.epicor.com";
				if (!item3.Value.Contains(value) && item3.Value.Contains($"{c2}"))
				{
					num3++;
				}
			}
			if (num3 > 0)
			{
				throw new Exception($"Schema upgrade error - When comment includes double quotes is not supported. See example9 of document. Occurrence(s) {num3}");
			}
		}
		pattern = "\\[(.|[\\r\\n])*?]";
		matchCollection = Regex.Matches(content, pattern, RegexOptions.Multiline);
		if (matchCollection.Count > 0)
		{
			int num4 = 0;
			foreach (Match item4 in matchCollection)
			{
				string[] array = item4.Value.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Trim().StartsWith("//") && i + 1 < array.Length && array[i + 1].Trim().StartsWith("//"))
					{
						flag = true;
					}
				}
				if (flag)
				{
					num4++;
				}
			}
			if (num4 > 0)
			{
				throw new Exception($"Schema upgrade error - When an array has multiple comments. See example4 of document. Occurrence(s) {num4}");
			}
		}
		return true;
	}

	private string FormCommentAsObject(string value, string type, bool flag = false)
	{
		if (!flag)
		{
			string result = "{\"_comment_" + commentCounter + "_" + type + "\":\"" + value + "\"},";
			commentCounter++;
			return result;
		}
		string result2 = ",{\"_comment_" + commentCounter + "_" + type + "\":\"" + value + "\"},";
		commentCounter++;
		return result2;
	}

	private string FormCommentAsProperty(string value, string type)
	{
		string result = "\"_comment_" + commentCounter + "_" + type + "\":\"" + value + "\",";
		commentCounter++;
		return result;
	}

	private string FormCommentAsProperty(string value, string type, string key)
	{
		string result = "\"_comment_" + key + "\":\"" + value + "\",";
		commentCounter++;
		return result;
	}

	private string ExtractOutComment(string strSource, string strStart, string strEnd)
	{
		int num = strSource.IndexOf(strStart, 0) + strStart.Length;
		int num2 = strSource.IndexOf(strEnd, num);
		return strSource.Substring(num, num2 - num);
	}
}
