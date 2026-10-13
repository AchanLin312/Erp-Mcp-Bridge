using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[XmlRoot(ElementName = "DocumentElement")]
public class DocumentElement
{
	[XmlElement(ElementName = "ViewButtons")]
	public List<ViewButtons> ViewButtons { get; set; }

	[XmlElement(ElementName = "AddNewSubscriber")]
	public List<AddNewSubscriber> AddNewSubscriber { get; set; }

	[XmlElement(ElementName = "CallContextSubscribers")]
	public List<CallContextSubscriber> CallContextSubscribers { get; set; }
}
