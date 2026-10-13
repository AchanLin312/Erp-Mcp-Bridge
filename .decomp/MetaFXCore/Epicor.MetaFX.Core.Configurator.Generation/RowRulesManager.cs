using System.Collections.Generic;
using System.Text;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

internal class RowRulesManager
{
	private readonly ConfiguratorArgs configuratorArgs;

	public RowRulesManager(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
	}

	public RulesSchema GenerateSchema()
	{
		RulesSchema rulesSchema = new RulesSchema();
		rulesSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1/rules.json";
		rulesSchema.ContentVersion = "1.0";
		rulesSchema.Imports = new object[1]
		{
			new
			{
				rules = new Reference
				{
					Refs = configuratorArgs.ImportPath + "/rules.jsonc#/rules"
				}
			}
		};
		return rulesSchema;
	}

	public List<RowRuleBase> GenerateRules()
	{
		List<RowRuleBase> list = new List<RowRuleBase>();
		list.AddRange(GeneratePageRules());
		list.AddRange(GeneratePcInputsRules());
		return list;
	}

	private List<RowRuleBase> GeneratePageRules()
	{
		List<RowRuleBase> list = new List<RowRuleBase>();
		foreach (PcPage item in configuratorArgs.Configuration.PcPage)
		{
			RowRuleBase rowRuleBase = new RowRuleBase();
			rowRuleBase.Name = "readonly" + configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq);
			rowRuleBase.DataView = configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq);
			rowRuleBase.Condition = $"PcPage.ReadOnly = true AND PcPage.PageSeq = {item.PageSeq}";
			List<object> list2 = new List<object>();
			list2.Add(new
			{
				action = "DisableRow",
				exceptTheseColumns = new List<object>()
			});
			rowRuleBase.Actions = list2.ToArray();
			list.Add(rowRuleBase);
		}
		return list;
	}

	public void CreateFile(RulesSchema ruleSchema)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(ruleSchema), "rules");
		configuratorArgs.HelperMethods.CreateJsonFile(data, configuratorArgs.OutputPath, "rules.jsonc");
	}

	private List<RowRuleBase> GeneratePcInputsRules()
	{
		List<RowRuleBase> list = new List<RowRuleBase>();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		foreach (PcInputsRuleDef pcInputRuleDef in configuratorArgs.Configuration.PcInputsRuleDef)
		{
			PcInput targetInput = configuratorArgs.Configuration.PcInputs.Find((PcInput input) => input.InputName == pcInputRuleDef.TargetInputName);
			PcInput pcInput = configuratorArgs.Configuration.PcInputs.Find((PcInput input) => input.InputName == pcInputRuleDef.SourceInputName);
			PcInputsRule pcInputsRule = configuratorArgs.Configuration.PcInputsRule.Find((PcInputsRule inputRule) => inputRule.TargetInputName == pcInputRuleDef.TargetInputName && inputRule.RuleSeq == pcInputRuleDef.RuleSeq);
			string dataview = configuratorArgs.ConfiguratorHelper.GetDataview(pcInput.PageSeq);
			string key = $"{pcInputRuleDef.TargetInputName}-{dataview}-{pcInputsRule.TargetInputValue}-{pcInputsRule.RuleSeq}";
			if (dictionary.ContainsKey(key))
			{
				dictionary[key].Add(GenerateConditionForPcInputsRule(targetInput, pcInputRuleDef, "<>"));
				continue;
			}
			List<string> list2 = new List<string>();
			list2.Add(GenerateConditionForPcInputsRule(targetInput, pcInputRuleDef, "<>"));
			dictionary.Add(key, list2);
		}
		foreach (KeyValuePair<string, List<string>> item in dictionary)
		{
			string text = item.Key.Split(new char[1] { '-' })[0];
			string dataView = item.Key.Split(new char[1] { '-' })[1];
			string value = item.Key.Split(new char[1] { '-' })[2];
			string text2 = item.Key.Split(new char[1] { '-' })[3];
			RowRuleBase rowRuleBase = new RowRuleBase();
			rowRuleBase.Name = "inputRule_" + text + "_RuleSeq" + text2 + "_True";
			rowRuleBase.DataView = dataView;
			rowRuleBase.Condition = string.Join(" AND ", item.Value).Replace("<>", "=");
			List<object> list3 = new List<object>();
			list3.Add(new
			{
				action = "SetColumnValue",
				epBinding = "TransView.ImageEpBindingName",
				value = (configuratorArgs.ConfiguratorHelper.GetImageBlobName("TransView", text) ?? "")
			});
			list3.Add(new
			{
				action = "SetColumnValue",
				epBinding = "TransView.ImageInputName",
				value = text
			});
			list3.Add(new
			{
				action = "SetColumnValue",
				epBinding = "TransView.ImageFileName",
				value = value
			});
			rowRuleBase.Actions = list3.ToArray();
			list.Add(rowRuleBase);
		}
		foreach (KeyValuePair<string, List<string>> item2 in dictionary)
		{
			string text3 = item2.Key.Split(new char[1] { '-' })[0];
			string dataView2 = item2.Key.Split(new char[1] { '-' })[1];
			_ = item2.Key.Split(new char[1] { '-' })[2];
			string text4 = item2.Key.Split(new char[1] { '-' })[3];
			RowRuleBase rowRuleBase2 = new RowRuleBase();
			rowRuleBase2.Name = "inputRule_" + text3 + "_RuleSeq" + text4 + "_False";
			rowRuleBase2.DataView = dataView2;
			rowRuleBase2.Condition = string.Join(" OR ", item2.Value);
			List<object> list4 = new List<object>();
			list4.Add(new
			{
				action = "SetColumnValue",
				epBinding = (configuratorArgs.ConfiguratorHelper.GetImageBlobName("TransView", text3) ?? ""),
				value = ""
			});
			list4.Add(new
			{
				action = "SetColumnValue",
				epBinding = "TransView.ImageFileName",
				value = ""
			});
			rowRuleBase2.Actions = list4.ToArray();
			list.Add(rowRuleBase2);
		}
		return list;
	}

	private string GenerateConditionForPcInputsRule(PcInput targetInput, PcInputsRuleDef pcInputRuleDef, string conditionOperator)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(configuratorArgs.ConfiguratorHelper.GetDataview(targetInput.PageSeq) + "." + pcInputRuleDef.SourceInputName + " " + conditionOperator + " ");
		switch (pcInputRuleDef.InputType)
		{
		case "Decimal":
			stringBuilder.Append(pcInputRuleDef.SourceDecimalValue);
			break;
		case "Boolean":
			stringBuilder.Append(pcInputRuleDef.SourceLogicalValue.ToString().ToLower());
			break;
		case "Integer":
			stringBuilder.Append(pcInputRuleDef.SourceIntegerValue);
			break;
		default:
			stringBuilder.Append("'" + pcInputRuleDef.SourceCharacterValue + "'");
			break;
		}
		return stringBuilder.ToString();
	}
}
