namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryFieldDesigner
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

	public bool IsRequired { get; set; }

	public bool IsReadOnly { get; set; }

	public string DefaultValue { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string SubquerySet_Alias { get; set; }

	public string SubquerySet_DataType { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
