namespace Epicor.MetaFX.Core.Models.Dashboard;

public class RowColumnChangedArgs
{
	public RowColumnChangedType Type { get; set; }

	public string Dataview { get; set; }

	public string Filter { get; set; }

	public string NextEvent { get; set; }

	public string EpBinding { get; set; }

	public string Value { get; set; }

	public string FromDataView { get; set; }

	public string ToDataView { get; set; }
}
