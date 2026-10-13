namespace Epicor.MetaFX.Core.Models.Applications;

public class ApplicationRequest
{
	public string Type { get; set; }

	public string SubType { get; set; }

	public string SearchText { get; set; }

	public bool IsPublished { get; set; }

	public bool IncludeAllLayers { get; set; }

	public bool IncludePersLayers { get; set; }
}
