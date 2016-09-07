using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;

public class SocketServer : MonoBehaviour
{
	private enum State 
	{
		None,
		StartServer,
		WaitClient,
		ConnectToHost,
		Connecting,
	}

	private const int _PORT = 50000;
	private IPAddress _hostAddress;
	private TCPNetwork _tcpNetwork;

	// ステータスを表示するテキスト
	private Text _statusText;

	// 現在ステータス
	private State _state = State.None;

	// 通知されたイベント
	private NetEvent _netEvent = NetEvent.None;

	private string _value;

	public GameObject target;

	void Start ()
	{
		_statusText = GameObject.Find ("StatusText").GetComponent<Text> ();

		IPHostEntry entry = Dns.GetHostEntry (Dns.GetHostName ());
		Debug.Log (entry.HostName);
		_hostAddress = entry.AddressList [0];
		_hostAddress = IPAddress.Parse ("127.0.0.1");

		GameObject networkObj = new GameObject ("Network");
		_tcpNetwork = networkObj.AddComponent<TCPNetwork> ();
		_tcpNetwork.RegHandler (NetworkEventHandler);
	}

	void Update() {
		if (_value != null && !_value.Equals ("")) {
			Debug.Log (_value);
			target.transform.eulerAngles = new Vector3 (0, float.Parse (_value), 0);
		}
		switch (_state) {
		case State.None:
			break;
		case State.StartServer: 
			UpdateStartServer (); 

			break;
		case State.WaitClient: 
			UpdateWaitClient(); 
			break;
		case State.ConnectToHost:
			UpdateConnectToHost ();
			break;
		case State.Connecting:
			UpdateConnecting ();
			break;
		default:
			throw new ArgumentOutOfRangeException (_state.ToString ());
		}
	}

	// 初回の処理
	private void Step(State state) {
		_state = state;

		switch (_state) {
		case State.None:
			break;
		case State.StartServer: 
			Debug.Log ("StepStartServer");
			StepStartServer(); 
			break;
		case State.WaitClient: 
			StepWaitClient(); 
			break;
		case State.ConnectToHost:
			StepConnectToHost ();
			break;
		case State.Connecting:
			StepConnecting ();
			break;
		default:
			throw new ArgumentOutOfRangeException (_state.ToString ());
		}
	}

	private void StepStartServer() {
		_tcpNetwork.StartServer(_PORT, 1);
	}

	private void UpdateStartServer() {
		if (_tcpNetwork.IsLoop) {
			Step(State.WaitClient);
		}
	}

	private void StepWaitClient() {

	}

	private void UpdateWaitClient() {
		if (_netEvent == NetEvent.Connect) {
			Step (State.Connecting);
		}
	}

	private void StepConnectToHost() {
		if (!_tcpNetwork.IsLoop) {
			_tcpNetwork.Connect (_hostAddress, _PORT);
		} else {
			// NetworkEventHandler (NetEvent.Error);
		}
	}

	private void UpdateConnectToHost() {
		if (_tcpNetwork.IsLoop) {
			Step (State.Connecting);
		}
	}

	private void StepConnecting() {
		if (_tcpNetwork.IsLoop) {
			_statusText.text = "接続中";
		}
	}

	private void UpdateConnecting() {

	}

	// StartServerボタン
	public void ClickStartServer() {
		Debug.Log ("StartServer");
		Step(State.StartServer);
	}

	// ConnectToHostボタン
	public void ClickConnectToHost() {
		Step(State.ConnectToHost);
	}

	private void NetworkEventHandler(NetEvent netevent, string value) {
		_netEvent = netevent;
		_value = value;
	}
}
