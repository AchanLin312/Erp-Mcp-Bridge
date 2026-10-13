using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

public class DynamicList
{
	public string InputName { get; set; }

	public long ListSeq { get; set; }

	public string Condition { get; set; }

	public SortFields[] List { get; set; }

	public string InitVal { get; set; }

	public string BaqProgramName { get; set; }

	public string BaqDispValue { get; set; }

	public string BaqInputVal { get; set; }

	public string ListDataSource { get; set; }

	public string TblLookupId { get; set; }

	public string TblLookupFunc { get; set; }

	public string UserDefinedName { get; set; }

	public string UserDefinedFunctionType { get; set; }

	public string UserDefinedReturnType { get; set; }

	public List<DynLstParam> Parameters { get; set; }

	public List<DynLstCriteria> Criterias { get; set; }
}
