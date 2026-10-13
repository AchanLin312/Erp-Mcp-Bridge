using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Dashboard;

internal class DashboardGridDisplayLayout
{
	public class Body
	{
		public UltraGridDisplayLayout UltraGridDisplayLayout { get; set; }

		public List<UltraGridColumn> UltraGridColumn { get; set; }

		[JsonConverter(typeof(SingleOrArrayConverter<SerializedColumnID>))]
		public List<SerializedColumnID> SerializedColumnID { get; set; }
	}

	private class SingleOrArrayConverter<T> : JsonConverter
	{
		public override bool CanWrite => false;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(List<T>);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JToken jToken = JToken.Load(reader);
			if (jToken.Type == JTokenType.Array)
			{
				return jToken.ToObject<List<T>>();
			}
			return new List<T> { jToken.ToObject<T>() };
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}

	public class Envelope
	{
		[JsonProperty("Body")]
		public Body Model { get; set; }
	}

	public class Key
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("href")]
		public string Href { get; set; }
	}

	public class GridLayout
	{
		[JsonProperty("Envelope")]
		public Envelope GridLayoutDetail { get; set; }
	}

	public class SerializedColumnID
	{
		public Key Key { get; set; }

		public bool IsGroupByColumn { get; set; }
	}

	public class UltraGridColumn
	{
		public Key Key { get; set; }
	}

	public class UltraGridDisplayLayout
	{
		public int ViewStyleBand { get; set; }
	}
}
