using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Event;

public class EventSchema : BaseSchema
{
	[JsonProperty(PropertyName = "events", Order = 10)]
	public List<Event> Events { get; set; } = new List<Event>();
}
