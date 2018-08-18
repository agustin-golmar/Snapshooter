
	using UnityEngine;

		/*
		* Representa una entidad controlada por un usuario. El usuario que la
		* controla se puede encontrar en un sistema remoto o local.
		*/

	public abstract class Player
		: MonoBehaviour, IBindable, IIdentifiable, IReplicable {

		protected Configuration config;
		protected Transport link;
		protected Stream stream;
		protected int id;

		void Awake() {
			config = GameObject
				.Find("Configuration")
				.GetComponent<Configuration>();
		}

		void Start() {
			link = config.GetLink(id);
			stream = new Stream(config.maxPacketsInQueue);
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
