using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models;

public class PeekFolder
{
	public string like { get; set; }

	public IEnumerable<string> peekForms { get; set; }
}
