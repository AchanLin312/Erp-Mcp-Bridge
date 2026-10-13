namespace Epicor.MetaFX.Core.Models.Events;

public class EventDesignerRequest
{
	public string ViewId { get; set; }

	public string Company { get; set; }

	public string TypeCode { get; set; }

	public string LayerName { get; set; }

	public string DeviceType { get; set; }

	public string CGCCode { get; set; }

	public int UxAppVersion { get; set; }
}
