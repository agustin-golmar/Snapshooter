
using System.Collections.Generic;
using System.Threading;

	/*
	* Provee una capa de multi-threading para despachar subtareas.
	*
	* @see https://msdn.microsoft.com/en-us/library/system.threading.thread(v=vs.110).aspx
	*/

public class Threading {

	protected readonly List<Thread> threads;

	public Threading() {
		threads = new List<Thread>();
	}

	public void Shutdown() {
		foreach (Thread thread in threads) {
			thread.Abort();
		}
		threads.Clear();
	}

	public Threading Submit(ThreadStart runnable) {
		Thread thread = new Thread(runnable) {
			IsBackground = true
		};
		threads.Add(thread);
		thread.Start();
		return this;
	}
}
