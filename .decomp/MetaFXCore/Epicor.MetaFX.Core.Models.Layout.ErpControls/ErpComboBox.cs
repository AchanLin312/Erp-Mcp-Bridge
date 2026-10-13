using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("erp-combo-box")]
public class ErpComboBox : ComponentModel
{
	public string AppDesignerComboType { get; set; }

	public bool? AppDesignerComboOverride { get; set; }

	public string EpBindingTextColumn { get; set; }

	[JsonProperty(PropertyName = "$ref", NullValueHandling = NullValueHandling.Ignore)]
	public string Reference { get; set; }

	public string DropDownStyle { get; set; }

	public bool? IsDelimited { get; set; }

	public string TextField { get; set; }

	public string ValueField { get; set; }

	public SortFields[] List { get; set; }

	public string Svc { get; set; }

	public string SvcPath { get; set; }

	public string TableName { get; set; }

	public string FieldName { get; set; }

	public string DbColumn { get; set; }

	public string Sort { get; set; }

	public string ParameterName { get; set; }

	public string DataSource { get; set; }

	public string ViewName { get; set; }

	public List<string> RestArguments { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string QueryId { get; set; }
}
