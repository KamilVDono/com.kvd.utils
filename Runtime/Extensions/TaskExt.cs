using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace KVD.Utils.Extensions
{
	public static class TaskExt
	{
		public static IEnumerator AsIEnumerator(this Task task)
		{
			while (!task.IsCompleted)
			{
				yield return null;
			}

			if (task.IsFaulted)
			{
				ExceptionDispatchInfo.Capture(task.Exception!).Throw();
			}

			yield return null;
		}

		public static IEnumerator AsIEnumerator<T>(this Task<T> task)
		{
			while (!task.IsCompleted)
			{
				yield return null;
			}

			if (task.IsFaulted)
			{
				ExceptionDispatchInfo.Capture(task.Exception!).Throw();
			}

			yield return null;
		}
	}
}
