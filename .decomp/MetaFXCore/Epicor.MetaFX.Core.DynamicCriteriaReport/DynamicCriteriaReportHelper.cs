using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.DynamicCriteriaReport;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.EpControls;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;

namespace Epicor.MetaFX.Core.DynamicCriteriaReport;

public class DynamicCriteriaReportHelper
{
	public const string SQLTYPE_DATETIME = "datetime";

	public const string SQLTYPE_STRING = "string";

	public const string SQLTYPE_INT = "int";

	public const string SQLTYPE_INT64 = "int64";

	public const string SQLTYPE_DECIMAL = "decimal";

	public const string SQLTYPE_BOOL = "bool";

	public const string REPORTPARAM = "ReportParam";

	public const string REPORTPARAMSTYLES = "ReportParamStyles";

	public const string CONSTANT = "Constant";

	public const string TODAY = "Today";

	public Component CreateFilterControl(RptCriteriaFilter filter)
	{
		string id = "chp" + filter.FilterName;
		string epBinding = BuildEpBindingName(filter.FilterName);
		return new Component
		{
			SourceTypeId = "ep-search-chip-selection",
			Id = id,
			Model = new SearchChipSelection
			{
				Id = id,
				Hidden = false,
				Disabled = false,
				LabelText = filter.FilterLabel,
				EpBinding = epBinding,
				Guid = Guid.NewGuid().ToString(),
				EmptyLabelText = "All " + filter.FilterLabel + " selected",
				SingularLabelText = filter.FilterLabel + " selected",
				PluralLabelText = filter.FilterLabel + " selected"
			}
		};
	}

	public Component CreateComboBoxControl(RptCriteriaPrompt prompt)
	{
		string id = "cmb" + BuildPromptControlName(prompt);
		string epBinding = BuildEpBindingName(prompt.PromptName);
		List<string> list = new List<string>();
		Component obj = new Component
		{
			Id = id,
			SourceTypeId = "erp-combo-box",
			Model = new ErpComboBox
			{
				Id = id,
				Guid = Guid.NewGuid().ToString(),
				LabelText = prompt.Label,
				EpBinding = epBinding,
				TabIndex = prompt.DisplayOrder,
				Hidden = false,
				Disabled = false,
				Readonly = false,
				IsKeyField = false,
				TextField = "Description",
				ValueField = "Value",
				Svc = "Ice.BO.RptDataDefSvc",
				SvcPath = "GetDropDownPromptRows",
				ParameterName = "returnObj",
				RestArguments = new List<string>()
			}
		};
		list.Add("rptDefID = '" + prompt.RptDefID + "'");
		list.Add("rptCriteriaSetID = '" + prompt.RptCriteriaSetID + "'");
		list.Add($"promptID = '{prompt.PromptID}'");
		(obj.Model as ErpComboBox).RestArguments = list;
		return obj;
	}

	public string BuildPromptControlName(RptCriteriaPrompt prompt)
	{
		return RemoveWhitespace(prompt.PromptName ?? "");
	}

	public string BuildEpBindingName(string fieldName, bool isToken = false)
	{
		fieldName = RemoveWhitespace(fieldName);
		if (isToken)
		{
			return "ReportParam." + fieldName + "Token";
		}
		return "ReportParam." + fieldName;
	}

	public string GetTypeStringFromSqlType(string sqlType)
	{
		switch (sqlType.ToLower())
		{
		case "bit":
			return "bool";
		case "date":
		case "datetime":
		case "datetimeoffset":
			return "DateTime";
		case "decimal":
			return "decimal";
		case "int":
			return "int";
		case "long":
			return "Int64";
		case "nvarchar":
		case "sysname":
		case "nchar":
			return "string";
		case "uniqueidentifier":
			return "Guid";
		default:
			throw new Exception("Unknown dbType: " + sqlType);
		}
	}

	public Event CreateGetNewParametersEvent(RptDataDef report)
	{
		Event obj = new Event();
		obj.Name = "GetNewParameters";
		obj.Action = new
		{
			type = "erpRest",
			param = new
			{
				svc = "%svc%",
				svcPath = "GetNewDynamicCriteriaReportParam",
				requestMethod = "POST",
				erpRequestMethod = "ErpPostWithDataViewProcessing",
				methodParameters = new object[1]
				{
					new
					{
						field = "reportID",
						value = report.RptDefID
					}
				},
				erpRestPostArgs = new
				{
					responseParamArgs = new object[2]
					{
						new
						{
							viewName = "%paramModel%",
							paramName = "%param%",
							responsePath = "returnObj"
						},
						new
						{
							viewName = "StyleList",
							paramName = "ReportStyle",
							responsePath = "returnObj"
						}
					}
				}
			},
			onSuccess = new
			{
				actions = new object[3]
				{
					new
					{
						type = "event-column-disable",
						dataTable = "%paramModel%"
					},
					new
					{
						type = "nextEvent",
						value = "initialize"
					},
					new
					{
						type = "event-column-enable",
						dataTable = "%paramModel%"
					}
				}
			}
		};
		return obj;
	}

