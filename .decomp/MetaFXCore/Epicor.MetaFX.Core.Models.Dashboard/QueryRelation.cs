using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class QueryRelation
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string RelationID { get; set; }

	public bool IsFK { get; set; }

	public string ParentTableID { get; set; }

	public string ChildTableID { get; set; }

	public string JoinType { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public bool OuterJoin { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
