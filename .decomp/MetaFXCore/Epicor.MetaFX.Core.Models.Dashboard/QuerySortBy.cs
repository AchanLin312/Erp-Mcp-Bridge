using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class QuerySortBy
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string TableID { get; set; }

	public string FieldName { get; set; }

	public int Seq { get; set; }

	public bool IsAsc { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public string DisplayName { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
