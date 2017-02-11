using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviourScript : MonoBehaviour {
	public string IP = "192.168.0.12";

	// Use this for initialization
	void Start () {
//		Wiimote w = new Wiimote ("UNIQUE-ID");
//		w.keys.change += (keycode, state) => {
//			Debug.Log(keycode + ":" + state);
//		};
//
//		w.keys[Wiimote.Keys.Key.key_code.XWII_KEY_A] = true;
		RemoteWiimoteManager.start(IP,9000);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
