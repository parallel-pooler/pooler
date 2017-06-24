using System;
using System.Collections.Generic;

namespace Pooler {
	/// <summary>
	/// Event arguments object with Exceptions field as List&lt;Exception&gt; of synchronously executed threads catched exceptions.
	/// </summary>
	public class AllDoneEventArgs: EventArgs {
		public List<Exception> Exceptions = new List<Exception>();
		public int PeakThreadsCount = 0;
		public int ExecutedTasksCount = 0;
	}
	/// <summary>
	/// Event arguments object with possible task result and currently and simultaneously running threads count in threads pool.
	/// </summary>
	public class TaskDoneEventArgs: EventArgs {
		public object TaskResult = null;
		public int RunningTasksCount = 0;
		public int ExecutedTasksCount = 0;
	}
	/// <summary>
	/// Event arguments object with Exception field as Exception of synchronously executed thread catched exception.
	/// </summary>
	public class ExceptionEventArgs: EventArgs {
		public Exception Exception = null;
	}
}