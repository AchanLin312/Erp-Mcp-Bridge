using System.ComponentModel;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFXLayerFixRequest
{
	public string previousVersionMetaUIFolder { get; set; }

	[DefaultValue(false)]
	public bool loadPreviousVersionFromDb { get; set; }

	public string[] applications { get; set; }

	[DefaultValue(false)]
	public bool clearBackupData { get; set; }
}
