using System;

namespace Epicor.MetaFX.Core.Models.Layout;

public class ComponentModelAttribute : Attribute
{
	public string Name { get; private set; }

	public ComponentModelAttribute(string name)
	{
		Name = name;
	}
}
