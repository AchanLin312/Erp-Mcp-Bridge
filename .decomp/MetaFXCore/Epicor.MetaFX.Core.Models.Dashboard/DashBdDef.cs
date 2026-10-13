using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashBdDef
{
	public string Company { get; set; }

	public string ProductID { get; set; }

	public string GlbCompany { get; set; }

	public string DefinitionID { get; set; }

	public string Description { get; set; }

	public DateTime LastUpdated { get; set; }

	public string LastUpdatedBy { get; set; }

	public string DashBdVersion { get; set; }

	public int DataBaseVersion { get; set; }

	public bool Delivered { get; set; }

	public bool MobileAccess { get; set; }

	public string CGCCode { get; set; }

	public DateTime? LastDeployedDate { get; set; }

	public string LastDeployedBy { get; set; }

	public string DashboardSchema { get; set; }

	public string DashboardAssembly { get; set; }

	public bool WebAccess { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string Caption { get; set; }

	public bool HasDashboardAssembly { get; set; }

	public bool AllCompanies { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
