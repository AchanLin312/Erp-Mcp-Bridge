using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.BAQReport;

public class ReportDefinition
{
	public List<BAQReport> BAQReport { get; set; }

	public List<BAQRptOptionFld> BAQRptOptionFld { get; set; }

	public List<BAQRptFilter> BAQRptFilter { get; set; }

	public List<BAQRptSort> BAQRptSort { get; set; }

	public List<BAQRptSortFld> BAQRptSortFld { get; set; }
}
