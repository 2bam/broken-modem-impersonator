using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//https://answers.unity.com/questions/1113690/microphone-input-in-unity-5x.html
//TODO: Read https://electronics.stackexchange.com/questions/239730/performing-fft-at-low-frequencies-but-high-resolution

class MicDummy : IMicProxy {
	public bool microphoneEnabled {get;set;}
	public void Feed(Word word) {}
	public void SetInstantSoundingChar(SoundChars current) {}
	public void OnBeginChar(SoundChars current, int currentIndex) {}
	public void OnEndChar(int currentIndex) {}
	public void OnLongSilence() {} 
	public MicDummy() {
		microphoneEnabled = true;
	}
}

[RequireComponent(typeof(AudioMeasureCS), typeof(AudioSource))]
public class Detector : MonoBehaviour {
	public float volumeThreshold = 0.18f;

	string _code;
	float _charge;
	bool _detected;
	SoundChars _last = SoundChars.Silence;

	[HideInInspector] public IMicProxy micProxy;
	TexCanvas _texFeedback;
	public Renderer rendFeedback;

	int _iFb;
	AudioMeasureCS _msr;

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
	public int queueMaxLength = 16;
	public int flipsThreshold = 3;
	public float rrrVolumeThresholdDeltaDiff = -3f;
	//public float rrrVolumeDelta = -5f;
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
	SoundChars winning;
	
	public float specIncPerSec;
	public float specDecPerSec;
	public int dbAvgQAmt = 6;

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
		, { SoundChars.Rrr, 1.05f }
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

	public void SetVolumeThreshold(Slider slider)
	{
		volumeThreshold = -slider.value;
		PlayerPrefs.SetFloat("volumeThreshold", volumeThreshold);
		PlayerPrefs.Save();
		//txtVolumeThreshold.text = volumeThreshold.ToString("0.0") + "dB";
		//User friendly
		txtVolumeThreshold.text = (Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 10f).ToString("0.0");
	}



	// Use this for initialization
	IEnumerator Start () {
		volumeThreshold = PlayerPrefs.GetFloat("volumeThreshold", volumeThreshold);
		sliderVolumeThreshold.value = -volumeThreshold;	//This will trigger a SetVolumeThreshold update. 

		if(micProxy == null)	//Avoid ifs
			micProxy = new MicDummy();

		_msr = GetComponent<AudioMeasureCS>();
		var asrc = GetComponent<AudioSource>();
		_texFeedback = new TexCanvas(rendFeedback, 128, 1024);

		int minFreq, maxFreq;
		Microphone.GetDeviceCaps(Microphone.devices[0], out minFreq, out maxFreq);
		asrc.clip = Microphone.Start(Microphone.devices[0], true, 10, maxFreq);	//TODO: Find out lengthSec param (10) influence
		asrc.loop = true;
		//asrc.mute = true;	--> NOTE: Can't mute output and also get spectrum, had to use AudioMixer strange workaround

		//Wait mic to start feeding samples
		while(Microphone.GetPosition(null) <= 0) {
			yield return null;
			Debug.Log("Waiting mic");
		}
		Debug.Log("MIC START " + (Microphone.IsRecording(Microphone.devices[0]) ? "REC" : "XXX") + " FREQ=" + maxFreq);
		asrc.Play();
	}

	void IncAmount(SoundChars which) {
		specificAmount[which] += specIncPerSec * specificChargeIncFactor[which] * Time.deltaTime;

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
		Gizmos.matrix = Matrix4x4.Scale(Vector3.one * 100f);
		Gizmos.DrawCube(Vector3.right * 1f, Vector3.one + Vector3.up * specificAmount[SoundChars.Bip]);
		Gizmos.DrawCube(Vector3.right * 3f, Vector3.one + Vector3.up * specificAmount[SoundChars.Bop]);
		Gizmos.DrawCube(Vector3.right * 5f, Vector3.one + Vector3.up * specificAmount[SoundChars.Rrr]);
		//Gizmos.DrawCube(Vector3.right * 7f, Vector3.one + Vector3.up * specificAmount[SoundChars.Silence]);
	}

