using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://tempuri.org/DashboardDataSet.xsd")]
public class DashboardViewColumns
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string ColumnName { get; set; }

	public bool IsVisible { get; set; }

	public string DataType { get; set; }

	public string Caption { get; set; }

	public string LabelSize { get; set; }

	public string LabelLocation { get; set; }

	public string LabelGuid { get; set; }

	public string ControlSize { get; set; }

	public string ControlLocation { get; set; }

	public string ControlGuid { get; set; }

	public bool DashboardPrompt { get; set; }

	[XmlIgnore]
	public bool DashboardPromptSpecified { get; set; }

	public bool DashboardHonorNull { get; set; }

	[XmlIgnore]
	public bool DashboardHonorNullSpecified { get; set; }

	public string DashboardCondition { get; set; }

	public string ControlType { get; set; }

	public string BaseType { get; set; }

	public string RetrieverCombo { get; set; }

	public string DescColumnName { get; set; }

	public bool IsKeyField { get; set; }

	public object QuickSearchId { get; set; }

	public bool IsFocused { get; set; }

	public bool OnYAxis { get; set; }

	public bool OnXAxis { get; set; }

	public bool OnZAxis { get; set; }
}
