using System.Threading;

namespace Epicor.MetaFX.Core.Helper;

internal class LockContext
{
	public Semaphore Lock;

	public string CurrentTransaction;
}
