namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashBdChunk
{
	public string Company { get; set; }

	public string ProductID { get; set; }

	public string GlbCompany { get; set; }

	public string DefinitionID { get; set; }

	public int SeqNum { get; set; }

	public string Chunk { get; set; }

	public string CGCCode { get; set; }

	public bool Deployed { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