	public Event CreateInitializeEvent(DynamicCriteriaReportRequest request)
	{
		object[] columns = BuildDefaultColumnValues(request);
		Event obj = new Event();
		obj.Name = "initialize";
		obj.Action = new
		{
			type = "updateRow",
			param = new object[1]
			{
				new { columns }
			}
		};
		return obj;
	}

	private object[] BuildDefaultColumnValues(DynamicCriteriaReportRequest request)
	{
		List<object> columns = new List<object>();
		request.ReportCriteriaDefinitions.ForEach(delegate(ReportCriteriaDefinition def)
		{
			foreach (RptCriteriaPrompt item2 in def.RptCriteriaPrompt)
			{
				BuildPromptControlName(item2);
				string typeStringFromSqlType = GetTypeStringFromSqlType(item2.DataType);
				if (!string.IsNullOrEmpty(item2.DefaultValue))
				{
					switch (typeStringFromSqlType.ToLower())
					{
					case "datetime":
					{
						bool isToken = IsDateToken(item2.DataType, item2.DefaultValue);
						columns.Add(BuildDefaultColumnValue(item2, item2.DefaultValue, null, isToken));
						break;
					}
					case "string":
						columns.Add(BuildDefaultColumnValue(item2, item2.DefaultValue));
						break;
					case "bool":
					{
						if (bool.TryParse(item2.DefaultValue, out var result3))
						{
							columns.Add(BuildDefaultColumnValue(item2, result3.ToString(), "bool"));
						}
						break;
					}
					case "int":
					case "int64":
					{
						if (int.TryParse(item2.DefaultValue, out var result2))
						{
							columns.Add(BuildDefaultColumnValue(item2, result2.ToString(), "number"));
						}
						break;
					}
					case "decimal":
					{
						if (decimal.TryParse(item2.DefaultValue, out var result))
						{
							columns.Add(BuildDefaultColumnValue(item2, result.ToString(), "number"));
						}
						break;
					}
					}
				}
				else
				{
					string text = typeStringFromSqlType.ToLower();
					if (!(text == "datetime"))
					{
						if (text == "bool")
						{
							columns.Add(BuildDefaultColumnValue(item2, "False", "bool"));
						}
					}
					else
					{
						var item = new
						{
							epBinding = BuildEpBindingName(item2.PromptName),
							value = "{Constant.Today}"
						};
						columns.Add(item);
					}
				}
			}
		});
		return columns.ToArray();
	}

	private object BuildDefaultColumnValue(RptCriteriaPrompt prompt, object value, string dataType = null, bool isToken = false)
	{
		if (dataType == null)
		{
			return new
			{
				epBinding = BuildEpBindingName(prompt.PromptName, isToken),
				value = value
			};
		}
		return new
		{
			epBinding = BuildEpBindingName(prompt.PromptName),
			value = value,
			dataType = dataType
		};
	}

	public Event CreatePrintPreviewEvent(RptDataDef report)
	{
		return new Event
		{
			Name = "printpreview",
			Trigger = new Trigger
			{
				Hook = "onClick",
				Type = "Control",
				Target = "printpreview"
			},
			Action = GetPresubmitAction(report, "executePrintPreview")
		};
	}

	public Event CreateGenerateOnlyEvent(RptDataDef report)
	{
		return new Event
		{
			Name = "generateOnly",
			Trigger = new Trigger
			{
				Hook = "onClick",
				Type = "Control",
				Target = "generateOnly"
			},
			Action = GetPresubmitAction(report, "executeGenerateOnly")
		};
	}

	public Event CreateSubmitEvent()
	{
		Event obj = new Event();
		obj.Name = "submitToAgent";
		obj.Action = new
		{
			type = "erpRest",
			param = new
			{
				svc = "%svc%",
				svcPath = "SubmitToAgent",
				requestMethod = "POST",
				erpRequestMethod = "ErpPostWithDataViewProcessing",
				methodParameters = new object[5]
				{
					new
					{
						field = "ds",
						value = new { },
						@params = new object[3]
						{
							new
							{
								field = "extensionTables",
								value = new object[0],
								isArray = true
							},
							new
							{
								field = "%param%",
								model = "%paramModel%",
								isArray = true
							},
							new
							{
								field = "ReportStyle",
								model = "StyleList",
								isArray = true
							}
						}
					},
					new
					{
						field = "agentID",
						value = ""
					},
					new
					{
						field = "agentSchedNum",
						epBinding = "%paramModel%.AgentSchedNum"
					},
					new
					{
						field = "agentTaskNum",
						epBinding = "%paramModel%.AgentTaskNum"
					},
					new
					{
						field = "maintProgram",
						value = "%maintProgram%"
					}
				}
			},
			onSuccess = new
			{
				actions = new object[0]
			},
			onError = new
			{
				actions = new object[1]
				{
					new
					{
						type = "return-error"
					}
				}
			}
		};
		return obj;
	}

