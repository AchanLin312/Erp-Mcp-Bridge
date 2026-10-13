namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxDiffRequest
{
	public string id { get; set; }

	public EpMetaFxRequestProperty leftRequestProperties { get; set; }

	public EpMetaFxRequestProperty rightRequestProperties { get; set; }
}
