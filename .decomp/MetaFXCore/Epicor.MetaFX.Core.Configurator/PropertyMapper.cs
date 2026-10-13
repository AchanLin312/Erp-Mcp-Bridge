using System.Reflection;

namespace Epicor.MetaFX.Core.Configurator;

public class PropertyMapper<TParent, TChild> where TParent : class where TChild : class
{
	public static void Copy(TParent parent, TChild child)
	{
		PropertyInfo[] properties = parent.GetType().GetProperties();
		PropertyInfo[] properties2 = child.GetType().GetProperties();
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			PropertyInfo[] array2 = properties2;
			foreach (PropertyInfo propertyInfo2 in array2)
			{
				if (propertyInfo.Name == propertyInfo2.Name && propertyInfo.PropertyType == propertyInfo2.PropertyType)
				{
					propertyInfo2.SetValue(child, propertyInfo.GetValue(parent));
					break;
				}
			}
		}
	}
}
