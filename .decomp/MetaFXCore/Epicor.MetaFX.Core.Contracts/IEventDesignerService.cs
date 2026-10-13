using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Events;

namespace Epicor.MetaFX.Core.Contracts;

public interface IEventDesignerService
{
	bool SaveDesigner(EventDesigner request);

	void DeleteDesigner(EventDesignerRequest request);

	EventDesigner GetDesigner(EventDesignerRequest request);

	List<EventDesigner> GetDesigners(EventDesignersRequest request);
}
