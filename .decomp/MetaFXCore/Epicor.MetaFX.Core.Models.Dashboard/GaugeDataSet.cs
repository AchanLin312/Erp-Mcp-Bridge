using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlRoot(ElementName = "GaugeDataSet")]
public class GaugeDataSet
{
	[XmlElement(ElementName = "GaugeTable")]
	public GaugeTable GaugeTable { get; set; }

	[XmlAttribute(AttributeName = "xmlns")]
	public string Xmlns { get; set; }

	[XmlText]
	public string Text { get; set; }
}
