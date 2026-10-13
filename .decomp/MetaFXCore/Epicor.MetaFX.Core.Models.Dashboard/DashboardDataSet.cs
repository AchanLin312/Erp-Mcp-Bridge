using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
[XmlRoot(Namespace = "http://tempuri.org/DashboardDataSet.xsd", IsNullable = false)]
public class DashboardDataSet
{
	public DashboardDefinition DashboardDefinition { get; set; }

	[XmlElement("DashboardQuery")]
	public DashboardQuery[] DashboardQuery { get; set; }

	[XmlElement("DashboardView")]
	public DashboardView[] DashboardView { get; set; }

	[XmlElement("DashboardQueryPublishers")]
	public DashboardQueryPublishers[] DashboardQueryPublishers { get; set; }

	[XmlElement("DashboardQueryWhereClause")]
	public DashboardQueryWhereClause[] DashboardQueryWhereClause { get; set; }

	[XmlElement("DashboardViewRules")]
	public DashboardViewRules[] DashboardViewRules { get; set; }

	[XmlElement("DashboardViewRuleActions")]
	public DashboardViewRuleActions[] DashboardViewRuleActions { get; set; }

	[XmlElement("DashboardViewFilter")]
	public DashboardViewFilter[] DashboardViewFilter { get; set; }

	[XmlElement("DashboardViewColumns")]
	public DashboardViewColumns[] DashboardViewColumns { get; set; }

	[XmlElement("DashboardViewOptions")]
	public DashboardViewOptions[] DashboardViewOptions { get; set; }

	public DashboardViewCustomizations DashboardViewCustomizations { get; set; }

	[XmlElement("DashboardViewImageColumns")]
	public DashboardViewImageColumns[] DashboardViewImageColumns { get; set; }

	[XmlElement("DashboardProperties")]
	public DashboardProperties[] DashboardProperties { get; set; }

	[XmlElement("DashboardQueryOptions")]
	public DashboardQueryOptions[] DashboardQueryOptions { get; set; }

	[XmlElement("DashboardURL")]
	public DashboardURL[] DashboardURLs { get; set; }

	[XmlElement("DashboardProcess")]
	public DashboardProcess[] DashboardProcess { get; set; }
}
