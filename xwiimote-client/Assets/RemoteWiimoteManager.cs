using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using WebSocketSharp;


public class RemoteWiimoteManager {
	private static UdpClient udpClient = null;
	private static WebSocket ws = null;
	private static IPEndPoint endPoint = null;
	private static bool connected = false;

	private static List<Wiimote> wiimotes = new List<Wiimote>();

	public delegate void WiimoteConnectHandler(Wiimote wiimote);
	public static event WiimoteConnectHandler wiimoteConnect;

	private static void RecieveUDP(IAsyncResult ar) {
		Byte[] bytes = udpClient.EndReceive (ar, ref endPoint);

//		int type = BitConverter.ToInt32 (bytes, 0);
//		int index = BitConverter.ToInt32 (bytes, 4);
//
//		// 4 bytes in 32 bit int
//		switch (type) {
//		case 0: // Accel
//			if (index >= lastIndexAccel) {
//				// Gonna get converted to floats here
//				accel.x = BitConverter.ToInt32 (bytes, 8);
//				accel.y = BitConverter.ToInt32 (bytes, 12);
//				accel.z = BitConverter.ToInt32 (bytes, 16);
//			}
//			break;
//		case 1: // IR
//			if (index >= lastIndexIR) {
//				int i = BitConverter.ToInt32 (bytes, 8);
//				// Gonna get converted to floats here
//				IR[i].x = (float)BitConverter.ToInt32 (bytes, 12)/100.0f;
//				IR[i].y = (float)BitConverter.ToInt32 (bytes, 16)/100.0f;
//				//IR.z = (double)BitConverter.ToInt32 (bytes, 16);
//			}
//			break;
//		case 2: // Potentially motion plus?
//
//		default:
//			break;
//		}

		udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);
	}

	public static void setRumble(Wiimote wm, bool state){
		List<Byte> bytes = new List<Byte> ();

		bytes.AddRange (BitConverter.GetBytes ((Int32)1));
		bytes.AddRange (BitConverter.GetBytes ((Int32)wm.ID));
		bytes.AddRange (BitConverter.GetBytes ((Int32)(state?1:0)));
		Debug.Log (BitConverter.ToString(bytes.ToArray ()));
		ws.Send(bytes.ToArray());
	}

	// Setup TCP and UDP connections
	public static void start(string ip, int port){
		ws = new WebSocket ("ws://"+ip+":"+port);
			
		if (!connected) {
			ws.OnMessage += (sender, e) => {
				Byte[] bytes = e.RawData;
				int type = BitConverter.ToInt32 (bytes, 0);

				switch(type){
				case 0:
					{
						int fd = BitConverter.ToInt32 (bytes, 4);

						// TODO: Check if fd already exists in list?
						Wiimote wm = new Wiimote(fd);
						wiimotes.Add(wm);

						// Call the event
						wiimoteConnect(wm);
					}
					break;
				case 1:
					break;
				case 2:
					{
						int fd = BitConverter.ToInt32 (bytes, 4);
						Wiimote wm = wiimotes.Find(_wm => _wm.ID == fd);

						Wiimote.Keys.Key.key_code key_code = (Wiimote.Keys.Key.key_code)BitConverter.ToInt32 (bytes, 8);
						bool key_state = BitConverter.ToInt32 (bytes, 12) == 1;
						wm.keys[key_code] = key_state;
					}
					break;
				default:
					break;
				}
//				Debug.Log ();
			};
			
			ws.OnOpen += (sender, e) => {
				connected = true;
			};

			ws.Connect ();

//			endPoint = new IPEndPoint (IPAddress.Any, 9000);
//			udpClient = new UdpClient (endPoint);
//
//			udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);
		}
	}

	// Close TCP and stop recieving UDP connections
	public static void stop() {
		if (connected) {
			udpClient.Close ();	
			connected = false;
		}
	}
}

public class Wiimote {
	// Potentially deal with ondisconnect here, which gets called from manager (i suppose like the others
	// And have a disconnected thing.
	public delegate void DisconnectHandler();

	public class Keys 
	{
		public delegate void ChangeHandler(Key.key_code keycode, bool state);
		public event ChangeHandler change;

		public struct Key {
			public enum key_code {
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

			public bool state;
		}

		private Key[] keys = new Key[(int)Key.key_code.XWII_KEY_NUM];

		public bool this[Key.key_code index]{
			get
			{
				return keys[(int)index].state;
			}

			set
			{
				keys[(int)index].state = value;
				change(index, keys[(int)index].state);
			}
		}
	}

	public class IRData 
	{
		public delegate void ChangeHandler(IRData irData);
		public event ChangeHandler change;

		public struct IRPoint {
			public uint x;
			public uint y;
		}
			
		private IRPoint[] IR = new IRPoint[2];

		// assumes we dont modify the x or y individually...
		// if we do, need to call change() afterwards
		public IRPoint this[int index]{
			get 
			{
				if (index != 0 || index != 1)
					throw new ArgumentOutOfRangeException ();
				
				return IR[index];
			}

			set 
			{
				if (index != 0 || index != 1)
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
			RemoteWiimoteManager.setRumble(this, value);
			
			_rumble = value;
		}
	}

	private bool _rumble = false;

	public Wiimote(int ID){
		this.ID = ID;
	}
}

