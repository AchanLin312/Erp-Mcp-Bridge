using System.Collections.Generic;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;

namespace Epicor.MetaFX.Core.Contracts;

public interface IUserDefinedApplicationService
{
	UserDefinedApplicationInfo[] GetUserDefinedViews();

	void CreateNewRow(string viewId);

	object GetSystemApplicationStatus(string viewId);

	bool IsUserDefinedApp(string viewId);

	bool IfUserHasSecurityAccess(string securityCode);

	void CreateNewRowForConversionLog(string viewId, string logDetails, bool isSuccess);

	List<ConversionLogMessageDetail> GetConversionLogs(string viewId);

	bool IsStoringConfigDumpAllowed();
}
