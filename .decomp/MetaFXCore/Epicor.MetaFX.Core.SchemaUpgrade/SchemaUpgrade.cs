using System;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.SchemaUpgrade.DataViewSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.RowRuleSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.StringsSchemaUpgrade;
using Epicor.MetaFX.Core.SchemaUpgrade.ToolsSchemaUpgrade;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade;

public class SchemaUpgrade
{
	private readonly ILogger logger;

	public SchemaUpgrade(ILogger logger)
	{
		this.logger = logger;
	}

	public string Upgrade(string content)
	{
		return UpgradeWithJSONC_Comments(content);
	}

	private string UpgradeWithJSONC_Comments(string content)
	{
		JsonLoadSettings settings = new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Load
		};
		JObject json = null;
		using (StringReader reader = new StringReader(content))
		{
			json = JObject.Load(new JsoncTextReader(reader), settings);
		}
		JToken jToken = json["$schema"];
		if (jToken != null)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jToken.ToString());
			int num = 1;
			while (!checkSchemaVersion(ref json, fileNameWithoutExtension))
			{
				json = Upgrade(json, fileNameWithoutExtension);
				num++;
				if (num == 50)
				{
					throw new Exception("Unable to upgrade : " + fileNameWithoutExtension + ", Reached maximum count of recursive upgrade.");
				}
			}
			using StringWriter stringWriter = new StringWriter();
			JsoncTextWriter writer = new JsoncTextWriter(stringWriter)
			{
				Formatting = Formatting.Indented
			};
			json.WriteTo(writer);
			return stringWriter.ToString();
		}
		return content;
	}

	private bool checkSchemaVersion(ref JObject json, string _type)
	{
		switch (_type)
		{
		case "events":
			return getContentVersion(ref json, _type) == "5.0";
		case "tools":
			return getContentVersion(ref json, _type) == "2.0";
		case "dataviews":
			return getContentVersion(ref json, _type) == "4.0";
		case "layout":
			return getContentVersion(ref json, _type) == "3.0";
		case "rules":
			return getContentVersion(ref json, _type) == "3.0";
		case "search":
			return getContentVersion(ref json, _type) == "4.0";
		case "strings":
			return getContentVersion(ref json, _type) == "3.0";
		case "pages":
		case "references":
			return true;
		default:
			return getContentVersion(ref json, _type) == "v1";
		}
	}

	private string getContentVersion(ref JObject json, string _type)
	{
		JProperty jProperty = json.Properties().FirstOrDefault((JProperty j) => j.Name == "$schema");
		if (jProperty == null)
		{
			return "";
		}
		string text = jProperty.ToString().ToLower().Replace("/" + _type + ".json", "");
		int num = text.LastIndexOf('/') + 1;
		text = text.Substring(num, text.Length - num - 1);
		string text2 = ((json["contentVersion"] != null) ? json["contentVersion"].ToString() : "");
		logger.Debug("Schema Version=" + text);
		logger.Debug("Content Version=" + text2);
		return text2;
	}

	public JObject Upgrade(JObject json, string type)
	{
		JToken jToken = json["contentVersion"];
		if (jToken != null)
		{
			if (jToken.ToString() == "1" || jToken.ToString() == "1.0" || jToken.ToString() == "1.0.0")
			{
				switch (type)
				{
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV11(logger).Upgrade(json);
				case "tools":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeToolsV11(logger).Upgrade(json);
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV11(logger).Upgrade(json);
				case "dataviews":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeDataViewV11(logger).Upgrade(json);
				case "layout":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV11(logger).Upgrade(json);
				case "rules":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeRowRuleV11(logger).Upgrade(json);
				}
				if (type.ToLower() == "strings")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeStringsV30(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "1.1")
			{
				switch (type)
				{
				case "dataviews":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeDataViewV20(logger).Upgrade(json);
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV12(logger).Upgrade(json);
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV12(logger).Upgrade(json);
				case "tools":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeToolsV12(logger).Upgrade(json);
				case "layout":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV12(logger).Upgrade(json);
				case "rules":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeRowRuleV12(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "1.2")
			{
				switch (type)
				{
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV13(logger).Upgrade(json);
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV13(logger).Upgrade(json);
				case "layout":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV13(logger).Upgrade(json);
				case "tools":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeToolsV20(logger).Upgrade(json);
				case "rules":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeRowRuleV30(logger).Upgrade(json);
				}
			}
			if (jToken.ToString() == "1.3")
			{
				switch (type)
				{
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV14(logger).Upgrade(json);
				case "layout":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV14(logger).Upgrade(json);
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV20(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "1.4")
			{
				if (type == "layout")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV15(logger).Upgrade(json);
				}
				if (type == "search")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV30(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "1.5")
			{
				if (type == "layout")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV16(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "1.6")
			{
				if (type == "layout")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV20(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "2.0")
			{
				switch (type)
				{
				case "dataviews":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeDataViewV30(logger).Upgrade(json);
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV21(logger).Upgrade(json);
				case "layout":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV21(logger).Upgrade(json);
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV30(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "2.1")
			{
				if (type == "events")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV22(logger).Upgrade(json);
				}
				if (type == "layout")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeLayoutV30(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "2.2")
			{
				if (type == "events")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV23(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "2.3")
			{
				if (type == "events")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV30(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "3.0")
			{
				switch (type)
				{
				case "events":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV31(logger).Upgrade(json);
				case "dataviews":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeDataViewV40(logger).Upgrade(json);
				case "search":
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeSearchV40(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "3.1")
			{
				if (type == "events")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV32(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "3.2")
			{
				if (type == "events")
				{
					logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
					return new UpgradeEventV40(logger).Upgrade(json);
				}
			}
			else if (jToken.ToString() == "4.0" && type == "events")
			{
				logger.Debug($"Schema with content version {jToken} of type {type} upgraded");
				return new UpgradeEventV50(logger).Upgrade(json);
			}
		}
		return json;
	}

	public JObject UpgradeToLatestSchema(JObject json, string type)
	{
		string text;
		do
		{
			text = json["contentVersion"].ToString();
			json = Upgrade(json, type);
		}
		while (text != json["contentVersion"].ToString());
		return json;
	}
}
