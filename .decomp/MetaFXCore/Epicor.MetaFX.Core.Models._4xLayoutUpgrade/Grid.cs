using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class Grid
{
	public string Id { get; set; }

	public string Guid { get; set; }

	public bool Hidden { get; set; }

	public bool IsKeyField { get; set; }

	public string EpBinding { get; set; }

	public int TabIndex { get; set; }

	public string Title { get; set; }

	public bool Disabled { get; set; }

	public bool Readonly { get; set; }

	public bool Pageable { get; set; }

	public bool Sortable { get; set; }

	public bool Resizable { get; set; }

	public List<ColumnsItem> Columns { get; set; }
}
