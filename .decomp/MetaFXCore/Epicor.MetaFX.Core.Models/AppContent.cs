using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Epicor.MetaFX.Core.Models;

public class AppContent
{
	[Required]
	public string ViewId { get; set; }

	[Required]
	public string CompanyId { get; set; }

	[Required]
	public Dictionary<string, string> Files { get; set; }
}
