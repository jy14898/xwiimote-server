using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviourScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Wiimote w = new Wiimote ("UNIQUE-ID");
		w.keys.change += (keycode, state) => {
			Debug.Log(keycode + ":" + state);
		};

		w.keys[Wiimote.Keys.Key.key_code.XWII_KEY_A] = true;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
