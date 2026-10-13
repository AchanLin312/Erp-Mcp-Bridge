using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Epicor.MetaFX.Core.Models.Configurator;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

internal class BAQHelper
{
	private readonly ConfiguratorArgs configuratorArgs;

	public BAQHelper(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
	}

	public List<object> GenerateBAQMethodParameters(PcDynLst dynamicList, List<PcDynLstParam> dynamicListParams)
	{
		List<object> commonProps = configuratorArgs.ConfiguratorHelper.GetCommonProps(dynamicList.ListDataSource, string.Empty, string.Empty, string.Empty, "true", string.Empty, new object[0]);
		string[] baqDisplayValues = GetBaqDisplayValues(dynamicList.BaqDispValue);
		commonProps.Add(new
		{
			prop = "textField",
			value = (baqDisplayValues.Any() ? ReplaceColumn(baqDisplayValues[0]) : string.Empty)
		});
		commonProps.Add(new
		{
			prop = "valueField",
			value = ReplaceColumn(dynamicList.BaqInputVal)
		});
		commonProps.Add(new
		{
			prop = "queryId",
			value = dynamicList.BaqProgramName
		});
		commonProps.Add(new
		{
			prop = "data"
		});
		commonProps.Add(new
		{
			prop = "filters",
			value = GenerateBaqCriteria(dynamicList)
		});
		return commonProps;
	}

	private List<string> GenerateBaqCriteria(PcDynLst dynamicList)
	{
		List<PcDynLstCriteria> list = configuratorArgs.Configuration.PcDynLstCriteria.Where((PcDynLstCriteria x) => x.InputName == dynamicList.InputName && x.ListSeq == dynamicList.ListSeq).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		DataTable dataTable = configuratorArgs.ConfiguratorService.GetQueryEmptyResultSetByID(dynamicList.BaqProgramName).Tables["Results"];
		string[] source = (from DataColumn x in dataTable.Columns
			select x.ColumnName).ToArray();
		string empty = string.Empty;
		for (int num = 0; num < list.Count; num++)
		{
			PcDynLstCriteria pcDynLstCriteria = list[num];
			string columnName = ReplaceColumn(pcDynLstCriteria.ColumnName);
			empty = string.Empty;
			if (num > 0)
			{
				stringBuilder.Append(string.IsNullOrEmpty(pcDynLstCriteria.Operator) ? " AND " : (" " + pcDynLstCriteria.Operator + " "));
			}
			if (!string.IsNullOrWhiteSpace(pcDynLstCriteria.LeftP))
			{
				stringBuilder.Append(" " + pcDynLstCriteria.LeftP + " ");
			}
			stringBuilder.Append(columnName);
			string text = source.FirstOrDefault((string column) => column.IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0);
			if (text != null)
			{
				empty = dataTable.Columns[columnName].DataType.Name;
			}
			if (pcDynLstCriteria.Condition.Equals("CONTAINS") || pcDynLstCriteria.Condition.Equals("BEGINS"))
			{
				stringBuilder.Append(empty.Equals("datetime") ? " = " : " LIKE ");
			}
			else
			{
				stringBuilder.Append(" " + pcDynLstCriteria.Condition + " ");
			}
			string columnValue = GetColumnValue(pcDynLstCriteria);
			if (pcDynLstCriteria.ValueFrom.Equals("BAQColumn"))
			{
				if (text != null && !empty.Equals("datetime"))
				{
					if (pcDynLstCriteria.Condition.Equals("CONTAINS"))
					{
						stringBuilder.Append(empty.Equals("string") ? ("'%'" + columnValue + "'%'") : ("'%' cast(" + columnValue + " as nvarchar)'%'"));
					}
					else if (pcDynLstCriteria.Condition.Equals("BEGINS"))
					{
						stringBuilder.Append(empty.Equals("string") ? (columnValue + "'%'") : ("cast(" + columnValue + "as nvarchar) + '%'"));
					}
					else
					{
						stringBuilder.Append(columnValue);
					}
				}
				else
				{
					stringBuilder.Append(GetColumnValue(pcDynLstCriteria));
				}
			}
			else if (text != null)
			{
				if (!empty.Equals("datetime"))
				{
					if (pcDynLstCriteria.Condition.Equals("CONTAINS"))
					{
						stringBuilder.Append("'%" + columnValue + "%'");
					}
					else if (pcDynLstCriteria.Condition.Equals("BEGINS"))
					{
						stringBuilder.Append("'" + columnValue + "%'");
					}
					else
					{
						stringBuilder.Append("'" + columnValue + "'");
					}
				}
				else
				{
					stringBuilder.Append("'" + columnValue + "'");
				}
			}
			if (!string.IsNullOrWhiteSpace(pcDynLstCriteria.RightP))
			{
				stringBuilder.Append(" " + pcDynLstCriteria.RightP + " ");
			}
		}
		return new List<string> { stringBuilder.ToString() };
	}

	private string[] GetBaqDisplayValues(string baqString)
	{
		return baqString.Split(new char[1] { '~' });
	}

	private string GetColumnValue(PcDynLstCriteria dynLstCriteria)
	{
		if (dynLstCriteria.ValueFrom.ToUpper().Equals("INPUTNAME"))
		{
			return "?[" + dynLstCriteria.ColumnValue + "]";
		}
		if (dynLstCriteria.ValueFrom.ToUpper().Equals("BAQCOLUMN"))
		{
			return ReplaceColumn(dynLstCriteria.ColumnValue);
		}
		return ParseSpecialCharactersForLike(dynLstCriteria.ColumnValue);
	}

	private string ParseSpecialCharactersForLike(object value)
	{
		return value.ToString().Replace("'", "''").Replace("[", "~~[")
			.Replace("]", "~~]")
			.Replace("~~[", "[[]")
			.Replace("~~]", "[]]")
			.Replace("*", "[*]")
			.Replace("%", "[%]");
	}

	private string ReplaceColumn(string val)
	{
		if (string.IsNullOrEmpty(val))
		{
			return string.Empty;
		}
		return val.Replace('.', '_');
	}
}
