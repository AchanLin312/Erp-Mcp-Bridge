using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptDataSourceField
{
	public string RowMod { get; set; }

	public Guid SysRowID { get; set; }

	public string FieldLabel { get; set; }

	public string LikeDataFieldName { get; set; }

	public string DataType { get; set; }

	public string DBFieldName { get; set; }

	public string DBTableName { get; set; }

	public string QueryID { get; set; }

	public string FieldName { get; set; }
}
