using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.BAQReport;

public class Table
{
	public List<ColumnValue> ColumnValues { get; set; }

	public string RowMod { get; set; }

	public string SysRowID { get; set; }
}
