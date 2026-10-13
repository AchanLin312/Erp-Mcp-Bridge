namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcDynLst
{
	public string Company { get; set; }

	public string PartNum { get; set; }

	public string RevisionNum { get; set; }

	public string InputName { get; set; }

	public long ListSeq { get; set; }

	public string Condition { get; set; }

	public string ListItems { get; set; }

	public string InitVal { get; set; }

	public bool BaqRunProgram { get; set; }

	public string BaqProgramName { get; set; }

	public string BaqDispValue { get; set; }

	public string BaqInputVal { get; set; }

	public string ListDataSource { get; set; }

	public string ConfigId { get; set; }

	public bool RunTblLookup { get; set; }

	public string TblLookupId { get; set; }

	public string TblLookupFunc { get; set; }

	public string RunUserDefined { get; set; }

	public string UserDefinedName { get; set; }

	public long SysRevId { get; set; }

	public string SysRowId { get; set; }

	public string SortOrder { get; set; }

	public string SubReturnColumn { get; set; }

	public string ScriptCondition { get; set; }

	public string AttributeId { get; set; }

	public bool EnableScript { get; set; }

	public bool EntprsConf { get; set; }

	public long PageSeq { get; set; }

	public string UserDefinedFunctionType { get; set; }

	public string UserDefinedReturnType { get; set; }

	public bool CanUpdate { get; set; }

	public long BitFlag { get; set; }

	public long PartNumSellingFactor { get; set; }

	public string PartNumSalesUm { get; set; }

	public bool PartNumTrackLots { get; set; }

	public string PartNumIum { get; set; }

	public bool PartNumTrackSerialNum { get; set; }

	public string PartNumPartDescription { get; set; }

	public bool PartNumTrackDimension { get; set; }

	public string PartNumPricePerCode { get; set; }

	public string RowMod { get; set; }
}
