namespace Epicor.MetaFX.Core.Models.Configurator;

public class SyncRevisionRequest
{
	public bool IpApproved { get; set; }

	public bool IpValidPassword { get; set; }

	public string IpConfigID { get; set; }

	public string IpAuditDesc { get; set; }
}
