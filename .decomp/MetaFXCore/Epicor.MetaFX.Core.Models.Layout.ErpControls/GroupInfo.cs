using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class GroupInfo
{
	public List<Group> Groups { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }
}
