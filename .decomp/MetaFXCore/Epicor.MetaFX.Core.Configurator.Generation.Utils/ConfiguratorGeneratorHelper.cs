using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

public class ConfiguratorGeneratorHelper
{
	public const string DATAVIEW_TRANSVIEW = "TransView";

	public const string EVENT_EXECUTEPAGEONLOADEVENTS = "ExecutePageOnLoadEvents";

	public const string EVENT_COLUMNCHANGED = "columnChanged";

	public const string EVENT_COLUMNCHANGING = "columnChanging";

	public const string EVENT_INITIALIZECOLUMNCHANGINGSETTINGS = "InitializeColumnChangingSettings";

	public const string EVENT_VALIDATECOLUMNCHANGING = "ValidateColumnChanging";

	public const string LAYOUT_ABSOLUTE = "Absolute";

	public const string LAYOUT_RESPONSIVE = "Responsive";

	public const string LABEL_CONTROL_PREFIX = "label_";

	public const string PAGE_PREFIX = "Page";

	public const string TRANSVIEW_INITIALIZING = "initializingConfigurator";

	public static readonly string[] CONTROLTYPES_WITH_NO_EVENTS = new string[4] { "Ice.Lib.Framework.EpiGroupBox, Ice.Lib.EpiClientLib", "Ice.Lib.Framework.EpiLabel, Ice.Lib.EpiClientLib", "Erp.Lib.Configurator.PcControls.EpiPcPictureBox, Erp.Lib.Configurator", "Erp.Lib.Configurator.PcControls.EpiPcBrowser, Erp.Lib.Configurator" };

	public static readonly string[] CONTROLTYPES_WITH_NO_ONINIT_EVENTS = new string[5] { "Ice.Lib.Framework.EpiGroupBox, Ice.Lib.EpiClientLib", "Ice.Lib.Framework.EpiLabel, Ice.Lib.EpiClientLib", "Erp.Lib.Configurator.PcControls.EpiPcPictureBox, Erp.Lib.Configurator", "Erp.Lib.Configurator.PcControls.EpiPcBrowser, Erp.Lib.Configurator", "Ice.Lib.Framework.EpiButton, Ice.Lib.EpiClientLib" };

	public string GetDataview(long pageSequence)
	{
		return string.Format("{0}{1}", "Page", pageSequence);
	}

	public string GetReadOnlyExpressionEventName(string inputName)
	{
		return "ReadOnlyExpression_" + inputName;
	}

	public string GetDynamicListExpressionEventName(string inputName, string listSeq)
	{
		return "DynamicListExpression_" + inputName + "_" + listSeq;
	}

	public string GetDynamicListExpressionSetPropertyName(string inputName, string listSeq)
	{
		return "DynamicListExpression_" + inputName + "_Set_" + listSeq;
	}

	public string GetUpdateImageEventName(string inputName)
	{
		return "UpdateImage_" + inputName;
	}

	public string GetImageBlobName(string dataViewName, string inputName)
	{
		return dataViewName + "." + inputName + "_Blob";
	}

	public dynamic GetInitialValue(string initialValue, string dataType)
	{
		if (string.IsNullOrEmpty(initialValue))
		{
			switch (dataType.ToLowerInvariant())
			{
			case "logical":
			case "boolean":
				return false;
			case "numeric":
			case "decimal":
			case "int32":
			case "int":
				return 0;
			case "string":
			case "date":
			case "datetime":
				return string.Empty;
			case "byte[]":
				return null;
			default:
				return string.Empty;
			}
		}
		switch (dataType.ToLowerInvariant())
		{
		case "logical":
		case "boolean":
			return PCConvert.ToBoolean(initialValue.ToLowerInvariant());
		case "numeric":
		case "decimal":
		{
			PCConvert.TryParse(initialValue, out decimal result2);
			return result2;
		}
		case "int32":
		case "int":
		{
			PCConvert.TryParse(initialValue, out int result);
			return result;
		}
		case "string":
		case "date":
		case "datetime":
			return initialValue;
		case "byte[]":
			return null;
		default:
			return string.Empty;
		}
	}

	public List<object> GetCommonProps(string listDataSource, string svc, string svcPath, string isDelimited, string invalidated, string value, object[] list)
	{
		List<object> list2 = new List<object>();
		if (listDataSource != "BAQ")
		{
			list2.Add(new
			{
				prop = "svc",
				value = svc
			});
			list2.Add(new
			{
				prop = "svcPath",
				value = svcPath
			});
			list2.Add(new
			{
				prop = "isDelimited",
				value = isDelimited
			});
			list2.Add(new
			{
				prop = "data"
			});
		}
		list2.Add(new
		{
			prop = "invalidated",
			value = invalidated
		});
		list2.Add(new
		{
			prop = "list",
			value = list
		});
		return list2;
	}

