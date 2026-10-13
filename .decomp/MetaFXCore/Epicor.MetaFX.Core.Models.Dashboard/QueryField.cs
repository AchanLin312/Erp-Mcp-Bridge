using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class QueryField
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string TableID { get; set; }

	public string FieldName { get; set; }

	public int Seq { get; set; }

	public string DBSchemaName { get; set; }

	public string DBTableName { get; set; }

	public string DBFieldName { get; set; }

	public string DataType { get; set; }

	public string Description { get; set; }

	public bool External { get; set; }

	public bool IsCalculated { get; set; }

	public string Formula { get; set; }

	public string FieldFormat { get; set; }

	public string FieldLabel { get; set; }

	public string LikeDataFieldTableID { get; set; }

	public int LikeDataFieldSeq { get; set; }

	public string Aggregation { get; set; }

	public bool IsGroupBy { get; set; }

	public bool UseLike { get; set; }

	public string LikeDataFieldName { get; set; }

	public bool Updatable { get; set; }

	public bool RaiseEvent { get; set; }

	public string QuickSearchID { get; set; }

	public bool DropDown { get; set; }

	public string UpdInitExpression { get; set; }

	public string Alias { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public string DisplayName { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
