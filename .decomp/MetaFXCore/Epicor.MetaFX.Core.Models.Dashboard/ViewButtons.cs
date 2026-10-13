using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[XmlRoot(ElementName = "ViewButtons")]
public class ViewButtons
{
	[XmlElement(ElementName = "Seq")]
	public string Seq { get; set; }

	[XmlElement(ElementName = "Text")]
	public string Text { get; set; }

	[XmlElement(ElementName = "Image")]
	public string Image { get; set; }

	[XmlElement(ElementName = "Action")]
	public string Action { get; set; }
}
