# **[Pooler](https://www.nuget.org/packages/Pooler)**

[![Latest Stable Version](https://img.shields.io/badge/Stable-v2.1.0-brightgreen.svg?style=plastic)](https://github.com/parallel-pooler/pooler/releases)
[![License](https://img.shields.io/badge/Licence-BSD-brightgreen.svg?style=plastic)](https://raw.githubusercontent.com/parallel-pooler/pooler/master/LICENSE)
![.NET Version](https://img.shields.io/badge/.NET->=4.0-brightgreen.svg?style=plastic)

.NET parallel tasks executing library. `Pooler.Parallel` class to execute different tasks in limited background threads count and `Pooler.Repeater` class to execute single specific task also in limited background threads count with specific repetition rate or in infinite mode.

## Instalation
```nuget
PM> Install-Package Pooler
```

## Examples
1. Basics - create new pool and run tasks
2. Add delegate tasks or functions returning results
3. Use Pooler events
4. Changing threads count at run
5. Regulating CPU and resources load
6. Ways to create pooler instance
7. Stop processing
8. Async tasks

### 1. Basics - create new pool and run tasks
```cs
// create new threads pool instance for max 10 threads running simultaneously:
Pooler.Parallel pool = Pooler.Parallel.CreateNew(10);

// add 500 anonymous functions to process:
for (int i = 0; i < 500; i++) {
	pool.Add(
		// any delegate or void to process
		(Pooler.Base pool) => {
			double dummyResult = Math.Pow(Math.PI, Math.PI);
		},
		// optional - do not run task instantly after adding, run them a few lines later together
		false,
		// optional - background execution thread priority to execute this task
		ThreadPriority.Lowest, 
		// optional - task is not async (task doesn't use any background threads inside)
		false
	);
}
// let's process all tasks in 10 simultaneously running threads in background:
pool.StartProcessing();
```

### 2. Add delegate tasks or functions returning results
```cs
// There is possible to add task as delegate:
pool.Add(delegate {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});
// ... or to add task as any void function accepting first param as Pooler type:
pool.Add((Pooler.Base p) => {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});

// ... or to add task as Func<Pooler.Base, object> function:
// accepting first param as Pooler type and returning any result:
pool.Add((Pooler.Base p) => {
	return Math.Pow(Math.PI, Math.PI);
});
// ... then you can pick up returned result in pool.TaskDone event:
pool.TaskDone += (Pooler.Base p, Pooler.TaskDoneEventArgs e)) => {
	double dummyResult = (double)e.TaskResult;
};
```

### 3. Use Pooler events
`pool.TaskDone` event is triggered after each task has been executed (successfuly or with exception):
```cs
pool.TaskDone += (Pooler.Base p, Pooler.TaskDoneEventArgs e)) => {
	Console.WriteLine("Single task has been executed.");
	// e.TaskResult [object] - any place for your task result data:
	Console.WriteLine("Task returned result: " + e.TaskResult);
	
	// e.RunningTasksCount [Int32]
	Console.WriteLine("Currently running executing background threads count: " + e.RunningTasksCount);
	
	// e.ExecutedTasksCount [Int32]
	Console.WriteLine("Executed tasks count: " + e.ExecutedTasksCount);
};
```
`pool.TaskException` event is triggered immediately when exception inside executing task is catched, before TaskDone event:
```cs
pool.TaskException += (Pooler.Base p, Pooler.ExceptionEventArgs e) => {
	Console.WriteLine("Catched exception during task execution.");
	
	// e.Exception [Exception]:
	Console.WriteLine(e.Exception.Message);
};
```
`pool.AllDone` event is triggered after all tasks in pooler store has been executed:
```cs
pool.AllDone += (Pooler.Base p, Pooler.AllDoneEventArgs e) => {
	Console.WriteLine("All tasks has been executed.");	
	
	// e.Exceptions [List<Exception>]:
	Console.WriteLine("Catched exceptions count: " + e.Exceptions.Count);
	
	// e.PeakThreadsCount [Int32]:
	Console.WriteLine("Max. running threads peak: " + e.PeakThreadsCount);
	
	// e.ExecutedTasksCount [Int32]:
	Console.WriteLine("Successfully executed tasks count: " + e.ExecutedTasksCount);
};
```

### 4. Changing running threads count in run
There is possible to change processing background threads count at game play any time you want, Pooler is using locks internaly to manage that properly:
```cs
pool.SetMaxRunningTasks(
	// new threads maximum to process all tasks in background
	50,
	// optional (true by default), to create and run all new background threads 
	// imediatelly inside this function call. If false, each new background thread 
	// to create to fill new maximum will be created after any running thread process 
	// execute it's current task, so there should not to be that increasing heap..
	true
);
```
There is also possible to get currently running maximum. Currently running maximum is not the same as currently running threads count.
```cs
int maxThreads = pool.GetMaxRunningTasks();
```

### 5. Regulating CPU and resources load
For .NET code, there is CPU load and any other resources load for your threads managed by operating system, so the only option how to manage load from .NET code is to sleep sometime. Then you can have some free system resources for another jobs, for example you need to have another free 20% CPU computation capacity. To manage sleeping and sleeping time in your tasks globaly by pool, you can use this:

1. Set up pausing time globaly for all tasks, any time you want, before processing or any time at run to cut CPU or other resources load:
```cs
pool.SetPauseMiliseconds(100);

// And you can read it any time of course:
int pausemiliseconds = pool.GetPauseMiliseconds();
```

2. Use `pool.Pause();` method sometimes in your hard task:
```cs
pool.Add((Pooler.Base p) => {
	double someHardCode1 = Math.Pow(Math.PI, Math.PI);
	p.Pause();
	double someHardCode2 = Math.Pow(Math.PI, Math.PI);
	p.Pause();
	double someHardCode3 = Math.Pow(Math.PI, Math.PI);
});
```
Now resources should not to be so bussy as before, try to put there harder code to process, increase pause time or try to use [WinForms Test Application](https://github.com/parallel-pooler/winforms-application-test).

### 6. Ways to create pooler instance

Create new parallel tasks instance by static factory or by new Pooler.Parallel:
```cs
Pooler.Parallel pool;
pool = Pooler.Parallel.CreateNew(10, 100);
pool = new Pooler.Parallel(10, 100);
```
First (optional) param is max. threads in background to executing all tasks. 10 by default.
Second (optional) param is pause miliseconds to slow down CPU load or other resources by `pool.Pause();` calls inside your tasks, 0 by default.

Create new repeater tasks instance by static factory or by new Pooler.Repeater to process manytimes only one specific call:
```cs
Pooler.Repeater pool;
pool = Pooler.Repeater.CreateNew(10, 500, 100);
pool = new Pooler.Repeater(10, 500, 100);
```
First (optional) param is max. threads in background to executing one specific task. 10 by default.
Second (optional) param is how many times will be specific task executed.
Third (optional) param is pause miliseconds to slow down CPU load or other resources by `pool.Pause();` calls inside your task, 0 by default.

#### Adding specific task into Repeater pool:
To add only one specific task into Repeater threads pool to execute this single task manytimes in limited background threads count, use:
```cs
// Set one specific task into Repeater as delegate:
pool.Set(delegate {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});
// ... or set task as any void function accepting first param as Pooler type:
pool.Set((Pooler.Base p) => {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});

// ... or set task as Func<Pooler.Base, object> function:
// accepting first param as Pooler type and returning any result:
pool.Set((Pooler.Base p) => {
	return Math.Pow(Math.PI, Math.PI);
});
// ... then you can pick up returned result as before in pool.TaskDone event:
pool.TaskDone += (Pooler.Base p, Pooler.TaskDoneEventArgs e)) => {
	double dummyResult = (double)e.TaskResult;
};
```
```

There is also possible to use single static instance from `Pooler.(Parallel|Repeater)._instance` by:
```cs
Pooler.Parallel pool = Pooler.Parallel.GetStaticInstance(10, 100);
Pooler.Repeater pool = Pooler.Repeater.GetStaticInstance(10, 500, 100);

// to get the same instance any time again, 
// just call it without params:
pool = Pooler.Parallel.GetStaticInstance();
// or:
pool = Pooler.Repeater.GetStaticInstance();

```

### 7. Stop processing
First optinal param (true by default) is to heavy abort - all background threads are aborted by `bgThread.Abort();`, what should be dangerous for your task. So switch this to `false` to let all running background threads go to their natural task end and than abort.
```cs
pool.StopProcessing(true);
```

### 8. Async tasks
To use any other threads or async code in your pool tasks, you need to tell pooler at the end of async task code that you are done by `pool.AsyncTaskDone();` or `pool.AsyncTaskDone(resultObject);`:
```cs
pool.Add(
	// any delegate or void to process
	(Pooler pool) => {
		// some async code start here:
		CustomDownloader client = new CustomDownloader(
			"http://example.com/something/what/takes/some/time/to/load"
		);
		client.Loaded += (object sender, EventArgs e) => {
			// not call pool to continue executing 
			// another tasks by this bg thread:
			pool.AsyncTaskDone(sender);
		};
		client.Load();
	},
	false,
	ThreadPriority.Lowest, 
	// true - task is async!
	true
);
pool.StartProcessing();
```
