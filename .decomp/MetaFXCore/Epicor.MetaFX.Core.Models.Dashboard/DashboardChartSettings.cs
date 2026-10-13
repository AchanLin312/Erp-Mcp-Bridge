using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType("DashboardChartSettings")]
public class DashboardChartSettings
{
	public dynamic ChartScalings { get; set; }

	public ChartStyle ChartStyle { get; set; }
}
