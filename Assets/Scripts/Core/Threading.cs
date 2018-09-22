
	using System.Threading;

		/*
		* Provee una capa de multi-threading para despachar subtareas.
		*
		* @see https://msdn.microsoft.com/en-us/library/system.threading.thread(v=vs.110).aspx
		*/

	public class Threading {

		protected readonly Thread [] threads;

		public Threading(int poolSize) {
			threads = new Thread [poolSize];
		}

		public void Shutdown() {
			foreach (Thread thread in threads) {
				thread.Abort();
			}
		}

		public Threading Submit(int index, ThreadStart runnable) {
			Thread thread = new Thread(runnable) {
				IsBackground = true
			};
			threads[index] = thread;
			thread.Start();
			return this;
		}
	}
