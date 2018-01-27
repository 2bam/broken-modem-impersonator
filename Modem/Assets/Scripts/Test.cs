using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//https://answers.unity.com/questions/1113690/microphone-input-in-unity-5x.html


//TODO: Read https://electronics.stackexchange.com/questions/239730/performing-fft-at-low-frequencies-but-high-resolution

public class Test : MonoBehaviour {
	public float volumeThreshold = 0.18f;

	string _code;
	float _charge;
	bool _detected;
	int _last = -1;

	Texture2D _texFeedback;
	public Renderer rendFeedback;
	int _iFb;

	Queue<float> _volumeQ = new Queue<float>();
	public int maxQLen = 16;
	public int flipsThreshold = 3;
	public float rrrVolumeThreshold = 0f;

	public Text txtVolumeThreshold;

	// Use this for initialization
	IEnumerator Start () {
		var aud = GetComponent<AudioSource>();
		int minFreq, maxFreq;
		for (int i = 0; i < Microphone.devices.Length; i++)
		{
			Microphone.GetDeviceCaps(Microphone.devices[0], out minFreq, out maxFreq);
			Debug.Log(string.Format("Found mic[{0}] = {1} -- {2} -- {3}"
				, i
				, Microphone.devices[i]
				, Microphone.IsRecording(Microphone.devices[0]) ? "MIC REC" : "MIC NO REC"
				, "FREQ " + minFreq + "-" + maxFreq
				));
		}
		aud.clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
		aud.loop = true;
		//aud.mute = true;
		while(Microphone.GetPosition(null) <= 0) {
			yield return null;
			Debug.Log("Waiting mic");
		}
		Debug.Log("Mic start " + (Microphone.IsRecording(Microphone.devices[0]) ? "MIC REC" : "MIC NO REC"));
		aud.Play();

		_texFeedback = new Texture2D(128, 512);
		rendFeedback.material.mainTexture = _texFeedback;

		/*for (int y = 0; y < _texFeedback.height; y++)
		{
			for (int x = 0; x < _texFeedback.width; x++)
			{
				Color color = ((x & y) != 0 ? Color.white : Color.gray);
				_texFeedback.SetPixel(x, y, color);
			}
		}
		_texFeedback.Apply();*/
	}

	public void SetVolumeThreshold(Slider slider)
	{
		volumeThreshold = slider.value;
		txtVolumeThreshold.text = volumeThreshold.ToString("0.00");
	}

	// Update is called once per frame
	void Update () {
		

		var c = GetComponent<AudioMeasureCS>();
		var str = string.Format("P{0:0.00} V{1:0.00}", c.PitchValue, c.DbValue) + "\n"
			+ _code;
		GetComponent<Text>().text = str;

		_volumeQ.Enqueue(c.DbValue);
		if (_volumeQ.Count > maxQLen)
			_volumeQ.Dequeue();
		//Debug.Log(_volumeQ.Aggregate("", (a, x) => a + string.Format("  {0:0.0}", x)));
		var flips = _volumeQ.Aggregate(
			new { flips = 0, last = _volumeQ.Peek() > rrrVolumeThreshold }
			, (a, x) =>
				{
					var val = x > rrrVolumeThreshold;
					return new { flips = a.flips + (val != a.last ? 1 : 0), last = val };
				}
			)
			.flips;

		if (flips > flipsThreshold)
			Debug.Log("RRR " + flips);

		if (_texFeedback != null)
		{
			for (int y = 0; y < _texFeedback.height; y++)
				_texFeedback.SetPixel(_iFb, y, Color.white);
			_texFeedback.SetPixel(_iFb, Mathf.Clamp((int)(c.PitchValue * 0.4f), 0, _texFeedback.height - 1), Color.red);
			_texFeedback.Apply();
			_iFb++;
			if (_iFb == _texFeedback.width) _iFb = 0;
		}

		int curr = -1;
		if (c.DbValue > volumeThreshold)
		{
			//iii 350
			//oo 550
			curr = (c.PitchValue < 420f ? 0 : 1);


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

		if(curr != -1 && _charge > 0.02f && !_detected) {
			_detected = true;
			_code += (curr == 0 ? "I " : "O");

			Debug.Log(str);
		}
		

	}
}
