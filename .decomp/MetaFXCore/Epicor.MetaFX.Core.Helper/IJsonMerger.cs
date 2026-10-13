using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Helper;

public interface IJsonMerger
{
	bool Debug { get; set; }

	JToken RawJson { get; }

	List<JObject> RawTokens { get; }

	JObject Load(string metaFilePath, string _type, JObject localizedStrings = null, bool shouldConvertToHierarchicalStructure = true, LayerRequest layerRequest = null, string pageName = null, bool isUntranslated = true, int uxAppVersion = 0, string basis = null);

	JObject LoadStrings(string metaFilePath);

	void LoadPatchTokens(string metaFilePath, string rootPath, int uxAppVersion = 0);
}
