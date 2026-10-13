using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Dashboard;

internal class BAQWhereItemHelper
{
	private readonly DashboardHelper dashboardHelper;

	public BAQWhereItemHelper(DashboardHelper dashboardHelper)
	{
		this.dashboardHelper = dashboardHelper;
	}

	public BAQWhereItem GetBAQWhereItem(string columnName, string tableName, string condition, string value, bool compareColumns, bool isEpBinding, string dataType, string baqColumn = null, bool honorNull = false, bool isRequired = false)
	{
		BAQWhereItem bAQWhereItem = new BAQWhereItem();
		bAQWhereItem.ColumnName = columnName;
		bAQWhereItem.TableName = tableName;
		bAQWhereItem.Condition = condition;
		bAQWhereItem.CompareColumns = compareColumns;
		bAQWhereItem.Value = value.Replace("'", "''");
		bAQWhereItem.FilterValue = bAQWhereItem.Value;
		bAQWhereItem.IsBinding = isEpBinding;
		if (bAQWhereItem.Condition.Equals("BEGINS", StringComparison.OrdinalIgnoreCase))
		{
			bAQWhereItem.Condition = "starts";
			if (!bAQWhereItem.CompareColumns)
			{
				bAQWhereItem.FilterValue = (bAQWhereItem.IsBinding ? dashboardHelper.GetEpBindingValue(dataType, bAQWhereItem.Value) : ("'" + bAQWhereItem.Value.Replace("*", "%").Replace(".", "%") + "%'"));
			}
		}
		else if (bAQWhereItem.Condition.Equals("MATCHES", StringComparison.OrdinalIgnoreCase))
		{
			bAQWhereItem.Condition = "contains";
			if (!bAQWhereItem.CompareColumns)
			{
				bAQWhereItem.FilterValue = (bAQWhereItem.IsBinding ? dashboardHelper.GetEpBindingValue(dataType, bAQWhereItem.Value) : ("'" + bAQWhereItem.Value.Replace("*", "%").Replace(".", "%") + "%'"));
			}
		}
		else if (!bAQWhereItem.CompareColumns)
		{
			if (bAQWhereItem.IsBinding)
			{
				bAQWhereItem.FilterValue = dashboardHelper.GetEpBindingValue(dataType, bAQWhereItem.Value, isRequired);
			}
			else
			{
				bAQWhereItem.FilterValue = "'" + bAQWhereItem.Value + "'";
			}
		}
		if (bAQWhereItem.CompareColumns)
		{
			bAQWhereItem.FilterValue = bAQWhereItem.FilterValue ?? "";
		}
		bAQWhereItem.BAQColumn = (string.IsNullOrEmpty(baqColumn) ? (bAQWhereItem.TableName + "_" + bAQWhereItem.ColumnName) : baqColumn);
		bAQWhereItem.HonorNull = honorNull;
		return bAQWhereItem;
	}

	public BAQWhereItem GetClientBAQWhereItem(string columnName, string tableName, string condition, string value, bool compareColumns, bool isEpBinding, string baqColumn = null, bool honorNull = false)
	{
		BAQWhereItem bAQWhereItem = new BAQWhereItem();
		bAQWhereItem.ColumnName = columnName;
		bAQWhereItem.TableName = tableName;
		bAQWhereItem.Condition = condition;
		bAQWhereItem.CompareColumns = compareColumns;
		bAQWhereItem.FilterValue = value;
		bAQWhereItem.IsBinding = isEpBinding;
		bAQWhereItem.BAQColumn = (string.IsNullOrEmpty(baqColumn) ? (bAQWhereItem.TableName + "_" + bAQWhereItem.ColumnName) : baqColumn);
		bAQWhereItem.HonorNull = honorNull;
		return bAQWhereItem;
	}

	public List<BAQWhereExpression> GetSearchTrackerFilters(Queue<BAQWhereItem> baqWhereItems, DashboardDataSet dashboardDataSet, DashboardView dashboardView)
	{
		List<BAQWhereExpression> list = new List<BAQWhereExpression>();
		Queue<BAQWhereItem> queue = new Queue<BAQWhereItem>(baqWhereItems);
		while (queue.Count > 0)
		{
			BAQWhereItem baqWhereItem = queue.Dequeue();
			BAQWhereExpression bAQWhereExpression = new BAQWhereExpression();
			string text = dashboardDataSet.DashboardViewColumns?.FirstOrDefault((DashboardViewColumns c) => c.QueryID == dashboardView.QueryID && c.ViewID == dashboardView.ViewID && c.ColumnName == baqWhereItem.BAQColumn)?.DataType;
			if (baqWhereItem.HonorNull)
			{
				bAQWhereExpression.Value = baqWhereItem.FilterValue.Insert(baqWhereItem.FilterValue.Length - 2, ",''");
				bAQWhereExpression.EmptyValue = dashboardHelper.GetDefaultValue(text);
			}
			else
			{
				bAQWhereExpression.Value = baqWhereItem.FilterValue;
				switch (text)
				{
				case "bigint":
				case "int":
				case "integer":
				case "decimal":
					bAQWhereExpression.EmptyValue = "0";
					break;
				}
			}
			bAQWhereExpression.Column = baqWhereItem.BAQColumn;
			bAQWhereExpression.Condition = baqWhereItem.Condition;
			bAQWhereExpression.HonorNull = baqWhereItem.HonorNull;
			list.Add(bAQWhereExpression);
		}
		return list;
	}
}
