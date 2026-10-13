using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardViewFilter
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string ColumnName { get; set; }

	public string Condition { get; set; }

	public string Value { get; set; }

	public bool IsPublisher { get; set; }

	public bool IsValueAnotherColumn { get; set; }
}
