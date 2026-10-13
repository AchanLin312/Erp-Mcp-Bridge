using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models;

public class SearchFolder
{
	public string Folder { get; set; }

	public IEnumerable<string> SearchSubFolders { get; set; }
}