	// Update is called once per frame
	void FixedUpdate() {

		foreach (var k in specificAmount.Keys.ToArray()) {
			specificAmount[k] = Mathf.Max(0f, specificAmount[k] - specDecPerSec * Time.deltaTime);
		}

		if (Input.GetKeyDown(KeyCode.R) && Application.isEditor)
			_code = "";

		//NOTE: THIS QUEUE'S EFFECTS WILL BE AFFECTED BY FRAMERATE IF NOT IN FIXED UPDATE.
		_queue.Enqueue(new QElem(_msr.PitchValue, _msr.DbValue));
		if (_queue.Count > queueMaxLength)
			_queue.Dequeue();

		//Debug.Log(_volumeQ.Aggregate("", (a, x) => a + string.Format("  {0:0.0}", x)));
		var reversedQ = _queue.Reverse().ToArray();       //Make last input first!
		var vflips = reversedQ.Aggregate(
			new { flips = 0, last = reversedQ.First().volume, ls = 1f }
			, (a, x) =>
				{
					var delta = a.last - x.volume;
					var cls = Mathf.Sign(delta);
					var val = cls*a.ls < 0 && Mathf.Abs(x.volume - a.last) > rrrVolumeThresholdDeltaDiff;
					return new { flips = a.flips + (val ? 1 : 0), last = x.volume, ls = cls };
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

		var maxLastDb = reversedQ
			.Take(dbAvgQAmt)
			.Select(x => x.volume)
			.Max();

		//Draw in texture
		DrawTextureCanvasFeedback(maxLastDb);

		SoundChars curr = SoundChars.Silence;

		if (
			vflips > flipsThreshold
			//&& Mathf.Abs(pitch - _lastPitch) < rrrSamePitchSensitivity
			&& maxLastDb > volumeThreshold
		) {
			Utility.LogInfo("RRR " + vflips);
			curr = SoundChars.Rrr;
			IncAmount(SoundChars.Rrr);
		}
		
		if (_msr.DbValue > volumeThreshold)
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



		_charge += Time.deltaTime;

		if (!micProxy.microphoneEnabled)
			_wordSounds.Clear();

		winning = specificAmount
			.Where(kv => kv.Key != SoundChars.Silence && kv.Value > 1f)
			.OrderByDescending(kv => kv.Value)
			.ThenBy(kv => kv.Key != SoundChars.Rrr)
			.Select(x => x.Key)
			.DefaultIfEmpty(SoundChars.Silence)
			.First()
			;

       //Magic override
		curr = winning;

		if (curr == SoundChars.Silence)
		{
			if (_charge > silenceChargeTime)
			{
				if(curr != _last)
					micProxy.OnEndChar(_wordSounds.Count);
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
				micProxy.OnBeginChar(curr, _wordSounds.Count);
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
						micProxy.Feed(word);
						_code += "!!";
						_wordSounds.Clear();
					}
					else
						_wordSounds.Dequeue();
				}
				Utility.LogInfo("PITCH INDEX " + _msr.pitchIndex + " PITCH " + _msr.PitchValue + " CALCD PITCH="+pitch);
			}
		}
		_lastPitch = pitch;
		_lastDb = _msr.DbValue;
		_lastPitchIndex = _msr.pitchIndex;
		micProxy.SetInstantSoundingChar(_last);

		if (Input.GetKey(KeyCode.X))
		{
			Debug.Log("X PITCH INDEX " + _msr.pitchIndex + " PITCH " + _msr.PitchValue + " CALCD PITCH=" + pitch);
		}
	}

	void DrawTextureCanvasFeedback(float maxLastDb) {
		if (!_texFeedback.enabled)
			return;

		int y0, y1;
		_texFeedback.VLine(_iFb, 0, _texFeedback.height, Color.white);
		y0 = Mathf.Clamp((int)((_msr.DbValue * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
		y1 = Mathf.Clamp((int)((_lastDb * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
		_texFeedback.VLine(_iFb, y0, y1, Color.cyan);
		y0 = Mathf.Clamp((int)(4f * _msr.pitchIndex * _texFeedback.height / _msr.QSamples), 0, _texFeedback.height - 1);
		y1 = Mathf.Clamp((int)(4f * _lastPitchIndex * _texFeedback.height / _msr.QSamples), 0, _texFeedback.height - 1);
		_texFeedback.VLine(_iFb, y0, y1, Color.red);

		//Draw current position pointer
		if (_iFb > 0)
		{
			var dbt = Mathf.Clamp((int)((volumeThreshold * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
			//var dbt2 = Mathf.Clamp((int)(((volumeThreshold + rrrVolumeDelta) * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);
			var ptf = 1600f / (48000f / 2f) * (float)_msr.QSamples; // convert index to frequency
			var pt = Mathf.Clamp((int)(4f * ptf * _texFeedback.height / _msr.QSamples), 0, _texFeedback.height - 1);
			var adb = Mathf.Clamp((int)((maxLastDb * 0.01f + 0.5f) * _texFeedback.height), 0, _texFeedback.height - 1);

			_texFeedback.VLine(_iFb, dbt, dbt, Color.blue);
			_texFeedback.VLine(_iFb, adb, adb, Color.green);
			//TextureLine(_iFb, dbt2, dbt2, Color.gray);
			_texFeedback.VLine(_iFb, pt, pt, Color.magenta);
			_texFeedback.VLine(_iFb, _texFeedback.height / 2, _texFeedback.height / 2, Color.black);
			_texFeedback.VLine(_iFb - 1, _texFeedback.height / 2, _texFeedback.height / 2, Color.white);

			//_texFeedback.SetPixel(_iFb, dbt, Color.blue);
			//_texFeedback.SetPixel(_iFb, _texFeedback.height / 2, Color.black);
			//_texFeedback.SetPixel(_iFb - 1, _texFeedback.height / 2, Color.white);
		}

		_texFeedback.Apply();
		_iFb = (_iFb+1) % _texFeedback.width;
	}

	void DbgEnumMics() {
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
	}

	void OnGUI() {
		IMGUI.Begin(this, "Detector");
			GUILayout.Label(string.Format("P{0:00000.00} V{1:000.00} - {2}", _msr.PitchValue, _msr.DbValue, _msr.PitchValueY));
			GUILayout.Label("CODE:"+_code);
			GUILayout.Label("Queue will record "+ (queueMaxLength * Time.fixedDeltaTime) + " seconds");
			_texFeedback.enabled = GUILayout.Toggle(_texFeedback.enabled, "Graphic feedback");
		IMGUI.End();
	}
}
