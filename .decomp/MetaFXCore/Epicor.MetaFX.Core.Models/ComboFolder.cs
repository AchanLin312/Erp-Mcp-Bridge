using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models;

public class ComboFolder
{
	public string Folder { get; set; }

	public IEnumerable<string> ComboSubFolders { get; set; }
}
