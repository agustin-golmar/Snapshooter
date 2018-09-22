
	using UnityEngine;

		/*
		* Representa una entidad controlada por un usuario. El usuario que la
		* controla se puede encontrar en un sistema remoto o local.
		*/

	public abstract class Player
		: MonoBehaviour, IBindable, IIdentifiable, IReplicable {

		protected Configuration config;

		// Estos 3 no se pueden contener en un único objeto?
		protected Link link;
		protected Demultiplexer input;
		protected Stream output;

		// Es necesario? Se puede evitar?
		protected int id;

		// Debería estar en otro lado...
		protected float lastSnapshot;
		protected float deltaSnapshot;

		protected void Awake() {
			config = GameObject
				.Find("Configuration")
				.GetComponent<Configuration>();
		}

		protected void Start() {
			link = config.GetLink(id);
			input = new Demultiplexer(config.maxPacketsInQueue);
			output = new Stream(config.maxPacketsInQueue);
			lastSnapshot = Time.fixedUnscaledTime;
			deltaSnapshot = 1.0f/10.0f;
		}

		public Demultiplexer GetInputDemultiplexer() {
			return input;
		}

		public Stream GetOutputStream() {
			return output;
		}

		public int GetID() {
			return id;
		}

		public IIdentifiable SetID(int id) {
			this.id = id;
			return this;
		}

		public abstract bool ShouldBind();
		public abstract void Replicate();
	}
