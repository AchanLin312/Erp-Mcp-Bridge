using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class BaqOptions
{
	public string Where { get; set; }

	public List<BAQWhereExpression> WhereList { get; set; }
}
