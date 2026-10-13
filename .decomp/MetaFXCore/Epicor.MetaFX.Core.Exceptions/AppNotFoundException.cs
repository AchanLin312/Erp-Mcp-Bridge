using System;

namespace Epicor.MetaFX.Core.Exceptions;

[Serializable]
public class AppNotFoundException : Exception
{
	public AppNotFoundException()
	{
	}

	public AppNotFoundException(string message)
		: base(message)
	{
	}

	public AppNotFoundException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
