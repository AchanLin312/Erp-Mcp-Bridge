namespace Epicor.MetaFX.Core.Models.Layers;

public class LayerProcessedInfo
{
	public bool PatchedFlag;

	public LayerProcessedInfo(bool patchedFlag = false)
	{
		PatchedFlag = patchedFlag;
	}

	public static string GetKey(string type, string key)
	{
		return type + "~" + key;
	}
}
