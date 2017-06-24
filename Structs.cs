using System.Threading;

/// <summary>
/// Task item for tasks store.
/// </summary>
public struct Task {
	/// <summary>
	/// Task job to execute.
	/// </summary>
	public object Job;
	/// <summary>
	/// Task background thread priority t run with.
	/// </summary>
	public ThreadPriority Priority;
	/// <summary>
	/// If true, task is asynchronous, false if task is synchronous.
	/// </summary>
	public bool Async;
}