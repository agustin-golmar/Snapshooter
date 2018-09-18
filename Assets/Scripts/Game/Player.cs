
	using UnityEngine;

		/*
		* Representa una entidad controlada por un usuario. El usuario que la
		* controla se puede encontrar en un sistema remoto o local.
		*/

	public abstract class Player
		: MonoBehaviour, IBindable, IIdentifiable, IReplicable {

		protected Configuration config;
		protected Link link;
		protected Stream input;
		protected Stream output;
		protected int id;
		protected float lastSnapshot;
		protected float deltaSnapshot;

		protected void Awake() {
			config = GameObject
				.Find("Configuration")
				.GetComponent<Configuration>();
		}

		protected void Start() {
			link = config.GetLink(id);
			input = new Stream(config.maxPacketsInQueue);
			output = new Stream(config.maxPacketsInQueue);
			lastSnapshot = Time.fixedUnscaledTime;
			deltaSnapshot = 1.0f/10.0f;
		}

		public Stream GetInputStream() {
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
