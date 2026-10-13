using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashBoardExecutionSetting
{
	public string Name { get; set; }

	public string Value { get; set; }

	public Guid SysRowID { get; set; }

	public string RowMod { get; set; }
}
