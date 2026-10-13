namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFXAuditLogRequest
{
	public string ViewId { get; set; }

	public string LayerName { get; set; }

	public string ApplicationType { get; set; }

	public string ApplicationSubType { get; set; }

	public string Company { get; set; }
}
