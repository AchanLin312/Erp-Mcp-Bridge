using System;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class DynAttrClassDtl
{
	public long SysRevID { get; set; }

	public bool WebAttribute { get; set; }

	public double MinInteger { get; set; }

	public double MinDecimal { get; set; }

	public DateTime? MinDate { get; set; }

	public double MaxInteger { get; set; }

	public double MaxDecimal { get; set; }

	public DateTime? MaxDate { get; set; }

	public bool InitialLogical { get; set; }

	public int InitialInteger { get; set; }

	public double InitialDecimal { get; set; }

	public DateTime? InitialDate { get; set; }

	public string InitialCharacter { get; set; }

	public decimal IncrPrec { get; set; }

	public string FieldLabel { get; set; }

	public string FieldFormat { get; set; }

	public string FieldDataType { get; set; }

	public string Description { get; set; }

	public bool Active { get; set; }

	public string RelatedToTableName { get; set; }

	public string RelatedToSchemaName { get; set; }

	public string AttributeID { get; set; }

	public string AttrClassID { get; set; }

	public string SysRowID { get; set; }

	public string BizType { get; set; }

	public string UOMAttributeID { get; set; }

	public int SortSeq { get; set; }

	public string RowMod { get; set; }

	public int BitFlag { get; set; }

	public bool IsExpressionDefined { get; set; }

	public string RequiredAtSelectedDesc { get; set; }

	public string RequiredAtSelectedCodes { get; set; }

	public string RequiredAtAvailableDesc { get; set; }

	public string RequiredAtAvailableCodes { get; set; }

	public string FinalExpressionUOM { get; set; }

	public bool IsViewable { get; set; }

	public bool IsFinalExpression { get; set; }

	public string ListValues { get; set; }

	public bool InUse { get; set; }

	public bool IsActual { get; set; }

	public bool DevBoolean01 { get; set; }

	public decimal DevDecimal01 { get; set; }

	public string DevCharacter01 { get; set; }

	public string PlanningType { get; set; }

	public string PlanningBaseUOM { get; set; }

	public bool IsCalculated { get; set; }

	public bool UsedInPlanning { get; set; }

	public string Company { get; set; }
}
