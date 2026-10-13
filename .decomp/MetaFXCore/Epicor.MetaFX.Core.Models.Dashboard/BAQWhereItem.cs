namespace Epicor.MetaFX.Core.Models.Dashboard;

public class BAQWhereItem
{
	public string ColumnName { get; set; }

	public string TableName { get; set; }

	public string Condition { get; set; }

	public string Value { get; set; }

	public bool CompareColumns { get; set; }

	public string BAQColumn { get; set; }

	public string FilterValue { get; set; }

	public bool IsBinding { get; set; }

	public bool HonorNull { get; set; }
}
