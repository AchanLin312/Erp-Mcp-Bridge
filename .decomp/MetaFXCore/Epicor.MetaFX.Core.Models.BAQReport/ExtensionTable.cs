using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.BAQReport;

public class ExtensionTable
{
	public List<Table> Table { get; set; }

	public string SystemCode { get; set; }

	public string TableName { get; set; }

	public List<Column> Columns { get; set; }

	public List<string> PrimaryKeyColumns { get; set; }

	public string PeerTableSystemCode { get; set; }

	public string PeerTableName { get; set; }
}
