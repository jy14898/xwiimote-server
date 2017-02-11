using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

/**
 * Do the talkin with the Wiimotes
 * Provide unique identifiers?
 * Produce wiimote objects
 * Allow WiimoteSubscribers
 * 
 **/
public class RemoteWiimoteManager {
	private static UdpClient udpClient = null;
	private static TcpClient tcpClient = null;
	private static IPEndPoint endPoint = null;
	private static bool connected = false;

	private static List<Wiimote> wiimotes = new List<Wiimote>();

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

	// Setup TCP and UDP connections
	public static void start(){
		if (!connected) {
			endPoint = new IPEndPoint (IPAddress.Any, 9000);
			udpClient = new UdpClient (endPoint);

			udpClient.BeginReceive (new AsyncCallback (RecieveUDP), null);

			connected = true;
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

	public readonly string ID;

	public bool rumble 
	{
		get
		{
			return _rumble;
		}

		set 
		{
			// Send rumble signal
			if (value) 
				Debug.Log ("Rumble!");
			else
				Debug.Log ("No rumble!");
			
			_rumble = value;
		}
	}

	private bool _rumble = false;

	public Wiimote(string ID){
		this.ID = ID;
	}
}

