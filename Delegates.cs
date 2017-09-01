namespace Pooler {
	/// <summary>
	/// Event called after all tasks in store are done. First param is threads pool instance, second param is event arguments with possible synchronous tasks exceptions.
	/// </summary>
	/// <param name="pool">Threads instance pool instance.</param>
	/// <param name="poolAllDoneEventArgs">Event arguments object with Exceptions field as List&lt;Exception&gt; of synchronously executed threads catched exceptions.</param>
	public delegate void AllDoneHandler (Base pool, AllDoneEventArgs poolAllDoneEventArgs);
	/// <summary>
	/// Event called after each tasks in store is done. First param is threads pool instance, second param is event arguments with possible task result and currently and simultaneously running tasks count.
	/// </summary>
	/// <param name="pool">Threads instance pool instance.</param>
	/// <param name="poolTaskDoneEventArgs">Event arguments object with possible task result and currently and simultaneously running tasks count.</param>
	public delegate void TaskDoneHandler (Base pool, TaskDoneEventArgs poolTaskDoneEventArgs);
	/// <summary>
	/// Event called from background thread when there is catched any exception in synchronously executed task.
	/// </summary>
	/// <param name="pool">Threads instance pool instance.</param>
	/// <param name="poolTaskExceptionEventArgs">Event arguments object with Exception field as Exception of synchronously executed thread catched exception.</param>
	public delegate void TaskExceptionHandler (Base pool, ExceptionEventArgs poolTaskExceptionEventArgs);
    /// <summary>
	/// Any delegate added internaly into tasks store in Add() method with no params section or with single param accepting Pooler.Base type.
	/// </summary>
	/// <param name="pool">threads instance pool instance.</param>
	public delegate void ParallelTaskDelegate (Parallel pool);
    /// <summary>
	/// Any delegate added internaly into tasks store in Add() method with no params section or with single param accepting Pooler.Base type.
	/// </summary>
	/// <param name="pool">threads instance pool instance.</param>
	public delegate void RepeaterTaskDelegate (Repeater pool);
}
