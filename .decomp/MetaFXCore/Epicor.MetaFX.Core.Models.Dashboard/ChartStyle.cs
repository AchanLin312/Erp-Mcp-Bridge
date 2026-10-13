using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType("ChartStyle")]
public class ChartStyle
{
	public string ChartType { get; set; }
}
