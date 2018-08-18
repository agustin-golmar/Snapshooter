
	using System.Net;
	using System.Net.Sockets;

		/*
		* Se encarga de proporcionar una capa de transporte sobre UDP. Ofrece
		* mecanismos para enviar y recibir paquetes, así como también para
		* realizar multi-casting.
		*
		* @see https://msdn.microsoft.com/en-us/library/system.net.sockets.udpclient(v=vs.110).aspx
		*/

	public class Transport {

		protected readonly object receiveLock = new object();
		protected readonly object sendLock = new object();
		protected IPEndPoint endpoint;
		protected UdpClient receiveSocket;
		protected UdpClient sendSocket;

		public Transport(Builder builder) {
			endpoint = new IPEndPoint(IPAddress.Parse(builder.ip), builder.port);
			if (builder.bind) {
				receiveSocket = ConcurrentSocket(endpoint);
				receiveSocket.Client.ReceiveTimeout = builder.receiveTimeout;
				sendSocket = ConcurrentSocket(endpoint);
				sendSocket.Client.SendTimeout = builder.sendTimeout;
			}
		}

		protected UdpClient ConcurrentSocket(IPEndPoint endpoint) {
			UdpClient socket = new UdpClient() {
				ExclusiveAddressUse = false
			};
			socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.Client.Bind(endpoint);
			return socket;
		}

		public void Close() {
			if (receiveSocket != null) {
				lock (receiveLock) {
					receiveSocket.Close();
				}
			}
			if (sendSocket != null) {
				lock (sendLock) {
					sendSocket.Close();
				}
			}
		}

		public Transport Multicast(Transport [] remotes, int baseIndex, Stream stream) {
			Packet packet = stream.Read();
			if (packet != null) {
				for (int i = baseIndex; i < remotes.Length; ++i) {
					Send(remotes[i], packet);
				}
			}
			return this;
		}

		public byte [] Receive(Transport remote) {
			lock (receiveLock) {
				try {
					byte [] payload = receiveSocket.Receive(ref remote.endpoint);
					return payload;
				}
				catch (SocketException) {
					return null;
				}
			}
		}

		public int Send(Transport remote, Packet packet) {
			if (packet != null) {
				byte [] payload = packet.GetPayload();
				lock (sendLock) {
					try {
						return sendSocket.Send(payload, payload.Length, remote.endpoint);
					}
					catch (SocketException) {}
				}
			}
			return 0;
		}

		public int Send(Transport remote, Stream stream) {
			return Send(remote, stream.Read());
		}

		public class Builder {

			public string ip;
			public int port;
			public bool bind;
			public int receiveTimeout;
			public int sendTimeout;

			public Builder() {
				ip = "127.0.0.1";
				port = 10000;
				bind = true;
				receiveTimeout = 2000;
				sendTimeout = 4000;
			}

			public Builder IP(string ip) {
				this.ip = ip;
				return this;
			}

			public Builder Port(int port) {
				this.port = port;
				return this;
			}

			public Builder Bind(bool bind) {
				this.bind = bind;
				return this;
			}

			public Builder ReceiveTimeout(int receiveTimeout) {
				this.receiveTimeout = receiveTimeout;
				return this;
			}

			public Builder SendTimeout(int sendTimeout) {
				this.sendTimeout = sendTimeout;
				return this;
			}

			public Transport Build() {
				return new Transport(this);
			}
		}
	}
