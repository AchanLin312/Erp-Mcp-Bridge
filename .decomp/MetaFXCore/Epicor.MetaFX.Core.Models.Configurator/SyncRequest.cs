using System.ComponentModel.DataAnnotations;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class SyncRequest
{
	[Required]
	public string ConfigId { get; set; }

	[Required]
	public string ExtCompanyID { get; set; }

	[Required]
	public string CompanyID { get; set; }
}
