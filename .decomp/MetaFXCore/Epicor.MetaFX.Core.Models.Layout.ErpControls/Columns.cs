namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class Columns
{
	public string Field { get; set; }

	public string Title { get; set; }

	public bool Sortable { get; set; }

	public bool Filterable { get; set; }

	public bool Resizable { get; set; }

	public bool Editable { get; set; }

	public bool Groupable { get; set; }

	public bool Hidden { get; set; }

	public string ImageName { get; set; }

	public int visibleIndex { get; set; }
}
