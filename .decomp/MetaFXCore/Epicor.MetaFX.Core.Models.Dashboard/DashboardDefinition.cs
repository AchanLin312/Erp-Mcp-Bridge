using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashboardDefinition
{
	public List<DashBdDef> DashBdDef { get; set; }

	public List<DashBdBAQ> DashBdBAQ { get; set; }

	public List<DashBdChunk> DashBdChunk { get; set; }

	public List<DashBdLike> DashBdLike { get; set; }
}
