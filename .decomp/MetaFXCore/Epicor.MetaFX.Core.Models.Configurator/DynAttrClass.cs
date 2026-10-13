namespace Epicor.MetaFX.Core.Models.Configurator;

public class DynAttrClass
{
	public string AttrClassID { get; set; }

	public string RelatedToSchemaName { get; set; }

	public string RelatedToTableName { get; set; }

	public bool Active { get; set; }

	public string Description { get; set; }

	public bool WebAttrClass { get; set; }

	public long SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string FullExpression { get; set; }

	public string CalculationType { get; set; }

	public string NumberOfPiecesAction { get; set; }

	public decimal NumberOfPiecesTolerance { get; set; }

	public string DevCharacter01 { get; set; }

	public decimal DevDecimal01 { get; set; }

	public bool DevBoolean01 { get; set; }

	public bool EnableInventoryTracking { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }

	public string Company { get; set; }
}
