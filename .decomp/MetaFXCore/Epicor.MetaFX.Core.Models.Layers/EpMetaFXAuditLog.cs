using System;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFXAuditLog
{
	public string Company { get; set; }

	public string TypeCode { get; set; }

	public string ViewId { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string[] ParentLayers { get; set; }

	public string CommentText { get; set; }

	public string DeviceType { get; set; }

	public DateTime? LastUpdated { get; set; }

	public string LastUpdatedBy { get; set; }

	public DateTime? Published { get; set; }
}
