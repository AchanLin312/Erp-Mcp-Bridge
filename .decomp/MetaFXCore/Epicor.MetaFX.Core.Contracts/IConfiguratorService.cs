using System.Collections.Generic;
using System.Data;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Layers;

namespace Epicor.MetaFX.Core.Contracts;

public interface IConfiguratorService
{
	Configuration GetConfiguration(string configId);

	DataSet GetQueryEmptyResultSetByID(string baqQueryId);

	ConfiguratorUDFunction GetUDFunction(UDGetRowsRequest request);

	DynamicAttrClass GetDynamicAttrClass(string attrClassId);

	List<EpMetaFXAuditLog> GetApproveLogs(string configId);

	Configuration UpdateMethodsAndCreatePage(string configId);

	UpdateConfiguratorResponse UpdateConfigurator(UpdateConfiguratorRequest request);

	UpdateConfiguratorResponse ApproveConfiguratorBeforeBulkPublish(string configurationId);
}
