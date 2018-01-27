using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Test : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		var aud = GetComponent<AudioSource>();
		aud.clip = Microphone.Start("Built-in Microphone", true, 10, 44100);
		aud.loop = true;
		while (!(Microphone.GetPosition(null) > 0)) {
			yield return null;
			Debug.Log("Waiting mic");
		}
		aud.Play();
	}
	
	// Update is called once per frame
	void Update () {
		var c = GetComponent<AudioMeasureCS>();
		GetComponent<Text>().text = "P" + c.PitchValue + " V" + c.DbValue;
	
	}
}
