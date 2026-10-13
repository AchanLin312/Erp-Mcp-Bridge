using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[XmlRoot(ElementName = "QueryPhraseSubscriberList")]
public class QueryPhraseSubscriberList
{
	[XmlElement(ElementName = "QueryPhraseSubscribers")]
	public List<QueryPhraseSubscribers> QueryPhraseSubscribers { get; set; }
}
