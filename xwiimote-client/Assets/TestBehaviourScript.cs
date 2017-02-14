using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviourScript : MonoBehaviour {
	public string IP = "192.168.0.12";

	// Use this for initialization
	void Start () {
		RemoteWiimoteManager.wiimoteConnect += (Wiimote wiimote) => {
			wiimote.keys.change += (keycode, state) => {
				Debug.Log (keycode + ":" + state);

				if(keycode == Wiimote.Keys.Key.key_code.XWII_KEY_A){
					wiimote.rumble = state;
				}
			};

			
		};

		RemoteWiimoteManager.start(IP,9000);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
