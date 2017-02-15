using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using WebSocketSharp;

//Subscribe to wiimote connect events
//Subscribe to specific wiimotes event_types
public class XWiimoteManager {
	private static UdpClient udpClient = null;
	private static WebSocket ws = null;
	private static IPEndPoint endPoint = null;

	private static List<Wiimote> wiimotes = new List<Wiimote>();

	public delegate void WiimoteConnectHandler(Wiimote wiimote);
	public static event WiimoteConnectHandler wiimoteConnect;

	enum event_types {
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

	//public delegate void W();

	// Potentially make this return some sort of object like the events in python?
	private static void DecodeEvent(byte[] bytes){
		bool wiimote_monitor = BitConverter.ToBoolean (bytes, 0);
		Int32     wiimote_id = BitConverter.ToInt32 (bytes, 1);

		if (wiimote_monitor) {
			Wiimote wm = new Wiimote(wiimote_id);
			wiimotes.Add(wm);

			// Call the event
			wiimoteConnect(wm);
		} else {
			event_types type = (event_types)BitConverter.ToUInt32 (bytes, 5);
			Int64        sec =              BitConverter.ToInt64 (bytes, 9);
			Int64       usec =              BitConverter.ToInt64 (bytes, 17);

			Wiimote       wm = wiimotes.Find (_wm => _wm.ID == wiimote_id);

			switch (type) {
			case event_types.XWII_EVENT_ACCEL:
				break;
			case event_types.XWII_EVENT_IR:
				for (int i = 0; i < 4; i++) {
					Wiimote.IRData.IRPoint p;
					p.x = BitConverter.ToInt32 (bytes, 25+i*2*4);
					p.y = BitConverter.ToInt32 (bytes, 29+i*2*4);
					wm.irData [i] = p;
				}

				break;
			case event_types.XWII_EVENT_MOTION_PLUS:
				break;
			case event_types.XWII_EVENT_KEY:
				Wiimote.Keys.code key_code 	= (Wiimote.Keys.code)BitConverter.ToUInt32 (bytes, 25);
				UInt32 key_state 			= BitConverter.ToUInt32 (bytes, 29);

				// This is not the case, they can also return 3 for repeat
				wm.keys[key_code] = key_state == 1;
				break;
			case event_types.XWII_EVENT_GONE:
				break;
			default:
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

	public static void setRumble(Wiimote wm, bool state){
		List<byte> bytes = new List<byte> ();

		bytes.AddRange (BitConverter.GetBytes ((Int32)1));
		bytes.AddRange (BitConverter.GetBytes ((Int32)wm.ID));
		bytes.AddRange (BitConverter.GetBytes ((Int32)(state?1:0)));

		ws.Send(bytes.ToArray());
	}

	// Setup TCP and UDP connections
	public static void start(string ip, int port){
		ws = new WebSocket ("ws://"+ip+":"+port);

		ws.OnMessage += RecieveWS;
		
		ws.OnOpen += (sender, e) => {
		};

		ws.Connect ();

		endPoint = new IPEndPoint (IPAddress.Any, 9001);
		udpClient = new UdpClient (endPoint);

		udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);
	}

	// Close TCP and stop recieving UDP connections
	public static void stop() {
		udpClient.Close ();	
	}
}

public class Wiimote {
	// Potentially deal with ondisconnect here, which gets called from manager (i suppose like the others
	// And have a disconnected thing.
	public delegate void DisconnectHandler();

	public class Keys 
	{
		public delegate void ChangeHandler(Keys.code keycode, bool state);
		public event ChangeHandler change;

		public enum code {
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

		private bool[] keys = new bool[(int)Keys.code.XWII_KEY_NUM];

		public bool this[Keys.code index]{
			get
			{
				return keys[(int)index];
			}

			set
			{
				keys[(int)index] = value;
				change(index, keys[(int)index]);
			}
		}
	}

	public class IRData 
	{
		public delegate void ChangeHandler(IRData irData);
		public event ChangeHandler change;

		public struct IRPoint {
			public Int32 x;
			public Int32 y;
		}
			
		private IRPoint[] IR = new IRPoint[4];

		// assumes we dont modify the x or y individually...
		// if we do, need to call change() afterwards
		public IRPoint this[int index]{
			get 
			{
				if (index < 0 || index > 3)
					throw new ArgumentOutOfRangeException ();
				
				return IR[index];
			}

			set 
			{
				if (index < 0 || index > 3)
					throw new ArgumentOutOfRangeException ();
				
				IR [index] = value;
				change(this);
			}
		}
	}

	public class LEDArray
	{
		public bool this[int index]
		{
			get 
			{
				if (index < 0 || index > 3)
					throw new ArgumentOutOfRangeException ();

				return false;
			}

			set 
			{
				if (index < 0 || index > 3)
					throw new ArgumentOutOfRangeException ();
			}
		}
	}
		
	public Keys keys = new Keys ();
	public IRData irData = new IRData ();

	public readonly int ID;

	public bool rumble 
	{
		get
		{
			return _rumble;
		}

		set 
		{
			// Send rumble signal
			// Could possibly do this a ncier way, oh well
			XWiimoteManager.setRumble(this, value);
			
			_rumble = value;
		}
	}

	private bool _rumble = false;

	public Wiimote(int ID){
		this.ID = ID;
	}
}

