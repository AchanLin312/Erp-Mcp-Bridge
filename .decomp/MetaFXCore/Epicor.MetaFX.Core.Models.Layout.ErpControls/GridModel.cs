using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class GridModel
{
	public int Height { get; set; }

	public string Scrollable { get; set; }

	public int PageSize { get; set; }

	public bool Sortable { get; set; }

	public bool Resizable { get; set; }

	public bool Editable { get; set; }

	public string EpBinding { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public ProviderModel ProviderModel { get; set; }

	public Columns[] Columns { get; set; }

	public Group[] Group { get; set; }
}
