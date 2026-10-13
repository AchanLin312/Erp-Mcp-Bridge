using System;

namespace Epicor.MetaFX.Core.Exceptions;

[Serializable]
public class MetaFxException : Exception
{
	private MetaFxExceptionType _exceptionType;

	private string _errorCode;

	public MetaFxExceptionType ExceptionType => _exceptionType;

	public string ErrorCode => _errorCode;

	public MetaFxException()
	{
	}

	public MetaFxException(MetaFxExceptionType exceptionType, string message)
		: base(message)
	{
		_exceptionType = exceptionType;
	}

	public MetaFxException(MetaFxExceptionType exceptionType, string message, Exception inner)
		: base(message, inner)
	{
		_exceptionType = exceptionType;
	}

	public MetaFxException(MetaFxExceptionType exceptionType, string errorCode, string message, Exception inner)
		: base(message, inner)
	{
		_exceptionType = exceptionType;
		_errorCode = errorCode;
	}
}
