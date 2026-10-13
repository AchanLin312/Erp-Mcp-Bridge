using System.Collections.Generic;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

public class OnSuccess
{
	public List<SuccessAction> actions { get; set; }
}
