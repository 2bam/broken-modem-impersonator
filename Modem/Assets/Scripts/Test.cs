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
	SoundChars _last = SoundChars.Silence;

	Texture2D _texFeedback;
	public Renderer rendFeedback;
	int _iFb;

	struct QElem {
		public float pitch;
		public float volume;
		public QElem(float Pitch, float Volume)
		{
			pitch = Pitch;
			volume = Volume;
		}
	}
	Queue<QElem> _queue = new Queue<QElem>();
	public int maxQLen = 16;
	public int flipsThreshold = 3;
	public float rrrVolumeThreshold = 0f;
	public int avgPitchCount = 3;
	public float samePitchSensitivity = 100f;

	public Text txtVolumeThreshold;

	float _lastPitch;

	public static readonly Dictionary<SoundChars, string> SoundToChar = new Dictionary<SoundChars, string> {
		{ SoundChars.Bip, "I" }
		, { SoundChars.Bop, "O" }
		, { SoundChars.Rrr, "R" }
		, { SoundChars.Silence, "_" }
	};

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

		if (Input.GetKeyDown(KeyCode.R) && Application.isEditor)
			_code = "";

		var c = GetComponent<AudioMeasureCS>();
		var str = string.Format("P{0:0.00} V{1:0.00}", c.PitchValue, c.DbValue) + "\n"
			+ _code;
		GetComponent<Text>().text = str;

		_queue.Enqueue(new QElem(c.PitchValue, c.DbValue));
		if (_queue.Count > maxQLen)
			_queue.Dequeue();

		//Debug.Log(_volumeQ.Aggregate("", (a, x) => a + string.Format("  {0:0.0}", x)));
		var vflips = _queue.Aggregate(
			new { flips = 0, last = _queue.Peek().volume > rrrVolumeThreshold }
			, (a, x) =>
				{
					var val = x.volume > rrrVolumeThreshold;
					return new { flips = a.flips + (val != a.last ? 1 : 0), last = val };
				}
			)
			.flips;

		/*
	var pitch = _queue
		.Select(x => x.pitch)
		.Take(avgPitchCount)
		//.Average();
		.Median();*/
		var pitch = c.PitchValue;


		if (_texFeedback != null)
		{
			for (int y = 0; y < _texFeedback.height; y++)
				_texFeedback.SetPixel(_iFb, y, Color.white);
			_texFeedback.SetPixel(_iFb, Mathf.Clamp((int)(c.PitchValue * 0.4f), 0, _texFeedback.height - 1), Color.red);
			_texFeedback.Apply();
			_iFb++;
			if (_iFb == _texFeedback.width) _iFb = 0;
		}

		SoundChars curr = SoundChars.Silence;

		if (vflips > flipsThreshold && Mathf.Abs(pitch - _lastPitch) < samePitchSensitivity)
		{
			Debug.Log("RRR " + vflips);
			curr = SoundChars.Rrr;
		}
		else if (c.DbValue > volumeThreshold)
		{
			//iii 350
			//oo 550
			curr = pitch < 400f ? SoundChars.Bip : SoundChars.Bop;
		}
		_lastPitch = pitch;

		_charge += Time.deltaTime;

		if (curr == SoundChars.Silence)
		{
			if (_charge > 0.5f)
			{
				_charge = 0f;
				_detected = false;
				_last = curr;
			}
		}
		else
		{
			if (curr != _last)
			{
				_last = curr;
				_charge = 0f;
				_detected = false;
				Debug.Log("Reset!");
			}

			if (_charge > 0.02f && !_detected)
			{
				_detected = true;
				_code += SoundToChar[curr];
				Debug.Log(str);
				Debug.Log("PITCH INDEX " + c.pitchIndex + " PITCH " + c.PitchValue);
			}
		}
		

	}
}
