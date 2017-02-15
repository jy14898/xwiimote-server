using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviourScript : MonoBehaviour {
	public string IP = "192.168.0.12";
	public Transform t1;
	public Transform t2;
	public Transform t3;
	public Transform t4;

	private Vector3 p1 = new Vector3();
	private Vector3 p2 = new Vector3();
	private Vector3 p3 = new Vector3();
	private Vector3 p4 = new Vector3();


	// Use this for initialization
	void Start () {
		XWiimoteManager.wiimoteConnect += (Wiimote wiimote) => {
			wiimote.keys.change += (keycode, state) => {
				Debug.Log (keycode + ":" + state);

				if(keycode == Wiimote.Keys.code.XWII_KEY_A){
					wiimote.rumble = state;
				}
			};
				
			wiimote.irData.change += (irData) => {
				p1 = new Vector3(irData[0].x,irData[0].y,0)/100.0f;
				p2 = new Vector3(irData[1].x,irData[1].y,0)/100.0f;
				p3 = new Vector3(irData[2].x,irData[2].y,0)/100.0f;
				p4 = new Vector3(irData[3].x,irData[3].y,0)/100.0f;
			};
		};

		XWiimoteManager.start(IP,9000);
	}
	
	// Update is called once per frame
	void Update () {
		t1.position = p1;
		t2.position = p2;
		t3.position = p3;
		t4.position = p4;
	}
}
