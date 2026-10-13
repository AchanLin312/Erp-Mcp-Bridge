using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.BAQReport;

public class BAQDefinition
{
	public List<DynamicQueryDesigner> DynamicQueryDesigner { get; set; }

	public List<QueryCtrlDesigner> QueryCtrlDesigner { get; set; }

	public List<QueryCtrlValuesDesigner> QueryCtrlValuesDesigner { get; set; }

	public List<QueryCustomActionDesigner> QueryCustomActionDesigner { get; set; }

	public List<QueryExecuteSettingDesigner> QueryExecuteSettingDesigner { get; set; }

	public List<QueryParameterDesigner> QueryParameterDesigner { get; set; }

	public List<QuerySubQueryDesigner> QuerySubQueryDesigner { get; set; }

	public List<QueryRelationDesigner> QueryRelationDesigner { get; set; }

	public List<QueryRelationFieldDesigner> QueryRelationFieldDesigner { get; set; }

	public List<QuerySortByDesigner> QuerySortByDesigner { get; set; }

	public List<QueryWhereItemDesigner> QueryWhereItemDesigner { get; set; }

	public List<QueryGroupByDesigner> QueryGroupByDesigner { get; set; }

	public List<QueryTableDesigner> QueryTableDesigner { get; set; }

	public List<QueryFieldDesigner> QueryFieldDesigner { get; set; }

	public List<QueryFieldAttributeDesigner> QueryFieldAttributeDesigner { get; set; }

	public List<QueryFunctionCallDesigner> QueryFunctionCallDesigner { get; set; }

	public List<QueryUpdateFieldDesigner> QueryUpdateFieldDesigner { get; set; }

	public List<QueryUpdateSettingsDesigner> QueryUpdateSettingsDesigner { get; set; }

	public List<QueryValueSetItemsDesigner> QueryValueSetItemsDesigner { get; set; }

	public List<ExtensionTable> ExtensionTables { get; set; }
}