	public string GetPageEventName(long pageSequence, string typeCode)
	{
		return typeCode + "_" + GetDataview(pageSequence);
	}

	public string GetDynamicAttributeEventName(string inputName)
	{
		return "DynamicAttribute_" + inputName;
	}

	public string GetConfiguratorEventName(string typeCode)
	{
		return "Configurator_" + typeCode;
	}

	public string BuildDynAttrListValues(DynamicAttrClass dynamicAttrClass, string company, string schemaName, string tableName, string attrClassId, string attrId)
	{
		char c = '~';
		char c2 = '`';
		string text = string.Empty;
		foreach (DynAttrClassDtlListValPartialRow item in SelectDynAttrClassDtlListVal(dynamicAttrClass, company, schemaName, tableName, attrClassId, attrId))
		{
			text = text + item.Code + c2 + item.Description + c;
		}
		if (!string.IsNullOrEmpty(text))
		{
			text = text.TrimEnd(new char[1] { c });
		}
		return text;
	}

	public SortFields[] GetComboFields(string comboStr)
	{
		string[] array = comboStr.Split(new char[1] { '~' });
		List<SortFields> list = new List<SortFields>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new char[1] { '`' });
			if (array3.Count() > 1)
			{
				list.Add(new SortFields
				{
					Display = array3[1],
					Value = array3[0]
				});
			}
		}
		return list.ToArray();
	}

	public DataFields[] GetRadioFields(string comboStr)
	{
		List<DataFields> list = new List<DataFields>();
		if (string.IsNullOrEmpty(comboStr))
		{
			return list.ToArray();
		}
		string[] array = comboStr.Split(new char[1] { '~' });
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { '`' });
			if (array2.Count() > 1)
			{
				list.Add(new DataFields
				{
					Value = array2[0],
					Label = array2[1]
				});
			}
		}
		return list.ToArray();
	}

	public DynAttrClassDtl FindDynAttrClassDtl(DynamicAttrClass dynamicAttrClass, string company, string schemaName, string tableName, string attrClassId, string attrId)
	{
		return dynamicAttrClass?.DynAttrClassDtl.Find((DynAttrClassDtl row) => row.Company == company && row.RelatedToSchemaName == schemaName && row.RelatedToTableName == tableName && row.AttrClassID == attrClassId && row.AttributeID == attrId);
	}

	public void GetMinMax(DynAttrClassDtl dynAttrClassDtl, out double minValue, out double maxValue)
	{
		minValue = 0.0;
		maxValue = 0.0;
		if (dynAttrClassDtl.MinDecimal != 0.0)
		{
			minValue = dynAttrClassDtl.MinDecimal;
		}
		if (dynAttrClassDtl.MinInteger != 0.0)
		{
			minValue = dynAttrClassDtl.MinInteger;
		}
		if (dynAttrClassDtl.MaxDecimal != 0.0)
		{
			maxValue = dynAttrClassDtl.MaxDecimal;
		}
		if (dynAttrClassDtl.MaxInteger != 0.0)
		{
			maxValue = dynAttrClassDtl.MaxInteger;
		}
	}

	private List<DynAttrClassDtlListValPartialRow> SelectDynAttrClassDtlListVal(DynamicAttrClass dynamicAttrClass, string company, string schemaName, string tableName, string attrClassId, string attrId)
	{
		List<DynAttrClassDtlListValPartialRow> list = new List<DynAttrClassDtlListValPartialRow>();
		foreach (DynAttrClassDtlListVal item in dynamicAttrClass.DynAttrClassDtlListVal.FindAll((DynAttrClassDtlListVal row) => row.Company == company && row.RelatedToSchemaName == schemaName && row.RelatedToTableName == tableName && row.AttrClassID == attrClassId && row.AttributeID == attrId))
		{
			list.Add(new DynAttrClassDtlListValPartialRow
			{
				Code = item.Code,
				Description = item.Description
			});
		}
		return list;
	}

	public object ConvertToAction(List<object> actions)
	{
		return CreateConditionAction("true", actions);
	}

	public object CreateConditionAction(string expression, List<object> actions)
	{
		return new
		{
			type = "condition",
			param = new
			{
				expression = expression,
				onSuccess = new { actions }
			}
		};
	}

	public object CreateNextEventAction(string callingEvent)
	{
		return new
		{
			type = "nextEvent",
			value = callingEvent
		};
	}

	public object CreateNextEventAction(string callingEvent, object param)
	{
		return new
		{
			type = "nextEvent",
			value = callingEvent,
			param = param
		};
	}

	public object CreateUpdateRowAction(string epBinding, object value)
	{
		return new
		{
			type = "updateRow",
			param = new object[1]
			{
				new
				{
					columns = new[]
					{
						new { epBinding, value }
					}
				}
			}
		};
	}
}
