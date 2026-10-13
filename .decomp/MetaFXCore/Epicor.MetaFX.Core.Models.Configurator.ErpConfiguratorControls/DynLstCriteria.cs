namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

public class DynLstCriteria
{
	public string ColumnName { get; set; }

	public string ColumnType { get; set; }

	public string StringValue { get; set; }

	public bool? BooleanValue { get; set; }

	public string DateValue { get; set; }

	public double? DecimalValue { get; set; }

	public string BaqValue { get; set; }

	public string InputNameValue { get; set; }

	public string Condition { get; set; }

	public string ValueFrom { get; set; }

	public string Operator { get; set; }

	public string LeftP { get; set; }

	public string RightP { get; set; }

	public long CriteriaSeq { get; set; }
}
