using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//https://answers.unity.com/questions/1113690/microphone-input-in-unity-5x.html


//TODO: Read https://electronics.stackexchange.com/questions/239730/performing-fft-at-low-frequencies-but-high-resolution

public class Detector : MonoBehaviour {
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

	[Range(0f, 1f)]
	public float silenceChargeTime = 0.5f;
	[Range(0f, 0.2f)]
	public float noteChargeTime = 0.02f;

	public Text txtVolumeThreshold;
	public bool enableTexFeedback = true;

	float _lastPitch;
	int _lastPitchIndex;


	public static readonly Dictionary<SoundChars, string> SoundToChar = new Dictionary<SoundChars, string> {
		{ SoundChars.Bip, "I" }
		, { SoundChars.Bop, "O" }
		, { SoundChars.Rrr, "R" }
		, { SoundChars.Silence, "_" }
	};

	// Use this for initialization
	IEnumerator Start () {

		//HACK: only one slider
		Debug.Assert(FindObjectsOfType<Slider>().Length == 1);
		FindObjectOfType<Slider>().value = volumeThreshold;

		var aud = GetComponent<AudioSource>();
		int minFreq, maxFreq;
		for (int i = 0; i < Microphone.devices.Length; i++)
		{
			Microphone.GetDeviceCaps(Microphone.devices[i], out minFreq, out maxFreq);
			Debug.Log(string.Format("Found mic[{0}] = {1} -- {2} -- {3}"
				, i
				, Microphone.devices[i]
				, Microphone.IsRecording(Microphone.devices[i]) ? "MIC REC" : "MIC NO REC"
				, "FREQ " + minFreq + "-" + maxFreq
				));
		}

		Microphone.GetDeviceCaps(Microphone.devices[0], out minFreq, out maxFreq);
		aud.clip = Microphone.Start(Microphone.devices[0], true, 10, maxFreq);
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

		
		var pitch = _queue
			.Select(x => x.pitch)
			.Skip(_queue.Count - avgPitchCount)
			//.Average();
			.Median();
		//var pitch = c.PitchValue;

		var str = string.Format("P{0:0.00}[{2:0.00}] V{1:0.00}", c.PitchValue, c.DbValue, pitch) + "\n"
			+ _code;
		GetComponent<Text>().text = str;

		//Draw in texture
		if (enableTexFeedback && _texFeedback != null)
		{
			var y0 = Mathf.Clamp((int)(c.pitchIndex * _texFeedback.height / c.QSamples), 0, _texFeedback.height - 1);
			var y1 = Mathf.Clamp((int)(_lastPitchIndex * _texFeedback.height / c.QSamples), 0, _texFeedback.height - 1);
			_lastPitchIndex = c.pitchIndex;
			if (y0 > y1) {
				var t = y0;
				y0 = y1;
				y1 = t;
			}
			for (int y = 0; y < _texFeedback.height; y++)
				_texFeedback.SetPixel(_iFb, y, y0<=y&&y<=y1 ? Color.red : Color.white);

			for (int y = 0; y < _texFeedback.height; y++)
				_texFeedback.SetPixel(_iFb, y, y0 <= y && y <= y1 ? Color.red : Color.white);

			if (_iFb > 0)
			{
				_texFeedback.SetPixel(_iFb, _texFeedback.height / 2, Color.blue);
				_texFeedback.SetPixel(_iFb - 1, _texFeedback.height / 2, Color.white);
			}

			_texFeedback.Apply();
			_iFb++;
			if (_iFb == _texFeedback.width) _iFb = 0;
		}

		SoundChars curr = SoundChars.Silence;

		if (
			vflips > flipsThreshold
			&& Mathf.Abs(pitch - _lastPitch) < samePitchSensitivity
			&& c.DbValue > rrrVolumeThreshold
		) {
			Utility.LogInfo("RRR " + vflips);
			curr = SoundChars.Rrr;
		}
		else if (c.DbValue > volumeThreshold)
		{
			if (2000f < pitch && pitch < 5000f) curr = SoundChars.Bip;
			else if (1f <= pitch && pitch < 900f) curr = SoundChars.Bop;
		}

		if (Input.GetKey(KeyCode.X))
		{
			Debug.Log("X PITCH INDEX " + c.pitchIndex + " PITCH " + c.PitchValue + " CALCD PITCH=" + pitch);
		}

		_lastPitch = pitch;
		_charge += Time.deltaTime;

		if (curr == SoundChars.Silence)
		{
			if (_charge > silenceChargeTime)
			{
				_charge = 0f;
				_detected = false;
				_last = curr;
				Utility.LogInfo("Reset!");
			}
		}
		else
		{
			if (curr != _last)
			{
				_last = curr;
				_charge = 0f;
				_detected = false;
				Utility.LogInfo("Change!");
			}

			if (_charge > noteChargeTime && !_detected)
			{
				_detected = true;
				_code += SoundToChar[curr];
				Utility.LogInfo(str);
				Utility.LogInfo("PITCH INDEX " + c.pitchIndex + " PITCH " + c.PitchValue + " CALCD PITCH="+pitch);
			}
		}
		

	}
}
