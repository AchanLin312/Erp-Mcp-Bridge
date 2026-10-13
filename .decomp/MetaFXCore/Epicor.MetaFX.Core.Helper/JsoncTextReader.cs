using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Helper;

internal class JsoncTextReader(TextReader reader) : JsonTextReader(reader)
{
	private static MethodInfo JsonTextReaderProcessValueCommaMethod = typeof(JsonTextReader).GetMethod("ProcessValueComma", BindingFlags.Instance | BindingFlags.NonPublic);

	private static MethodInfo JsonTextReaderEatWhitespaceMethod = typeof(JsonTextReader).GetMethod("EatWhitespace", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo JsonTextReaderCharsField = typeof(JsonTextReader).GetField("_chars", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo JsonTextReaderCharPosField = typeof(JsonTextReader).GetField("_charPos", BindingFlags.Instance | BindingFlags.NonPublic);

	private bool _fakeCommentProp;

	private bool _finishedFakeComment;

	private string _commentText = string.Empty;

	public override bool Read()
	{
		bool flag = true;
		if (_fakeCommentProp)
		{
			_fakeCommentProp = false;
			SetToken(JsonToken.String, _commentText);
			flag = true;
			_finishedFakeComment = true;
		}
		else
		{
			if (_finishedFakeComment)
			{
				_finishedFakeComment = false;
				JsonTextReaderEatWhitespaceMethod.Invoke(this, null);
				char[] obj = (char[])JsonTextReaderCharsField.GetValue(this);
				int num = (int)JsonTextReaderCharPosField.GetValue(this);
				if (obj[num] == ',')
				{
					JsonTextReaderProcessValueCommaMethod.Invoke(this, null);
				}
				SetToken(JsonToken.Comment, _commentText);
				SetStateBasedOnCurrent();
			}
			int lineNumber = base.LineNumber;
			flag = base.Read();
			if (base.TokenType == JsonToken.Comment && (base.CurrentState == State.Object || base.CurrentState == State.ObjectStart || (base.CurrentState == State.PostValue && !base.Path.EndsWith("]"))))
			{
				_commentText = base.Value?.ToString() ?? string.Empty;
				SetToken(JsonToken.PropertyName, "_comment_" + ((base.LineNumber == lineNumber) ? "" : "*") + Guid.NewGuid().ToString("D"));
				_fakeCommentProp = true;
			}
		}
		return flag;
	}
}
