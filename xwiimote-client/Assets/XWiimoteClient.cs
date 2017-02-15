using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using WebSocketSharp;

public class XWiimoteClient {
	private static UdpClient udpClient = null;
	private static WebSocket        ws = null;
	private static IPEndPoint endPoint = null;

	private static List<XWiimoteEvents> wiimotes = new List<XWiimoteEvents>();

	public delegate void WiimoteConnectHandler(XWiimoteEvents wiimote);
	public static event WiimoteConnectHandler wiimoteConnect;

	private enum event_types {
		XWII_EVENT_KEY,
		XWII_EVENT_ACCEL,
		XWII_EVENT_IR,
		XWII_EVENT_BALANCE_BOARD,
		XWII_EVENT_MOTION_PLUS,
		XWII_EVENT_PRO_CONTROLLER_KEY,
		XWII_EVENT_PRO_CONTROLLER_MOVE,
		XWII_EVENT_WATCH,
		XWII_EVENT_CLASSIC_CONTROLLER_KEY,
		XWII_EVENT_CLASSIC_CONTROLLER_MOVE,
		XWII_EVENT_NUNCHUK_KEY,
		XWII_EVENT_NUNCHUK_MOVE,
		XWII_EVENT_DRUMS_KEY,
		XWII_EVENT_DRUMS_MOVE,
		XWII_EVENT_GUITAR_KEY,
		XWII_EVENT_GUITAR_MOVE,
		XWII_EVENT_GONE,
		XWII_EVENT_NUM
	};
		
	private static void DecodeEvent(byte[] bytes){
		bool wiimote_monitor = BitConverter.ToBoolean (bytes, 0);
		Int32     wiimote_id = BitConverter.ToInt32 (bytes, 1);

		if (wiimote_monitor) {
			XWiimoteEvents wm = new XWiimoteEvents(wiimote_id);
			wiimotes.Add(wm);

			// Call the event
			wiimoteConnect(wm);
		} else {
			event_types type = (event_types)BitConverter.ToUInt32 (bytes, 5);
			Int64        sec =              BitConverter.ToInt64 (bytes, 9);
			Int64       usec =              BitConverter.ToInt64 (bytes, 17);

			XWiimoteEvents wm = wiimotes.Find (_wm => _wm.ID == wiimote_id);

			switch (type) {
			case event_types.XWII_EVENT_ACCEL:
				XWiimoteEvents.AccelData accelData;

				accelData.x = BitConverter.ToInt32 (bytes, 25);
				accelData.y = BitConverter.ToInt32 (bytes, 29);
				accelData.z = BitConverter.ToInt32 (bytes, 33);

				wm.raiseAccelChange (accelData);
				break;
			case event_types.XWII_EVENT_IR:
				XWiimoteEvents.IRPoint[] irData = new XWiimoteEvents.IRPoint[4];

				for (int i = 0; i < 4; i++) {
					irData [i].x = BitConverter.ToInt32 (bytes, 25 + i * 2 * 4);
					irData [i].y = BitConverter.ToInt32 (bytes, 29 + i * 2 * 4);
				}

				wm.raiseIRChange (irData);
				break;
			case event_types.XWII_EVENT_MOTION_PLUS:
				// NOT YET IMPLMENTED
				break;
			case event_types.XWII_EVENT_KEY:
				XWiimoteEvents.KeyCode key_code = (XWiimoteEvents.KeyCode)BitConverter.ToUInt32 (bytes, 25);
				UInt32 key_state = BitConverter.ToUInt32 (bytes, 29);

				wm.raiseKeyChange (key_code, key_state);
				break;
			case event_types.XWII_EVENT_GONE:
				wm.raiseDisconnect ();
				wiimotes.Remove (wm);
				break;
			default:
				// UNRECOGNIZED EVENT
				break;
			}

		}
	}

	private static void RecieveUDP(IAsyncResult ar) {
		byte[] bytes = udpClient.EndReceive (ar, ref endPoint);
		DecodeEvent (bytes);
		udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);
	}

	private static void RecieveWS(object sender, MessageEventArgs e) {
		if (e.IsBinary) {
			byte[] bytes = e.RawData;
			DecodeEvent (bytes);
		} else {
			Debug.Log (e.Data);
		}
	}

	public static void setRumble(Int32 wiimote_id, bool state){
		List<byte> bytes = new List<byte> ();

		bytes.AddRange (BitConverter.GetBytes ((Int32)1));
		bytes.AddRange (BitConverter.GetBytes (wiimote_id));
		bytes.AddRange (BitConverter.GetBytes ((Int32)(state?1:0)));

		ws.Send(bytes.ToArray());
	}

	// Setup TCP and UDP connections
	public static void start(string ip, int port){
		ws = new WebSocket ("ws://" + ip + ":" + port);

		ws.OnMessage += RecieveWS;

		ws.OnOpen += (sender, e) => {
			Debug.Log("Websocket connected");

			// Limit IPAddress.Any to the ip?
			endPoint = new IPEndPoint (IPAddress.Any, 9001);
			udpClient = new UdpClient (endPoint);

			udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);
		};

		ws.OnError += (sender, e) => {
			Debug.LogError (e.Message);
		};

		ws.OnClose += (sender, e) => {
			if(e.WasClean){
				Debug.Log("Server closed the connection.");
			}else{
				Debug.Log("Websocket closed with reason: " + e.Reason);
			}

			// Guess I should do this?
			ws = null;

			if(udpClient != null)
				udpClient.Close ();	
			udpClient = null;
		};

		ws.Connect ();
	}

	public static void stop() {
		if( ws != null) ws.Close ();
	}
}

public class XWiimoteEvents {
	public delegate void DisconnectHandler();
	public delegate void KeyChangeHandler(KeyCode keycode, UInt32 state);
	public delegate void IRChangeHandler(IRPoint[] irData);
	public delegate void AccelChangeHandler(AccelData accelData);

	public event DisconnectHandler  disconnect;
	public event KeyChangeHandler   keyChange;
	public event IRChangeHandler    irChange;
	public event AccelChangeHandler accelChange;

	public void raiseDisconnect() {
		if(disconnect != null)
			disconnect ();
	}
		
	public void raiseKeyChange(KeyCode keycode, UInt32 state) {
		if(keyChange != null)
			keyChange (keycode,state);
	}
		
	public void raiseIRChange(IRPoint[] irData) {
		if(irChange != null)
			irChange (irData);
	}

	public void raiseAccelChange(AccelData accelData) {
		if(accelChange != null)
			accelChange (accelData);
	}

	public enum KeyCode {
		XWII_KEY_LEFT,
		XWII_KEY_RIGHT,
		XWII_KEY_UP,
		XWII_KEY_DOWN,
		XWII_KEY_A,
		XWII_KEY_B,
		XWII_KEY_PLUS,
		XWII_KEY_MINUS,
		XWII_KEY_HOME,
		XWII_KEY_ONE,
		XWII_KEY_TWO,
		XWII_KEY_X,
		XWII_KEY_Y,
		XWII_KEY_TL,
		XWII_KEY_TR,
		XWII_KEY_ZL,
		XWII_KEY_ZR,
		XWII_KEY_THUMBL,
		XWII_KEY_THUMBR,
		XWII_KEY_NUM
	};

	public struct IRPoint {
		public Int32 x;
		public Int32 y;
	}

	public struct AccelData {
		public Int32 x;
		public Int32 y;
		public Int32 z;
	}

	public readonly int ID;

	public XWiimoteEvents(int ID){
		this.ID = ID;
	}
}

