using System;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
public class CustomDashboardViewColumns
{
	public string schema { get; set; }

	[XmlElement("DashboardViewColumns")]
	public DashboardViewColumns[] DashboardViewColumns { get; set; }
}
