using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[XmlRoot(ElementName = "QueryPhraseSubscribers")]
public class QueryPhraseSubscribers
{
	[XmlElement(ElementName = "PublisherName")]
	public string PublisherName { get; set; }

	[XmlElement(ElementName = "Token")]
	public string Token { get; set; }

	[XmlElement(ElementName = "PublisherID")]
	public string PublisherID { get; set; }
}
