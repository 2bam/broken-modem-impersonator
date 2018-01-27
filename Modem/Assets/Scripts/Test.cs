using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//https://answers.unity.com/questions/1113690/microphone-input-in-unity-5x.html

public class Test : MonoBehaviour {
	public float volumeThreshold = 0.18f;

	string _code;
	float _charge;
	bool _detected;
	int _last = -1;
	// Use this for initialization
	IEnumerator Start () {
		var aud = GetComponent<AudioSource>();
		aud.clip = Microphone.Start("Built-in Microphone", true, 10, 44100);
		aud.loop = true;
		//aud.mute = true;
		while (!(Microphone.GetPosition(null) > 0)) {
			yield return null;
			Debug.Log("Waiting mic");
		}
		Debug.Log("Mic on");
		aud.Play();
	}


	// Update is called once per frame
	void Update () {
		var c = GetComponent<AudioMeasureCS>();
		var str = string.Format("P{0:0.00} V{1:0.00}", c.PitchValue, c.DbValue) + "\n"
			+ _code;
		GetComponent<Text>().text = str;

		int curr = -1;
		if (c.DbValue > volumeThreshold)
		{
			//iii 350
			//oo 550
			curr = (c.PitchValue < 430f ? 0 : 1);
		}

		if (curr != _last)
		{
			_last = curr;
			_charge = 0f;
			_detected = false;
			Debug.Log("Reset!");
		}
		else
			_charge += Time.deltaTime;

		if(curr != -1 && _charge > 0.05f && !_detected) {
			_detected = true;
			_code += (curr == 0 ? "I " : "O");

			Debug.Log(str);
		}
		

	}
}
