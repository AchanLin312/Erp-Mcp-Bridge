using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardProperties
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string PropertyID { get; set; }

	public string PropertyValue { get; set; }
}
