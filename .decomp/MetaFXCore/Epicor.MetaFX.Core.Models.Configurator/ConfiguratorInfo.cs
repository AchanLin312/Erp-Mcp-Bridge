using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class ConfiguratorInfo
{
	public string ConfigId { get; set; }

	public bool Approved { get; set; }

	public string AuditDesc { get; set; }

	public string Description { get; set; }

	public bool AprvRev { get; set; }

	public bool IsValidPwd { get; set; }

	public bool DispConfSummary { get; set; }

	public List<StatusExpr> StatusExpr { get; set; }

	public List<PcPageExpr> PcPageExpr { get; set; }

	public string DynamicAttributeClassId { get; set; }

	public ConfiguratorUDFunction ConfigUDFunc { get; set; }

	public ConfiguratorInfo()
	{
		StatusExpr = new List<StatusExpr>();
		PcPageExpr = new List<PcPageExpr>();
	}
}
