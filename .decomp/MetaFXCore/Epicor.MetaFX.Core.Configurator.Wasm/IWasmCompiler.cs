using System.Collections.Generic;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Configurator;

namespace Epicor.MetaFX.Core.Configurator.Wasm;

public interface IWasmCompiler
{
	EpMetaFxResponse<WasmResponse> Compile(string dllName, string code, List<Method> methods, out byte[] dllBytes);
}
