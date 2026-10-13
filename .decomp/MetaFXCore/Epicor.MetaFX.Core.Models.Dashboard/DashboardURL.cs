using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardURL
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string UrlID { get; set; }

	public string URLAddress { get; set; }

	public string UrlPanelID { get; set; }

	public string PublisherName { get; set; }

	public string Caption { get; set; }

	public string BindToQuery { get; set; }

	public bool ShowQuerySummary { get; set; }

	public string StyleSheetPublishers { get; set; }

	public string CanvasId { get; set; }

	public string QueryPhraseSubscribers { get; set; }

	public string EpmType { get; set; }
}
