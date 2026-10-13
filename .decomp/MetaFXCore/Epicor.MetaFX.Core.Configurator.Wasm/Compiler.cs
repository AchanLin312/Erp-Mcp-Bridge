using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Configurator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Epicor.MetaFX.Core.Configurator.Wasm;

public class Compiler : IWasmCompiler
{
	private readonly string DLL_PATH = Path.Combine("..", "..", "WebAssemblies", "Server");

	private static readonly object SyncLock = new object();

	private readonly ILogger logger;

	private readonly string rootPath;

	private Dictionary<string, string> mapper = new Dictionary<string, string>
	{
		{ "Page1", "Page_ReadOnlyExpression" },
		{ "Configurator_OnLoaded", "OnLoaded_Configurator" },
		{ "Configurator_OnComplete", "OnComplete_Configurator" },
		{ "Changed", "_ExecuteCsharp_ColumnChanged" },
		{ "Validate", "_ExecuteCsharp_ColumnChanging" },
		{ "ReadOnlyExpression", "" },
		{ "Page_ReadOnlyExpression", "" }
	};

	private static List<MetadataReference> References { get; set; }

	public Compiler(string rootPath, ILogger logger)
	{
		this.rootPath = rootPath;
		this.logger = logger;
	}

	public EpMetaFxResponse<WasmResponse> Compile(string dllName, string code, List<Method> methods, out byte[] dllBytes)
	{
		dllBytes = null;
		EpMetaFxResponse<WasmResponse> epMetaFxResponse = new EpMetaFxResponse<WasmResponse>();
		Initialize();
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Preview));
		CSharpCompilation cSharpCompilation = CSharpCompilation.Create(dllName, new SyntaxTree[1] { syntaxTree }, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		ImmutableArray<Diagnostic> diagnostics = cSharpCompilation.GetDiagnostics();
		SyntaxNode root = syntaxTree.GetRoot();
		foreach (Diagnostic item in diagnostics)
		{
			switch (item.Severity)
			{
			case DiagnosticSeverity.Info:
			case DiagnosticSeverity.Warning:
				logger.Debug(item.ToString());
				break;
			case DiagnosticSeverity.Error:
			{
				logger.Debug(item.ToString());
				MethodDeclarationSyntax methodDeclarationSyntax = root.FindNode(item.Location.SourceSpan).AncestorsAndSelf().OfType<MethodDeclarationSyntax>()
					.FirstOrDefault();
				string inputString = string.Empty;
				if (methodDeclarationSyntax != null)
				{
					inputString = methodDeclarationSyntax.Identifier.ValueText;
				}
				(bool, string) tuple = ProcessEvent(inputString, methods);
				List<MessageDetail> details = epMetaFxResponse.Details;
				ConversionLogMessageDetail obj = new ConversionLogMessageDetail
				{
					Type = "error",
					Message = item.ToString(),
					MethodName = tuple.Item2
				};
				(obj.IsEvent, _) = tuple;
				details.Add(obj);
				break;
			}
			}
		}
		if (epMetaFxResponse.Details.Any((MessageDetail detail) => detail.Type == "error"))
		{
			epMetaFxResponse.IsSuccess = false;
			epMetaFxResponse.Message = "Compilation Failed";
			return epMetaFxResponse;
		}
		using (MemoryStream memoryStream = new MemoryStream())
		{
			if (!cSharpCompilation.Emit(memoryStream).Success)
			{
				epMetaFxResponse.IsSuccess = false;
				epMetaFxResponse.Message = "Compilation Failed";
				return epMetaFxResponse;
			}
			memoryStream.Seek(0L, SeekOrigin.Begin);
			epMetaFxResponse.Data = new WasmResponse();
			byte[] inArray = (dllBytes = memoryStream.ToArray());
			epMetaFxResponse.Data.Wasm = Convert.ToBase64String(inArray);
		}
		if (!epMetaFxResponse.IsSuccess)
		{
			epMetaFxResponse.IsSuccess = true;
			epMetaFxResponse.Message = "Compiled Successfully.";
		}
		return epMetaFxResponse;
	}

	private void Initialize()
	{
		if (References != null)
		{
			return;
		}
		lock (SyncLock)
		{
			if (References == null)
			{
				References = new List<MetadataReference>();
				string[] files = Directory.GetFiles(Path.Combine(rootPath, DLL_PATH), "*.dll", SearchOption.TopDirectoryOnly);
				foreach (string path in files)
				{
					References.Add(MetadataReference.CreateFromFile(path));
				}
			}
		}
	}

	private (bool isEvent, string methodName) ProcessEvent(string inputString, List<Method> methods)
	{
		if (string.IsNullOrEmpty(inputString))
		{
			return (isEvent: false, methodName: string.Empty);
		}
		string[] array = inputString.Split(new char[1] { '_' });
		if (array.Length > 1 && array[0] == "Configurator")
		{
			string methodName = GetMethodName(methods, inputString, array);
			return (isEvent: true, methodName: methodName);
		}
		return (isEvent: false, methodName: inputString);
	}

	private string GetMethodName(List<Method> methods, string inputString, string[] parts)
	{
		if (inputString.Contains("DynLst"))
		{
			string newStr = inputString.Replace("DynLst", "DynamicListExpression");
			Method method = methods.FirstOrDefault((Method i) => i.Control != null && newStr.Contains(i.Control) && i.Type != null && newStr.Contains(i.Type) && !string.IsNullOrEmpty(i.ClientMethodId));
			if (method == null)
			{
				return string.Empty;
			}
			return method.ClientMethodId + "_" + parts[^1];
		}
		Method method2 = methods.FirstOrDefault((Method i) => i.Control != null && inputString.Contains(i.Control) && i.Type != null && inputString.Contains(i.Type) && !string.IsNullOrEmpty(i.ClientMethodId));
		if (string.IsNullOrEmpty(method2?.ClientMethodId))
		{
			if (inputString.Contains("Page1"))
			{
				Method method3 = methods.FirstOrDefault((Method i) => i.Type == mapper["Page1"] && i.Control == "Page1");
				if (method3 != null)
				{
					return "ReadOnlyExpression_" + method3.Control;
				}
			}
			if (mapper.ContainsKey(inputString))
			{
				return mapper[inputString];
			}
		}
		else
		{
			string[] array = method2?.ClientMethodId?.Split(new char[1] { '_' });
			if (array != null && array.Length != 0 && mapper.ContainsKey(array[0]))
			{
				if (array[0] == "ReadOnlyExpression")
				{
					return "ReadOnlyExpression_" + method2.Control;
				}
				if (array[0] == "Page_ReadOnlyExpression")
				{
					return method2.ClientMethodId;
				}
				return method2.Control + mapper[array[0]];
			}
		}
		return method2?.ClientMethodId ?? string.Empty;
	}
}
