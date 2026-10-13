using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardQuery
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string DynamicQueryID { get; set; }

	public string QueryPanelID { get; set; }

	public int RefreshInterval { get; set; }

	public bool SynchronizeViews { get; set; }

	public string Caption { get; set; }

	public bool PublishToTitle { get; set; }

	public string PublishToTitleColumn { get; set; }

	public string PublishToTitleCaption { get; set; }

	public string DockLayoutXML { get; set; }

	public bool AutoRefreshOnLoad { get; set; }

	[XmlIgnore]
	public bool AutoRefreshOnLoadSpecified { get; set; }

	public bool IsPrimary { get; set; }

	public string ParentView { get; set; }

	public string ParentColumn { get; set; }

	public string ChildColumn { get; set; }

	public string Like { get; set; }

	public string PanelSize { get; set; }

	public string ViewName { get; set; }
}
