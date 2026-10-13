using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class Group
{
	public string Id { get; set; }

	public List<JToken> Controls { get; set; }

	public string Label { get; set; }

	public string ParentId { get; set; }
}