	public Tuple<Event, MessageDetail> CreateSearchFilterEvent(RptCriteriaFilter filter, string rootPath, string lookupField, List<RptCriteriaFilterAdapterXRef> adapterXref)
	{
		MessageDetail item = null;
		string text = adapterXref.Find((RptCriteriaFilterAdapterXRef xref) => xref.RptCriteriaFilterAdapter == filter.AdapterName)?.LikeField;
		if (!string.IsNullOrEmpty(text))
		{
			if (!File.Exists(GetFilePath(Path.Combine(rootPath, "shared", "search", text, "default"), "search")))
			{
				item = new MessageDetail
				{
					Message = "Search " + text + " does not exists.",
					Type = "warning"
				};
			}
		}
		else
		{
			item = new MessageDetail
			{
				Message = "Search does not exists for " + lookupField + ".",
				Type = "warning"
			};
		}
		Event obj = new Event();
		obj.Name = BuildEpBindingName(filter.FilterName);
		Trigger trigger = new Trigger();
		trigger.Hook = "onClick";
		trigger.Type = "EpBinding";
		trigger.Target = BuildEpBindingName(filter.FilterName);
		obj.Trigger = trigger;
		obj.Action = new
		{
			type = "showSearch",
			param = new
			{
				searchOptions = new
				{
					selectMode = "MultiSelect",
					like = (text ?? ""),
					searchForm = "default",
					sortByColumn = string.Empty
				}
			},
			onSuccess = new
			{
				actions = new object[3]
				{
					new
					{
						type = "event-column-disable",
						dataTable = "ReportParam"
					},
					new
					{
						type = "search-value-set",
						epBinding = BuildEpBindingName(filter.FilterName),
						value = "actionResult." + lookupField
					},
					new
					{
						type = "event-column-enable",
						dataTable = "ReportParam"
					}
				}
			}
		};
		return new Tuple<Event, MessageDetail>(obj, item);
	}

	public List<RowRuleBase> CreateHidePanelsRule(DynamicCriteriaReportRequest request)
	{
		List<RowRuleBase> rules = new List<RowRuleBase>();
		request.ReportCriteriaDefinitions.ForEach(delegate(ReportCriteriaDefinition def)
		{
			foreach (RptCriteriaSet criteria in def.RptCriteriaSet)
			{
				RowRuleBase rowRuleBase = new RowRuleBase
				{
					Name = RemoveWhitespace("hide" + def.RptDataDef[0].RptDefID + "_" + criteria.RptCriteriaSetID + "Panel"),
					DataView = "ReportParamStyles",
					Condition = "ReportParamStyles.RptDefID = '" + def.RptDataDef[0].RptDefID + "' and ReportParamStyles.SelectedCriteriaSet = '" + criteria.RptCriteriaSetID + "'"
				};
				List<object> actions = new List<object>();
				request.ReportCriteriaDefinitions.ForEach(delegate(ReportCriteriaDefinition reportCriteriaDefinition)
				{
					foreach (RptCriteriaSet item in reportCriteriaDefinition.RptCriteriaSet.Where((RptCriteriaSet o) => def.RptDataDef[0].RptDefID != reportCriteriaDefinition.RptDataDef[0].RptDefID || o.RptCriteriaSetID != criteria.RptCriteriaSetID))
					{
						actions.Add(new
						{
							action = "SettingStyle.Invisible",
							epBinding = RemoveWhitespace("ReportParamStyles." + reportCriteriaDefinition.RptDataDef[0].RptDefID + "_" + item.RptCriteriaSetID)
						});
					}
				});
				rowRuleBase.Actions = actions.ToArray();
				rules.Add(rowRuleBase);
			}
		});
		return rules;
	}

	private string GetFilePath(string folderPath, string type)
	{
		return Path.Combine(folderPath, GetFileName(folderPath, type));
	}

	private string GetFileName(string folderPath, string file)
	{
		if (File.Exists(Path.Combine(folderPath, file + ".jsonc")))
		{
			return file + ".jsonc";
		}
		return file + ".json";
	}

	public bool IsDateToken(string sqlDataType, string columnValue)
	{
		if (IsEqual(sqlDataType, "date") || IsEqual(sqlDataType, "datetime"))
		{
			if (!string.IsNullOrEmpty(columnValue))
			{
				return columnValue.StartsWith("&");
			}
			return false;
		}
		return false;
	}

	private object GetPresubmitAction(RptDataDef report, string nextEvent)
	{
		return new
		{
			type = "updateRow",
			param = new object[1]
			{
				new
				{
					columns = new object[2]
					{
						new
						{
							epBinding = "ReportParam.ReportID",
							value = report.RptDefID
						},
						new
						{
							epBinding = "ReportParam.UserID",
							value = "%session.context.user%"
						}
					}
				}
			},
			onSuccess = new
			{
				actions = new object[1]
				{
					new
					{
						type = "nextEvent",
						value = nextEvent
					}
				}
			}
		};
	}

	public string RemoveWhitespace(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return value.Replace(" ", string.Empty);
		}
		return value;
	}

	public bool IsEqual(string str1, string str2)
	{
		if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}
}
