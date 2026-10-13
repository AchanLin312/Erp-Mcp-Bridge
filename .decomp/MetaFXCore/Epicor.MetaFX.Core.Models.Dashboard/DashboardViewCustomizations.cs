using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardViewCustomizations
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public object CustomizationID { get; set; }

	public string CustomizationDataSetXML { get; set; }
}
