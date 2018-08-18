
	using System.Threading;
	using UnityEngine;

		/*
		* Representa un jugador local.
		*/

	public class LocalPlayer : Player {

		// Comandos de movimiento:
		private const string KEY_W = "w";
		private const string KEY_A = "a";
		private const string KEY_S = "s";
		private const string KEY_D = "d";

		// Velocidad, en [m/s]:
		public float speed;

		void Update() {
			bool moved = true;
			float delta = speed * Time.deltaTime;
			Vector3 deltaPosition = Vector3.zero;
			switch (Input.inputString) {
				case KEY_W : {
					deltaPosition = new Vector3(0, 0, delta);
					break;
				}
				case KEY_A : {
					deltaPosition = new Vector3(-delta, 0, 0);
					break;
				}
				case KEY_S : {
					deltaPosition = new Vector3(0, 0, -delta);
					break;
				}
				case KEY_D : {
					deltaPosition = new Vector3(delta, 0, 0);
					break;
				}
				default : {
					moved = false;
					break;
				}
			}
			if (moved) {
				transform.Translate(deltaPosition);
				Packet packet = new Packet.Builder(config.maxPacketSize)
					.AddString("30 - Testing a fucking packet.")
					.AddVector(deltaPosition)
					//.AddBitBuffer(...)
					//.AddInt(...)
					//.WhatEverYouLike(...)
					.Build();
				stream.Write(packet);
				Debug.Log("El jugador local se movió.");
			}
		}

		public override bool ShouldBind() {
			return true;
		}

		public override void Replicate() {
			// UDP puede ser lento debido a ARP. Non-blocking?
			while (!config.OnStart()) {
				Thread.Sleep(100);
			}
			while (!config.OnEscape()) {
				link.Multicast(config.GetLinks(), 1, stream);
				Thread.Sleep(config.replicationLag);
			}
		}
	}
