using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.DataView;

public class DataViewSchema : BaseSchema
{
	[JsonProperty(PropertyName = "dataviews", Order = 10)]
	public List<DataView> DataViews { get; set; }
}
