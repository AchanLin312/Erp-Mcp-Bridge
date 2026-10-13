namespace Epicor.MetaFX.Core.Models;

public class ImportLayerRequest
{
	public string content { get; set; }

	public bool overwrite { get; set; }

	public string overrideCompanyId { get; set; }

	public string overridePersonalizationCompanyId { get; set; }

	public string overrideCustomizationUserId { get; set; }

	public string overridePersonalizationUserId { get; set; }
}
