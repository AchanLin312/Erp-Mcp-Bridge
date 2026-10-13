using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Epicor.MetaFX.Core.Configurator.Wasm.Templates;

[GeneratedCode("Microsoft.VisualStudio.TextTemplating", "16.0.0.0")]
public class CodeTemplateBase
{
	public class ToStringInstanceHelper
	{
		private IFormatProvider formatProviderField = CultureInfo.InvariantCulture;

		public IFormatProvider FormatProvider
		{
			get
			{
				return formatProviderField;
			}
			set
			{
				if (value != null)
				{
					formatProviderField = value;
				}
			}
		}

		public string ToStringWithCulture(object objectToConvert)
		{
			if (objectToConvert == null)
			{
				throw new ArgumentNullException("objectToConvert");
			}
			MethodInfo method = objectToConvert.GetType().GetMethod("ToString", new Type[1] { typeof(IFormatProvider) });
			if (method == null)
			{
				return objectToConvert.ToString();
			}
			return (string)method.Invoke(objectToConvert, new object[1] { formatProviderField });
		}
	}

	private StringBuilder generationEnvironmentField;

	private CompilerErrorCollection errorsField;

	private List<int> indentLengthsField;

	private string currentIndentField = "";

	private bool endsWithNewline;

	private IDictionary<string, object> sessionField;

	private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();

	protected StringBuilder GenerationEnvironment
	{
		get
		{
			if (generationEnvironmentField == null)
			{
				generationEnvironmentField = new StringBuilder();
			}
			return generationEnvironmentField;
		}
		set
		{
			generationEnvironmentField = value;
		}
	}

	public CompilerErrorCollection Errors
	{
		get
		{
			if (errorsField == null)
			{
				errorsField = new CompilerErrorCollection();
			}
			return errorsField;
		}
	}

	private List<int> indentLengths
	{
		get
		{
			if (indentLengthsField == null)
			{
				indentLengthsField = new List<int>();
			}
			return indentLengthsField;
		}
	}

	public string CurrentIndent => currentIndentField;

	public virtual IDictionary<string, object> Session
	{
		get
		{
			return sessionField;
		}
		set
		{
			sessionField = value;
		}
	}

	public ToStringInstanceHelper ToStringHelper => toStringHelperField;

	public void Write(string textToAppend)
	{
		if (string.IsNullOrEmpty(textToAppend))
		{
			return;
		}
		if (GenerationEnvironment.Length == 0 || endsWithNewline)
		{
			GenerationEnvironment.Append(currentIndentField);
			endsWithNewline = false;
		}
		if (textToAppend.EndsWith(Environment.NewLine, StringComparison.CurrentCulture))
		{
			endsWithNewline = true;
		}
		if (currentIndentField.Length == 0)
		{
			GenerationEnvironment.Append(textToAppend);
			return;
		}
		textToAppend = textToAppend.Replace(Environment.NewLine, Environment.NewLine + currentIndentField);
		if (endsWithNewline)
		{
			GenerationEnvironment.Append(textToAppend, 0, textToAppend.Length - currentIndentField.Length);
		}
		else
		{
			GenerationEnvironment.Append(textToAppend);
		}
	}

	public void WriteLine(string textToAppend)
	{
		Write(textToAppend);
		GenerationEnvironment.AppendLine();
		endsWithNewline = true;
	}

	public void Write(string format, params object[] args)
	{
		Write(string.Format(CultureInfo.CurrentCulture, format, args));
	}

	public void WriteLine(string format, params object[] args)
	{
		WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
	}

	public void Error(string message)
	{
		CompilerError compilerError = new CompilerError();
		compilerError.ErrorText = message;
		Errors.Add(compilerError);
	}

	public void Warning(string message)
	{
		CompilerError compilerError = new CompilerError();
		compilerError.ErrorText = message;
		compilerError.IsWarning = true;
		Errors.Add(compilerError);
	}

	public void PushIndent(string indent)
	{
		if (indent == null)
		{
			throw new ArgumentNullException("indent");
		}
		currentIndentField += indent;
		indentLengths.Add(indent.Length);
	}

	public string PopIndent()
	{
		string result = "";
		if (indentLengths.Count > 0)
		{
			int num = indentLengths[indentLengths.Count - 1];
			indentLengths.RemoveAt(indentLengths.Count - 1);
			if (num > 0)
			{
				result = currentIndentField.Substring(currentIndentField.Length - num);
				currentIndentField = currentIndentField.Remove(currentIndentField.Length - num);
			}
		}
		return result;
	}

	public void ClearIndent()
	{
		indentLengths.Clear();
		currentIndentField = "";
	}
}
