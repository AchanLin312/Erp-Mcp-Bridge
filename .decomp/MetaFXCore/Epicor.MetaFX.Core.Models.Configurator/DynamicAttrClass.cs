using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class DynamicAttrClass
{
	public List<DynAttrClass> DynAttrClass { get; set; }

	public List<DynAttrClassDtl> DynAttrClassDtl { get; set; }

	public List<DynAttrClassDtlListVal> DynAttrClassDtlListVal { get; set; }

	public List<object> ExtensionTables { get; set; }
}
