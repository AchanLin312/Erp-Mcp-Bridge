namespace Epicor.MetaFX.Core.Models.Configurator;

public class DynAttrClassDtlListVal
{
	public string AttrClassID { get; set; }

	public string AttributeID { get; set; }

	public string Code { get; set; }

	public string RelatedToSchemaName { get; set; }

	public string RelatedToTableName { get; set; }

	public string Description { get; set; }

	public long SysRevID { get; set; }

	public string SysRowID { get; set; }

	public bool Active { get; set; }

	public string PlanningConvOperator { get; set; }

	public decimal PlanningConvFactor { get; set; }

	public int WhereAvailable { get; set; }

	public string TheoreticalUOM { get; set; }

	public string DevCharacter01 { get; set; }

	public decimal DevDecimal01 { get; set; }

	public bool DevBoolean01 { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }

	public string Company { get; set; }
}
