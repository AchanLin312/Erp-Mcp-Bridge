using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class QueryRelationField
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string RelationID { get; set; }

	public int Seq { get; set; }

	public string ParentFieldName { get; set; }

	public string ParentFieldDataType { get; set; }

	public bool ParentFieldIsExpr { get; set; }

	public string ChildFieldName { get; set; }

	public string ChildFieldDataType { get; set; }

	public bool ChildFieldIsExpr { get; set; }

	public string AndOr { get; set; }

	public bool Neg { get; set; }

	public string LeftP { get; set; }

	public string RightP { get; set; }

	public string CompOp { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
