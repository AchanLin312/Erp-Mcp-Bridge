namespace Epicor.MetaFX.Core.Models.Layers;

public class SessionData
{
	public string CompanyID { get; set; }

	public bool IsMultiTenant { get; set; }

	public string UserID { get; set; }

	public string TenantID { get; set; }
}
