using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DynamicQuery
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string AuthorID { get; set; }

	public string Description { get; set; }

	public string DisplayPhrase { get; set; }

	public bool IsShared { get; set; }

	public string Version { get; set; }

	public string CGCCode { get; set; }

	public bool XCompany { get; set; }

	public string GlbCompany { get; set; }

	public bool Updatable { get; set; }

	public bool ExtQuery { get; set; }

	public string ExtDatasourceName { get; set; }

	public bool SystemFlag { get; set; }

	public string Extension { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public string SecCode { get; set; }

	public bool AllCompanies { get; set; }

	public bool UseLiveDB { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
