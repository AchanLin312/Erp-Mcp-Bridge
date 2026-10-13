using JsonDiffPatch;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Helper;

public static class JsonDiffExtender
{
	public static PatchDocument DiffwithProp(this JsonDiffer jd, JToken from, JToken to, string idName = "")
	{
		JToken jToken = RenameIdProperty(from, idName);
		JToken to2 = RenameIdProperty(to, idName);
		return jd.Diff(jToken, to2, useIdPropertyToDetermineEquality: true);
	}

	private static JToken RenameIdProperty(JToken json, string idName)
	{
		return JToken.Parse(json.ToString().Replace(idName, "id"));
	}
}
