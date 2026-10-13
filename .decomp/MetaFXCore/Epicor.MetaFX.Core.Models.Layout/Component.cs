using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout;

public class Component
{
	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public string ParentId { get; set; }

	public string Id { get; set; }

	public string TargetTypeId { get; set; }

	public string SourceTypeId { get; set; }

	public ComponentModel Model { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<Component> Components { get; set; }

	public ComponentBoxData BoxData { get; set; }

	public bool? IsSelectedTab { get; set; }

	public int? TabIndex { get; set; }
}
