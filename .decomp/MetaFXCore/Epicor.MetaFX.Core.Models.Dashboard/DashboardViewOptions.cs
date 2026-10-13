using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardViewOptions
{
	public string Company { get; set; }

	public string DefininitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string PropertyName { get; set; }

	public string PropertyType { get; set; }

	public string PropertyValue { get; set; }
}
