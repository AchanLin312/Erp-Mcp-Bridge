namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxCacheInvalidateRequest
{
	public string viewId { get; set; } = string.Empty;

	public string Type { get; set; } = string.Empty;

	public string Like { get; set; } = string.Empty;

	public string LanguageId { get; set; } = string.Empty;
}
