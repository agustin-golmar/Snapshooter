using UnityEngine;

public class Player : MonoBehaviour {

	protected void Start() {
	}

	protected void Update() {
	}

	protected void LateUpdate() {
		Camera.main.transform.LookAt(transform);
	}
}
