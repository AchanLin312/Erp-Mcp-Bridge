using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Applications;

public class ApplicationType
{
	public string Type { get; set; }

	public List<ApplicationSubType> SubTypes { get; set; }

	public string FilePath { get; set; }

	public ApplicationType()
	{
		SubTypes = new List<ApplicationSubType>();
	}
}
