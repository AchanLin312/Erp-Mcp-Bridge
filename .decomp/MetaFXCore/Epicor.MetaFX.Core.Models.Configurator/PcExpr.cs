namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcExpr
{
	public string Company { get; set; }

	public string PartNum { get; set; }

	public string RevisionNum { get; set; }

	public string InputName { get; set; }

	public string ConfigId { get; set; }

	public string TypeCode { get; set; }

	public long SeqNum { get; set; }

	public string Expression { get; set; }

	public long SysRevId { get; set; }

	public string SysRowId { get; set; }

	public long BitFlag { get; set; }

	public string RowMod { get; set; }

	public long? PageSeq { get; set; }
}
