using System;
using System.Collections.Generic;
using System.IO;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.EpControls;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI.Event;

namespace Epicor.MetaFX.Core.BAQReport;

public class ReportHelper
{
	public const string SQLTYPE_DATETIME = "datetime";

	public const string SQLTYPE_STRING = "string";

	public const string SQLTYPE_INT = "int";

	public const string SQLTYPE_INT64 = "int64";

	public const string SQLTYPE_DECIMAL = "decimal";

	public const string SQLTYPE_BOOL = "bool";

	public const string REPORTPARAM = "ReportParam";

	public const string CONSTANT = "Constant";

	public const string TODAY = "Today";

	public Component CreateFilterControl(BAQRptFilter filter)
	{
		string id = "chp" + filter.FilterLabel;
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
				EpBinding = string.Format("{0}.Filter{1}", "ReportParam", filter.Seq),
				Guid = Guid.NewGuid().ToString(),
				EmptyLabelText = "All " + filter.FilterLabel + " selected",
				SingularLabelText = filter.FilterLabel + " selected",
				PluralLabelText = filter.FilterLabel + " selected"
			}
		};
	}

	public Component CreateSortControl(List<BAQRptSort> sort, string parentId = null)
	{
		string text = Guid.NewGuid().ToString();
		string text2 = Guid.NewGuid().ToString();
		return new Component
		{
			Id = "row-" + text.Substring(0, 5),
			ParentId = parentId,
			SourceTypeId = "metafx-div",
			Model = new ComponentModel
			{
				Id = "row-" + text.Substring(0, 5),
				Hidden = false,
				Guid = Guid.NewGuid().ToString()
			},
			Components = new List<Component>
			{
				new Component
				{
					Id = "col-" + text2.Substring(0, 5),
					ParentId = "row-" + text.Substring(0, 5),
					SourceTypeId = "metafx-col",
					Model = new ComponentModel
					{
						Id = "col-" + text2.Substring(0, 5),
						Guid = Guid.NewGuid().ToString(),
						Hidden = false,
						FlexBasis = "100%"
					},
					Components = new List<Component>
					{
						new Component
						{
							Id = "cboSortBy",
							ParentId = "col-" + text2.Substring(0, 5),
							SourceTypeId = "erp-combo-box",
							Model = new ErpComboBox
							{
								Id = "cboSortBy",
								Guid = Guid.NewGuid().ToString(),
								LabelText = "Sort By",
								Hidden = false,
								IsKeyField = false,
								Disabled = false,
								Readonly = false,
								EpBinding = "ReportParam.SortBy",
								DropDownStyle = "DropDown",
								IsDelimited = true,
								TextField = "display",
								ValueField = "value",
								List = GetSortFields(sort)
							}
						}
					}
				}
			}
		};
	}

	private SortFields[] GetSortFields(List<BAQRptSort> report)
	{
		List<SortFields> list = new List<SortFields>();
		foreach (BAQRptSort item in report)
		{
			list.Add(new SortFields
			{
				Display = item.SortID,
				Value = item.SortID
			});
		}
		return list.ToArray();
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

	public string GetOptionalFieldName(BAQRptOptionFld optionalField)
	{
		if (optionalField.DataTableID == "ReportParam")
		{
			return optionalField.FieldName;
		}
		return $"Option{optionalField.Seq:00}";
	}

	public Event CreateGetNewParametersEvent(Epicor.MetaFX.Core.Models.BAQReport.BAQReport report)
	{
		Event obj = new Event();
		obj.Name = "GetNewParameters";
		obj.Action = new
		{
			type = "erpRest",
			param = new
			{
				svc = "%svc%",
				svcPath = "GetNewBAQReportParam",
				erpRequestMethod = "ErpPostWithDataViewProcessing",
				methodParameters = new object[1]
				{
					new
					{
						field = "pcReportID",
						value = report.BAQRptID
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
				actions = new object[1]
				{
					new
					{
						type = "nextEvent",
						value = "initialize"
					}
				}
			}
		};
		return obj;
	}

	public Event CreateInitializeEvent(ReportDefinition reportDefinition)
	{
		object[] columns = BuildDefaultColumnValues(reportDefinition);
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

	public Event CreatePrintPreviewEvent(Epicor.MetaFX.Core.Models.BAQReport.BAQReport report)
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
				svcPath = "TransformAndSubmit",
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
				actions = new object[1]
				{
					new
					{
						type = "erp-message-handler",
						param = new
						{
							type = "Toast",
							level = "info",
							message = "{{strings.ReportSubmitted}}"
						}
					}
				}
			},
			onError = new
			{
				actions = new object[1]
				{
					new
					{
						type = "showErrorDialog"
					}
				}
			}
		};
		return obj;
	}

	public Event CreateGenerateOnlyEvent(Epicor.MetaFX.Core.Models.BAQReport.BAQReport report)
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

	public Tuple<Event, MessageDetail> CreateSearchFilterEvent(BAQRptFilter filter, string rootPath, QueryFieldDesigner queryFieldDefinition)
	{
		MessageDetail item = null;
		string text = queryFieldDefinition.LikeDataFieldTableID + "." + queryFieldDefinition.LikeDataFieldName;
		if (!File.Exists(GetFilePath(Path.Combine(rootPath, "shared", "search", text, "default"), "search")))
		{
			item = new MessageDetail
			{
				Message = "Search " + text + " does not exists.",
				Type = "warning"
			};
		}
		Event obj = new Event();
		obj.Name = string.Format("{0}.Filter{1}", "ReportParam", filter.Seq);
		Trigger trigger = new Trigger();
		trigger.Hook = "onClick";
		trigger.Type = "EpBinding";
		trigger.Target = string.Format("{0}.Filter{1}", "ReportParam", filter.Seq);
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
				actions = new object[1]
				{
					new
					{
						type = "setValue",
						epBinding = string.Format("{0}.Filter{1}", "ReportParam", filter.Seq),
						value = "actionResult." + queryFieldDefinition.LikeDataFieldName
					}
				}
			}
		};
		return new Tuple<Event, MessageDetail>(obj, item);
	}

	public object CreatePatch(Epicor.MetaFX.Core.Models.BAQReport.BAQReport report)
	{
		return new object[4]
		{
			new
			{
				id = "svc",
				value = "Ice.RPT.BAQReportSvc"
			},
			new
			{
				id = "param",
				value = "BAQReportParam"
			},
			new
			{
				id = "paramModel",
				value = "ReportParam"
			},
			new
			{
				id = "maintProgram",
				value = "Ice.UIRpt." + report.BAQRptID
			}
		};
	}

	private object GetPresubmitAction(Epicor.MetaFX.Core.Models.BAQReport.BAQReport report, string nextEvent)
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
							value = report.ReportID
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

	private object[] BuildDefaultColumnValues(ReportDefinition reportDefinition)
	{
		List<BAQRptOptionFld> bAQRptOptionFld = reportDefinition.BAQRptOptionFld;
		List<object> list = new List<object>();
		foreach (BAQRptOptionFld item2 in bAQRptOptionFld)
		{
			string optionalFieldName = GetOptionalFieldName(item2);
			string typeStringFromSqlType = GetTypeStringFromSqlType(item2.DataType);
			if (!string.IsNullOrEmpty(item2.DefaultValue))
			{
				switch (typeStringFromSqlType.ToLower())
				{
				case "datetime":
				{
					if (DateTime.TryParse(item2.DefaultValue, out var result2))
					{
						list.Add(BuildDefaultColumnValue(optionalFieldName, result2, "date"));
					}
					break;
				}
				case "string":
					list.Add(BuildDefaultColumnValue(optionalFieldName, item2.DefaultValue));
					break;
				case "bool":
				{
					if (bool.TryParse(item2.DefaultValue, out var result4))
					{
						list.Add(BuildDefaultColumnValue(optionalFieldName, result4.ToString(), "bool"));
					}
					break;
				}
				case "int":
				case "int64":
				{
					if (int.TryParse(item2.DefaultValue, out var result3))
					{
						list.Add(BuildDefaultColumnValue(optionalFieldName, result3.ToString(), "number"));
					}
					break;
				}
				case "decimal":
				{
					if (decimal.TryParse(item2.DefaultValue, out var result))
					{
						list.Add(BuildDefaultColumnValue(optionalFieldName, result.ToString(), "number"));
					}
					break;
				}
				}
			}
			else if (typeStringFromSqlType.ToLower() == "datetime")
			{
				var item = new
				{
					epBinding = "ReportParam." + optionalFieldName,
					value = "{Constant.Today}"
				};
				list.Add(item);
			}
		}
		if (reportDefinition.BAQRptSort != null && reportDefinition.BAQRptSort.Count > 0)
		{
			list.Add(BuildDefaultColumnValue("SortBy", reportDefinition.BAQRptSort[0].SortID));
		}
		return list.ToArray();
	}

	private object BuildDefaultColumnValue(string fieldName, object value, string dataType = null)
	{
		if (dataType == null)
		{
			return new
			{
				epBinding = "ReportParam." + fieldName,
				value = value
			};
		}
		return new
		{
			epBinding = "ReportParam." + fieldName,
			value = value,
			dataType = dataType
		};
	}

	private string GetFileName(string folderPath, string file)
	{
		if (File.Exists(Path.Combine(folderPath, file + ".jsonc")))
		{
			return file + ".jsonc";
		}
		return file + ".json";
	}

	private string GetFilePath(string folderPath, string type)
	{
		return Path.Combine(folderPath, GetFileName(folderPath, type));
	}
}
