using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardViewImageColumns
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string ColumnName { get; set; }

	public string Caption { get; set; }

	public string ImageName { get; set; }

	public int VisibleIndex { get; set; } = -1;

	public string Id { get; set; }
}
