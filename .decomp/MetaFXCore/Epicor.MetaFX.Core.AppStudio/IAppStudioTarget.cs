using System;
using System.Collections.Generic;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.AppStudio;

internal interface IAppStudioTarget
{
	void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc);

	void Publish(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc);

	List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc, bool validateBeforePublish = false);
}
