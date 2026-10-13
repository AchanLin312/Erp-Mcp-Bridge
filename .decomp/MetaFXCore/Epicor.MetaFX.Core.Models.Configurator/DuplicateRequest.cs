using System.ComponentModel.DataAnnotations;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class DuplicateRequest
{
	[Required]
	public string ViewId { get; set; }

	[Required]
	public string NewViewId { get; set; }
}
