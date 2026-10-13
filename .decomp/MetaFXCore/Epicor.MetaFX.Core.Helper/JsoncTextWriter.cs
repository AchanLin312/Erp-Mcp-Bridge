using System.IO;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Helper;

internal class JsoncTextWriter : JsonTextWriter
{
	private bool _rewritingFakeProperty;

	private bool _extraIndentation;

	public JsoncTextWriter(TextWriter writer)
		: base(writer)
	{
	}

	public override void WritePropertyName(string name)
	{
		if (name.StartsWith("_comment_"))
		{
			_rewritingFakeProperty = true;
			_extraIndentation = name.Contains("*");
		}
		else
		{
			base.WritePropertyName(name);
		}
	}

	public override void WriteValue(string value)
	{
		if (_rewritingFakeProperty)
		{
			_rewritingFakeProperty = false;
			if (_extraIndentation)
			{
				WriteIndent();
			}
			else
			{
				WriteIndentSpace();
			}
			WriteComment(value);
		}
		else
		{
			base.WriteValue(value);
		}
	}
}
