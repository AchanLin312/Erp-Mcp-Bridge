using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Epicor.MetaFX.Core.Helper;

public sealed class LockController
{
	private static readonly ConcurrentDictionary<string, LockContext> _lockStates = new ConcurrentDictionary<string, LockContext>();

	public static string CreateLock(string identifier)
	{
		return AddLockController(identifier);
	}

	public static string CreateLock(string identifier, bool startBlocked)
	{
		return AddLockController(identifier, startBlocked);
	}

	public static string CreateLock(string identifier, bool startBlocked, TimeSpan timeout)
	{
		return AddLockController(identifier, startBlocked, timeout);
	}

	public static void WaitForLock(string identifier, TimeSpan? timeout = null)
	{
		WaitForLock(identifier, timeout, false);
	}

	public static string BlockLock(string identifier)
	{
		if (_lockStates.TryGetValue(identifier, out var value))
		{
			string text = Guid.NewGuid().ToString();
			value.Lock.WaitOne();
			_lockStates[identifier].CurrentTransaction = text;
			return text;
		}
		return string.Empty;
	}

	public static void ReleaseLock(string identifier)
	{
		if (_lockStates.TryGetValue(identifier, out var value))
		{
			try
			{
				value.Lock.Release();
			}
			catch (SemaphoreFullException)
			{
			}
		}
	}

	public static void SafelyReleaseLock(string identifier, string transactionId)
	{
		_lockStates.TryGetValue(identifier, out var value);
		if (value?.CurrentTransaction == transactionId)
		{
			ReleaseLock(identifier);
		}
	}

	private static void WaitForLock(string identifier, TimeSpan? timeout = null, bool keepBlocked = false)
	{
		if (!_lockStates.TryGetValue(identifier, out var value))
		{
			return;
		}
		bool flag = value.Lock.WaitOne(timeout ?? TimeSpan.FromMilliseconds(-1.0));
		if (!keepBlocked)
		{
			ReleaseLock(identifier);
			if (!flag)
			{
				value.CurrentTransaction = string.Empty;
			}
		}
	}

	private static string AddLockController(string identifier, bool startBlocked = false, TimeSpan? timeout = null)
	{
		string text = Guid.NewGuid().ToString();
		if (!_lockStates.TryGetValue(identifier, out var _))
		{
			Semaphore semaphore = new Semaphore((!startBlocked) ? 1 : 0, 1);
			_lockStates.TryAdd(identifier, new LockContext
			{
				Lock = semaphore,
				CurrentTransaction = text
			});
			return text;
		}
		WaitForLock(identifier, timeout, startBlocked);
		_lockStates[identifier].CurrentTransaction = text;
		return text;
	}
}
