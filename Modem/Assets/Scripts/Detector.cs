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

	MicProxy _micProxy;
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
	public float rrrVolumeThresholdDelta = -3f;
	public int avgPitchCount = 3;
	public float rrrSamePitchSensitivity = 1000f;

	[Range(0f, 1f)]
	public float silenceChargeTime = 0.5f;
	[Range(0f, 0.5f)]
	public float noteChargeTime = 0.02f;

	public Text txtVolumeThreshold;
	public bool enableTexFeedback = true;

	Queue<SoundChars> _wordSounds = new Queue<SoundChars>();
	public Slider sliderVolumeThreshold;

	float _lastPitch;
	int _lastPitchIndex;
	float _lastDb;

	public float specIncPerSec;
	public float specDecPerSec;

	Dictionary<SoundChars, float> specificAmount = new Dictionary<SoundChars, float>()
	{
		  { SoundChars.Bip, 0f }
		, { SoundChars.Bop, 0f }
		, { SoundChars.Rrr, 0f }
		, { SoundChars.Silence, 0f }
		
	};

	Dictionary<SoundChars, float> specificChargeIncFactor = new Dictionary<SoundChars, float>()
	{
		  { SoundChars.Bip, 1f }
		, { SoundChars.Bop, 1f }
		, { SoundChars.Rrr, 3.5f }
		, { SoundChars.Silence, 0.25f }

	};

	Dictionary<SoundChars, float> specificChargeThreshold = new Dictionary<SoundChars, float>()
	{
		  { SoundChars.Bip, 1f }
		, { SoundChars.Bop, 1f }
		, { SoundChars.Rrr, 1f }
		
	};

	public static readonly Dictionary<SoundChars, string> SoundToChar = new Dictionary<SoundChars, string> {
		{ SoundChars.Bip, "I" }
		, { SoundChars.Bop, "O" }
		, { SoundChars.Rrr, "R" }
		, { SoundChars.Silence, "_" }
	};

	// Use this for initialization
	IEnumerator Start () {
		volumeThreshold = PlayerPrefs.GetFloat("volumeThreshold", volumeThreshold);
		sliderVolumeThreshold.value = -volumeThreshold;

		_micProxy = FindObjectOfType<MicProxy>();

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

		_texFeedback = new Texture2D(128, 1024);
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
		volumeThreshold = -slider.value;
		PlayerPrefs.SetFloat("volumeThreshold", volumeThreshold);
		PlayerPrefs.Save();
		//txtVolumeThreshold.text = volumeThreshold.ToString("0.0") + "dB";
		//User friendly
		txtVolumeThreshold.text = (Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 10f).ToString("0.0");
	}

	void TextureLine(int x, int y0, int y1, Color color) {
		if (y0 > y1)
		{
			var t = y0;
			y0 = y1;
			y1 = t;
		}
		y0 -= 5;
		y1 += 5;

		for (int y = y0; y < y1; y++)
			_texFeedback.SetPixel(_iFb, y, color);


	}

	SoundChars winning;
	void IncAmount(SoundChars which) {
		specificAmount[which] += specIncPerSec * specificChargeIncFactor[which] * Time.deltaTime;

		winning = specificAmount
			.Where(kv => kv.Key != SoundChars.Silence && kv.Value > 1f)
			.OrderByDescending(kv => kv.Value)
			.ThenBy(kv => kv.Key == SoundChars.Rrr)
			.Select(x => x.Key)
			.DefaultIfEmpty(SoundChars.Silence)
			.First()
			;

		if (specificAmount[which] > 1f)
		{
/*			Debug.Log("INC TH " + which.ToString());
			Debug.Log("WINNING " + winning.ToString() );*/
		}
		if (specificAmount[which] > 2f)
			specificAmount[which] = 2f;
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawCube(Vector3.right * 1f, Vector3.one + Vector3.up * specificAmount[SoundChars.Bip]);
		Gizmos.DrawCube(Vector3.right * 3f, Vector3.one + Vector3.up * specificAmount[SoundChars.Bop]);
		Gizmos.DrawCube(Vector3.right * 5f, Vector3.one + Vector3.up * specificAmount[SoundChars.Rrr]);
		//Gizmos.DrawCube(Vector3.right * 7f, Vector3.one + Vector3.up * specificAmount[SoundChars.Silence]);
	}

	// Update is called once per frame
	void Update() {

		foreach (var k in specificAmount.Keys.ToArray()) {
			specificAmount[k] = Mathf.Max(0f, specificAmount[k] - specDecPerSec * Time.deltaTime);
		}

		if (Input.GetKeyDown(KeyCode.R) && Application.isEditor)
			_code = "";

		var c = GetComponent<AudioMeasureCS>();

		//FIXME: THIS QUEUE'S EFFECTS WILL BE AFFECTED BY FRAMERATE
		_queue.Enqueue(new QElem(c.PitchValue, c.DbValue));
		if (_queue.Count > maxQLen)
			_queue.Dequeue();

		//Debug.Log(_volumeQ.Aggregate("", (a, x) => a + string.Format("  {0:0.0}", x)));
		var reversedQ = _queue.Reverse();       //Last first!
		var vflips = reversedQ.Aggregate(
			new { flips = 0, last = reversedQ.First().volume > volumeThreshold + rrrVolumeThresholdDelta }
			, (a, x) =>
				{
					var val = x.volume > volumeThreshold + rrrVolumeThresholdDelta;
					return new { flips = a.flips + (val != a.last ? 1 : 0), last = val };
				}
			)
			.flips;


		var pitch = reversedQ
			.Select(x => x.pitch)
			.Take(avgPitchCount)
			//.Skip(_queue.Count - avgPitchCount)
			//.Average();
			.Median();
		//.Max();		//Reason: "B" and "P" parts are detected as low-freq, so use the highest
		//var pitch = c.PitchValue;

		var str = string.Format("P{0:0.00}[{2:0.00}] V{1:0.00}", c.PitchValue, c.DbValue, pitch) + "\n"
			+ _code;
		GetComponent<Text>().text = str;

		//Draw in texture
		if (enableTexFeedback && _texFeedback != null)
		{
			int y0, y1;
			TextureLine(_iFb, 0, _texFeedback.height, Color.white);
			y0 = Mathf.Clamp((int)((c.DbValue * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
			y1 = Mathf.Clamp((int)((_lastDb * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
			TextureLine(_iFb, y0, y1, Color.cyan);
			y0 = Mathf.Clamp((int)(4f * c.pitchIndex * _texFeedback.height / c.QSamples), 0, _texFeedback.height - 1);
			y1 = Mathf.Clamp((int)(4f * _lastPitchIndex * _texFeedback.height / c.QSamples), 0, _texFeedback.height - 1);
			TextureLine(_iFb, y0, y1, Color.red);

			//Draw current position pointer
			if (_iFb > 0)
			{
				var dbt = Mathf.Clamp((int)((volumeThreshold * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
				var dbt2 = Mathf.Clamp((int)(((volumeThreshold + rrrVolumeThresholdDelta) * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
				var ptf = 1600f / (48000f / 2f) * (float)c.QSamples; // convert index to frequency
				var pt = Mathf.Clamp((int)(4f * ptf * _texFeedback.height / c.QSamples), 0, _texFeedback.height - 1);

				TextureLine(_iFb, dbt, dbt, Color.blue);
				TextureLine(_iFb, dbt2, dbt2, Color.gray);
				TextureLine(_iFb, pt, pt, Color.magenta);
				TextureLine(_iFb, _texFeedback.height / 2, _texFeedback.height / 2, Color.black);
				TextureLine(_iFb - 1, _texFeedback.height / 2, _texFeedback.height / 2, Color.white);

				//_texFeedback.SetPixel(_iFb, dbt, Color.blue);
				//_texFeedback.SetPixel(_iFb, _texFeedback.height / 2, Color.black);
				//_texFeedback.SetPixel(_iFb - 1, _texFeedback.height / 2, Color.white);
			}

			_texFeedback.Apply();
			_iFb++;
			if (_iFb == _texFeedback.width) _iFb = 0;
		}

		SoundChars curr = SoundChars.Silence;

		if (
			vflips > flipsThreshold
			&& Mathf.Abs(pitch - _lastPitch) < rrrSamePitchSensitivity
			//&& c.DbValue > volumeThreshold * rrrVolumeThresholdFactor
		) {
			Utility.LogInfo("RRR " + vflips);
			curr = SoundChars.Rrr;
			IncAmount(SoundChars.Rrr);
		}
		
		if (c.DbValue > volumeThreshold)
		{
			SoundChars curr2 = SoundChars.Silence;
			if (1200f < pitch && pitch < 5000f) curr2 = SoundChars.Bip;
			else if (1f <= pitch && pitch < 1200f) curr2 = SoundChars.Bop;
			
			if(curr2 != SoundChars.Silence)
				IncAmount(curr2);

			if (curr == SoundChars.Silence)
				curr = curr2;
		}

		if (curr == SoundChars.Silence)
			IncAmount(curr);

		if (Input.GetKey(KeyCode.X))
		{
			Debug.Log("X PITCH INDEX " + c.pitchIndex + " PITCH " + c.PitchValue + " CALCD PITCH=" + pitch);
		}

		_charge += Time.deltaTime;

		if (!_micProxy.microphoneEnabled)
			_wordSounds.Clear();

		//Magic override
		curr = winning;

		if (curr == SoundChars.Silence)
		{
			if (_charge > silenceChargeTime)
			{
				if(curr != _last)
					_micProxy.OnEndChar(_wordSounds.Count);
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
				//_charge = 0f;
				_detected = false;
				Utility.LogInfo("Change!");
			}
			else if (_charge > noteChargeTime * specificChargeThreshold[curr] && !_detected)
			{
				_micProxy.OnBeginChar(curr, _wordSounds.Count);
				_detected = true;
				_code += SoundToChar[curr];
				_wordSounds.Enqueue(curr);
				if (_wordSounds.Count == Word.MAX_DIGITS)
				{
					//NOTE: For different sized words the feed should be sent on a silence.
					var word = AppData.Instance.AvailableWords
						.FirstOrDefault(w => w.BipBopValues.Zip(_wordSounds, (s1, s2) => s1 == s2).All(x => x));
					_code += ".";

					if (word != null)
					{
						_micProxy.Feed(word);
						_code += "!!";
						_wordSounds.Clear();
					}
					else
						_wordSounds.Dequeue();
				}
				Utility.LogInfo(str);
				Utility.LogInfo("PITCH INDEX " + c.pitchIndex + " PITCH " + c.PitchValue + " CALCD PITCH="+pitch);
			}
		}
		_lastPitch = pitch;
		_lastDb = c.DbValue;
		_lastPitchIndex = c.pitchIndex;
		_micProxy.SetInstantSoundingChar(_last);

	}
}
