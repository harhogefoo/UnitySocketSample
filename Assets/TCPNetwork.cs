using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;


public enum NetEvent
{
	None,
	StartServer,
	Connect,
	Disconnect,
	Error
}

public class TCPNetwork : MonoBehaviour {
	
	private const int WaitTime = 5;

	TcpListener listener;
	private Socket _listener;
	private Socket _socket;

	public bool IsLoop { get; set; }
	private Thread _dispatchThread;

	private Action<NetEvent> _handler;

	/// <summary>
	/// サーバーとして開始
	/// </summary>
	/// <param name="port"></param>
	/// <param name="connectionNum"></param>
	/// <returns></returns>
	public bool StartServer(int port, int connectionNum) {
		try {
			IPAddress ipStr = IPAddress.Parse("127.0.0.1");
			listener = new TcpListener(ipStr, port);
			listener.Start();
			// _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// _listener.Bind(new IPEndPoint(IPAddress.Any, port));
			// _listener.Listen(connectionNum);
		} catch {
			return false;
		}

		_handler(NetEvent.StartServer);

		return LaunchThread();
	}

	public bool Connect(IPAddress ipaddress, int port) 
	{
		// 既にサーバとして起動
		if (_listener != null) {
			return false;
		}

		bool result = false;
		try {
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.NoDelay = true;
			_socket.Connect(ipaddress, port);
			result = LaunchThread();

		} catch (SocketException) {
			_socket = null;
		}

		if (_handler != null) {
			_handler (result ? NetEvent.Connect : NetEvent.Error);
		}
		return result;
	}

	private bool LaunchThread() {
		try {
			IsLoop = true;
			_dispatchThread = new Thread(new ThreadStart(Dispatch));
			_dispatchThread.Start();
		} catch {
			return false;
		}
		return true;
	}

    void OnDisable()
    {
        _dispatchThread.Abort();
        _dispatchThread = null;
    }

	private void Dispatch() {
		while (IsLoop) {
			Accept();

			Thread.Sleep(WaitTime);
		}
	}

	private void Accept() {
		if (listener != null && listener.Server.Poll (0, SelectMode.SelectRead)) {
			TcpClient client = listener.AcceptTcpClient ();
			NetworkStream ns = client.GetStream ();

			Encoding enc = Encoding.UTF8;
			MemoryStream ms = new MemoryStream ();
			byte[] resBytes = new byte[256];
			int resSize = 0;
			resSize = ns.Read (resBytes, 0, resBytes.Length);
			ms.Write (resBytes, 0, resSize);
			string resMsg = enc.GetString (ms.GetBuffer (), 0, (int)ms.Length);
			ms.Close ();
			Debug.Log (resMsg);

			// クライアント接続を通知
			_handler (NetEvent.Connect);

			string sendMsg = resMsg.Length.ToString ();
			byte[] sendBytes = enc.GetBytes (sendMsg + "\n");
			ns.Write (sendBytes, 0, sendBytes.Length);
			ns.Close();
			client.Close ();
		}
	}

	public void RegHandler(Action<NetEvent> act) {
		_handler = act;
	}

	public void ClearHandler() {
		_handler = null;
	}
}