using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviourScript : MonoBehaviour {
	public string IP = "192.168.0.12";
	public Transform t1;
	public Transform t2;
	public Transform t3;
	public Transform t4;

	private Vector2 p1 = new Vector2();
	private Vector2 p2 = new Vector2();
	private Vector2 p3 = new Vector2();
	private Vector2 p4 = new Vector2();


	// Use this for initialization
	void Start () {
		XWiimoteClient.wiimoteConnect += (XWiimoteEvents wiimote) => {
			Debug.Log("Wiimote ID" + wiimote.ID + " connected.");

			wiimote.keyChange += (keycode, state) => {
				Debug.Log (keycode + ":" + state);

				if(keycode == XWiimoteEvents.KeyCode.XWII_KEY_A){
					XWiimoteClient.setRumble(wiimote.ID, state == 1);
				}
			};
				
			wiimote.irChange += (irData) => {
				p1 = new Vector2(irData[0].x,irData[0].y)/10.0f;
				p2 = new Vector2(irData[1].x,irData[1].y)/10.0f;
				p3 = new Vector2(irData[2].x,irData[2].y)/10.0f;
				p4 = new Vector2(irData[3].x,irData[3].y)/10.0f;
			};

			wiimote.disconnect += () => {
				Debug.Log("Wiimote ID" + wiimote.ID + " disconnected.");
			};
		};

		XWiimoteClient.start(IP,9000);
	}

	void OnGUI() {
		GUI.Box (new Rect (p1,new Vector2(1,1)), "1");
		GUI.Box (new Rect (p2,new Vector2(1,1)), "2");
		GUI.Box (new Rect (p3,new Vector2(1,1)), "3");
		GUI.Box (new Rect (p4,new Vector2(1,1)), "4");

	}

	void OnDisable(){
		XWiimoteClient.stop ();
	}
}
