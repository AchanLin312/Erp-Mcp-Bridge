namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("erp-radio-button")]
public class ErpRadioButton : ComponentModel
{
	public DataFields[] Data { get; set; }

	public string Svc { get; set; }

	public string SvcPath { get; set; }

	public string TableName { get; set; }

	public string FieldName { get; set; }

	public string DbColumn { get; set; }

	public string Sort { get; set; }

	public string ParameterName { get; set; }

	public string DataSource { get; set; }

	public string ViewName { get; set; }

	public bool VerticalAlign { get; set; }
}
