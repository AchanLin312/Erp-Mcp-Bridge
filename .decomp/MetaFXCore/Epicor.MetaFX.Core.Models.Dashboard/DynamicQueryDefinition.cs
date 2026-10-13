using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DynamicQueryDefinition
{
	public List<DynamicQuery> DynamicQuery { get; set; }

	public List<object> QueryCtrl { get; set; }

	public List<object> QueryCtrlValues { get; set; }

	public List<object> QueryCustomAction { get; set; }

	public List<object> QueryExecuteSetting { get; set; }

	public List<object> QueryParameter { get; set; }

	public List<QuerySubQuery> QuerySubQuery { get; set; }

	public List<QueryRelation> QueryRelation { get; set; }

	public List<QueryRelationField> QueryRelationField { get; set; }

	public List<QuerySortBy> QuerySortBy { get; set; }

	public List<QueryWhereItem> QueryWhereItem { get; set; }

	public List<object> QueryGroupBy { get; set; }

	public List<QueryTable> QueryTable { get; set; }

	public List<QueryField> QueryField { get; set; }

	public List<QueryFieldAttribute> QueryFieldAttribute { get; set; }

	public List<object> QueryFunctionCall { get; set; }

	public List<object> QueryUpdateField { get; set; }

	public List<object> QueryUpdateSettings { get; set; }

	public List<object> QueryValueSetItems { get; set; }

	public List<object> ExtensionTables { get; set; }
}
