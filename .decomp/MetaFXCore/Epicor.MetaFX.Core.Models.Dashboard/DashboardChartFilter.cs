namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashboardChartFilter
{
	public string ColumnName { get; set; }

	public string Condition { get; set; }

	public string Value { get; set; }

	public bool IsValueAnotherColumn { get; set; }
}
