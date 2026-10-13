using System;
using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashboardGauge
{
	public Guid Id { get; set; }

	public string BAQ { get; set; }

	public List<DashboardChartFilter> Filters { get; set; }

	public string GaugeType { get; set; }

	public string GaugeCaption { get; set; }

	public string DashboardDefinitionID { get; set; }

	public GaugeDataSet gaugeDataSet { get; set; }
}
