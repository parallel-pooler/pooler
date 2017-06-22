# **[Parallel Pooler](https://www.nuget.org/packages/Pooler)**

[![Latest Stable Version](https://img.shields.io/badge/Stable-v1.1.0-brightgreen.svg?style=plastic)](https://github.com/tomFlidr/desharp/releases)
[![License](https://img.shields.io/badge/Licence-BSD-brightgreen.svg?style=plastic)](https://raw.githubusercontent.com/debug-sharp/desharp/master/LICENCE.md)
![.NET Version](https://img.shields.io/badge/.NET->=4.0-brightgreen.svg?style=plastic)

.NET parallel tasks executing library.

## Instalation
```nuget
PM> Install-Package Pooler
```

## Examples
- Basics - create new pool and run tasks
- Add delegate tasks or functions returning results
- Use Pooler events
- Changing threads count at run
- Regulating CPU and resources load
- Ways to creating parallel pooler instance
- Stop processing

### Basics - create new pool and run tasks
```cs
using Parallel;
...

// create new threads pool instance 
// for max 10 threads running simultaneously:
Pooler pool = Pooler.CreateNew(10);

// add 500 anonymous functions to process:
for (int i = 0; i < 500; i++) {
	pool.Add(
		// any delegate or void to process
		(Pooler pool) => {
			double dummyResult = Math.Pow(Math.PI, Math.PI);
		},
		// optional - do not run task instantly after adding,
		// run them a few lines later together
		false,
		// optional - background execution thread priority 
		// to execute this task
		ThreadPriority.Lowest, 
		// optional - task is not async (task doesn't use 
		// any background threads inside)
		false
	);
}

// let's process all tasks in 10 simultaneously 
// running threads in background:
pool.StartProcessing();
```

### Add delegate tasks or functions returning results
```cs
// There is possible to add task as delegate:
pool.Add(delegate {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});
// Or there is possible to add task as any void 
// function accepting first param as Pooler type:
pool.Add((Pooler p) => {
	double dummyResult = Math.Pow(Math.PI, Math.PI);
});

// Or there is possible to add task as any 
// Func<Pooler, object> function accepting 
// first param as Pooler type and returning any result:
pool.Add((Pooler p) => {
	return Math.Pow(Math.PI, Math.PI);
});
// You can pick up returned result in TaskDone event:
pool.TaskDone += (Pooler p, PoolerTaskDoneEventArgs e)) => {
	double dummyResult = (double)e.TaskResult;
};
```

### Use Pooler events
```cs
// triggered after each task has been executed (successfuly or with exception):
pool.TaskDone += (Pooler p, PoolerTaskDoneEventArgs e)) => {
	Console.WriteLine("Single task has been executed.");
	
	// e.TaskResult [object] - any place for your task result data:
	Console.WriteLine("Task returned result: " + e.TaskResult);
	
	// e.RunningThreadsCount [Int32]
    Console.WriteLine("Currently running threads count: " + e.RunningThreadsCount);
};

// triggered immediately when exception inside executing task is catched, before TaskDone event:
pool.ThreadException += (Pooler p, PoolerExceptionEventArgs e) => {
	Console.WriteLine("Catched exception during task execution.");
	
	// e.Exception [Exception]:
	Console.WriteLine(e.Exception.Message);
};

// triggered after all tasks in pooler store has been executed:
pool.AllDone += (Pooler p, PoolerAllDoneEventArgs e) => {
	Console.WriteLine("All tasks has been executed.");	
	
	// e.Exceptions [List<Exception>]:
	Console.WriteLine("Catched exceptions count: " + e.Exceptions.Count);
	
	// e.PeakThreadsCount [Int32]:
	Console.WriteLine("Max. running threads peak: " + e.PeakThreadsCount);
	
	// e.ExecutedTasksCount [Int32]:
	Console.WriteLine("Successfully executed tasks count: " + e.ExecutedTasksCount);
	
	// e.NotExecutedTasksCount [Int32]:
	// Not executed (aborted) tasks count by possible pool.StopProcessing(); call:
	Console.WriteLine("Not executed (aborted) tasks count: " + e.NotExecutedTasksCount);
};
```

### Changing running threads count in run
```cs
// There is possible to change processing background threads count
// at game play any time you want, Pooler is using locks internaly
// to manage that properly:
pool.SetMaxRunningTasks(
	// new threads maximum to process all tasks in background
	50,
	// optional . true by default - to create and run all new background
	// threads imediatelly inside this function call.
	// If false, each new background thread to create to fill
	// new maximum will be created after any running thread process
	// execute it's current task, so there should not to be that increasing heap..
	true
);

// There is also possible to get currently running maximum.
// Currently running maximum is not the same as currently 
// running threads count.
int maxThreads = pool.GetMaxRunningTasks();
```

### Regulating CPU and resources load
```cs
// For .NET code, there is CPU load for your pool background 
// threads managed by operating system resources manager, so 
// the only option how to do it from .NET code is to sleep sometime.
// To manage sleeping and sleeping time in your tasks globaly, 
// you can use this methods:

// 1. Set up pausing time globaly for all tasks,
// any time you want, before processing or any time 
// at run to cut CPU or other resources load:
pool.SetPauseMiliseconds(100);

// And you can read it any time of course:
int pausemiliseconds = pool.GetPauseMiliseconds();

// 2. Use pool.Pause() method sometimes in your hard task:
pool.Add((Pooler p) => {
	double someHardCode1 = Math.Pow(Math.PI, Math.PI);
	p.Pause();
	double someHardCode2 = Math.Pow(Math.PI, Math.PI);
	p.Pause();
	double someHardCode3 = Math.Pow(Math.PI, Math.PI);
});

// now resources should not to be so bussy as before,
// try to put there harder code to process, increase pause time
// or try to use test at:
// https://github.com/parallel-pooler/winforms-application-test
```

### Ways to creating parallel pooler instance
```cs
Pooler pool;

// Creating new instance by static factory or by new Pooler

// First optional param is max. threads in background 
// to executing all tasks. 10 by default;

// Second optional param is pause miliseconds to slow down
// CPU load or resources by pool.Pause() calls inside your tasks,
// 0 by default.

pool = Pooler.CreateNew(10, 100);
pool = new Pooler(10, 100);


// There is also possible to use single 
// static instance from Pooler._instance by:
pool = Pooler.GetStaticInstance(10, 100);

// to get the same instance any time again, 
// just call it without params:
pool = Pooler.GetStaticInstance();

```

### Stop processing
```cs
// First optinal param 'abortAllThreadsImmediately' with true
// by default is to hardly abort all background threads by thread.Abort();,
// what should be dangerous for your tasks, so to swtch this to false
// will let all running threads go to their natural task end and than die.
pool.StopProcessing(true);
```
