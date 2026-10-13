using System;
using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashboardChart
{
	public Guid Id { get; set; }

	public string BAQ { get; set; }

	public List<string> XAxis { get; set; }

	public List<string> YAxis { get; set; }

	public List<string> ZAxis { get; set; }

	public List<DashboardChartFilter> Filters { get; set; }

	public string ChartType { get; set; }

	public string ChartCaption { get; set; }

	public string DashboardDefinitionID { get; set; }
}
