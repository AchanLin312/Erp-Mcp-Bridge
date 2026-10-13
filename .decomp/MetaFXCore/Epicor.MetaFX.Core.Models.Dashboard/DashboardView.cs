using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardView
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string ViewType { get; set; }

	public string Caption { get; set; }

	public string ViewPanelID { get; set; }

	public string PanelSize { get; set; }

	public string ChartSettings { get; set; }

	public DashboardChart DasboardChart { get; set; }

	public DashboardGauge DashboardGauge { get; set; }

	public Point ScreenLocation { get; set; }

	public int? TabIndex { get; set; }

	public int? TabId { get; set; }

	public bool? IsSelectedTab { get; set; }
}
