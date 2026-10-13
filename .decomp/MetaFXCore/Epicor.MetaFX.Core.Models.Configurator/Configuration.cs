using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class Configuration
{
	public List<PcStatus> PcStatus { get; set; }

	public List<PcPage> PcPage { get; set; }

	public List<PcInput> PcInputs { get; set; }

	public List<object> PcInputsLayerHeader { get; set; }

	public List<object> PcInputsLayerDetail { get; set; }

	public List<PcDynLst> PcDynLst { get; set; }

	public List<PcDynLstCriteria> PcDynLstCriteria { get; set; }

	public List<object> PcDynLstExpr { get; set; }

	public List<PcDynLstParam> PcDynLstParams { get; set; }

	public List<PcExpr> PcInputsExpr { get; set; }

	public List<PcInputsRule> PcInputsRule { get; set; }

	public List<PcInputsRuleDef> PcInputsRuleDef { get; set; }

	public List<object> QBuildObj { get; set; }

	public List<object> QBuildMapping { get; set; }

	public List<PcExpr> PcPageExpr { get; set; }

	public List<PcStatusExpr> PcStatusExpr { get; set; }

	public List<object> ExtensionTables { get; set; }
}
