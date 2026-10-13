using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Layout;

public class ModelConverter : JsonConverter
{
	private static readonly Dictionary<string, Type> ModelTypes;

	static ModelConverter()
	{
		ModelTypes = new Dictionary<string, Type>();
		foreach (Type item in from t in Assembly.GetAssembly(typeof(ComponentModel)).GetTypes()
			where t.IsSubclassOf(typeof(ComponentModel))
			select t)
		{
			object obj = item.GetCustomAttributes(inherit: true).FirstOrDefault((object attr) => attr is ComponentModelAttribute);
			if (obj != null)
			{
				ModelTypes.Add(((ComponentModelAttribute)obj).Name, item);
			}
		}
	}

	public override bool CanConvert(Type objectType)
	{
		if (objectType == typeof(Component))
		{
			return true;
		}
		return false;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JToken jToken = JToken.ReadFrom(reader);
		string text = jToken["sourceTypeId"]?.ToString();
		if (string.IsNullOrEmpty(text))
		{
			text = jToken["targetTypeId"]?.ToString();
		}
		Component component = new Component();
		if (ModelTypes.ContainsKey(text))
		{
			component.Model = Activator.CreateInstance(ModelTypes[text]) as ComponentModel;
		}
		else
		{
			component.Model = Activator.CreateInstance(typeof(ComponentModel)) as ComponentModel;
		}
		serializer.Populate(jToken.CreateReader(), component);
		return component;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
