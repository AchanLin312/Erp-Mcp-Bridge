using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlRoot(ElementName = "GaugeTable")]
public class GaugeTable
{
	[XmlElement(ElementName = "GaugeKey")]
	public string GaugeKey { get; set; }

	[XmlElement(ElementName = "GaugeType")]
	public string GaugeType { get; set; }

	[XmlElement(ElementName = "ScaleKey")]
	public string ScaleKey { get; set; }

	[XmlElement(ElementName = "ScaleStartValue")]
	public int ScaleStartValue { get; set; }

	[XmlElement(ElementName = "ScaleEndValue")]
	public int ScaleEndValue { get; set; }

	[XmlElement(ElementName = "MarkerKey")]
	public string MarkerKey { get; set; }

	[XmlElement(ElementName = "MarkerValue")]
	public int MarkerValue { get; set; }

	[XmlElement(ElementName = "StartValueBinding")]
	public string StartValueBinding { get; set; }

	[XmlElement(ElementName = "EndValueBinding")]
	public string EndValueBinding { get; set; }

	[XmlElement(ElementName = "MarkerBinding")]
	public string MarkerBinding { get; set; }

	[XmlElement(ElementName = "MarkerAllowDrag")]
	public bool MarkerAllowDrag { get; set; }

	[XmlElement(ElementName = "MarkerBindingReadOnly")]
	public bool MarkerBindingReadOnly { get; set; }
}
