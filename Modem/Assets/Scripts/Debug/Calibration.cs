using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

using UnityEditor;

[RequireComponent(typeof(AudioSource))]
public class Calibration : MonoBehaviour {

	//
	//	TODO: Also calibrate lerped pitch delay by freq
	//



	[Header("Params")]
	public float detectFreqThreshold = 100f;	//TODO: Should this be linear?
	public float detectVolThreshold = 1f;	//TODO: Should this be linear?
	public float delayTimeout = 5f;
	public float endSilence = 1f;
	public float eachDuration = 2f;
	public float delayTestTone = 440f;
	public int delayTestTimes = 4;
	public int lodPitch = 2;

	public float calibrationFreq = 48000f;

	[Header("Feedback")]
	public float delayFDT = -1f;
	public float delayDT = -1f;

	AudioSource _asrc;
	AudioMeasureCS _msr;

	public AnimationCurve fixManual;
	public AnimationCurve fixCalcd;

	string _step = "";

	const int rate = 44000;
	const int channels = 1;


	AudioClip CreateFromSamples(float[] freqs) {
		var eachSamples = (int)(rate * eachDuration);
		var silenceSamples = (int)(rate * endSilence);
		var totalSamples = freqs.Length * eachSamples + silenceSamples;

		var clip = AudioClip.Create("Freqs", totalSamples, channels, rate, false);
		var values = new float[totalSamples * channels];
		var phaseOffset = 0f;
		for(var i = 0; i<freqs.Length; i++) {
			float value = 0f;
			for(int s = 0; s<eachSamples; s++) {
				value = Mathf.Sin(phaseOffset + 2 * Mathf.PI * s * freqs[i] / rate);
				for(int c = 0; c < channels; c++)
					values[i*eachSamples + s + c] = value;
			}
			//Accumulate phase shift to avoid harmonics on sine hard-reset:
			phaseOffset += 2 * Mathf.PI * eachSamples * freqs[i] / rate;
		}
		for(int s = 0; s<silenceSamples; s++)
			values[freqs.Length * eachSamples + s] = 0f;
		
		clip.SetData(values, 0);
		return clip;
	}

	void Start () {
		_msr = FindObjectOfType<AudioMeasureCS>();
		Debug.Assert(_msr);
		_asrc = GetComponent<AudioSource>();
		Debug.Assert(_asrc);
	}

	IEnumerator _DelayCalibrationOnce(List<float> list, YieldInstruction wait, Func<float> dtFn) {
		_asrc.Play();
		float t = 0f;
		while(t < delayTimeout && (Mathf.Abs(_msr.PitchValueX - delayTestTone) > detectFreqThreshold || _msr.DbValue < detectVolThreshold)) {
			t += dtFn();
			yield return wait;
		}
		_asrc.Stop();
		while(_asrc.isPlaying)
			yield return null;
		list.Add(t);
		yield return new WaitForSeconds(0.5f);
	}

	IEnumerator DelayCalibration() {
		//Create tone
		_asrc.clip = CreateFromSamples(new float[] { delayTestTone });
		var list = new List<float>(delayTestTimes);
		Func<float> dtFn;

		//Check with render times
		//NOTE: This will no longer work as AudioMeasureCS no longer uses Update to analyze data.
		//_step = "Calibrating deltaTime delay";
		//Func<float> dtFn = ()=>Time.deltaTime;
		//for(var i = 0; i<delayTestTimes; i++) {
		//	yield return StartCoroutine(_DelayCalibrationOnce(list, null, dtFn));
		//}
		//delayDT = list.Median();
		//list.Clear();

		//Check with fixed time
		_step = "Calibrating fixedDeltaTime delay";
		var wait = new WaitForFixedUpdate();
		dtFn = ()=>Time.fixedDeltaTime;
		for(var i = 0; i<delayTestTimes; i++) {
			yield return StartCoroutine(_DelayCalibrationOnce(list, wait, dtFn));
		}
		delayFDT = list.Median();

		_step = "Delay calibration done";
	}



	IEnumerator PitchVolumeCalibration(IEnumerable<float> freqs) {

		var applySpectrumMapPrev = _msr.applySpectrumMap;
		_msr.applySpectrumMap = false;

		_step = "Pitch-volume sweep (shh!)";

		//octave sweep
		var sweep = freqs.ToArray();
		//var sweep = Enumerable.Range(1, 64).Select(i => i * calibrationFreq / 2 / 16).ToArray();
		//Create tone
		_asrc.clip = CreateFromSamples(sweep);

		var volumes = sweep.Select(x => new List<float>(Mathf.CeilToInt(rate * eachDuration))).ToArray();
		var wait = new WaitForFixedUpdate();
		_asrc.loop = false;
		_asrc.Play();
		//TODO: offset detection by delayFDT and check only if matched index matched?
		//		only move forward and resync or show desync msg if debugging
		while(_asrc.isPlaying) {
			for(int i = 0; i<volumes.Length; i++) {
				if(Mathf.Abs(_msr.PitchValueX - sweep[i]) < detectFreqThreshold) {
					volumes[i].Add(_msr.DbValue);
				}
			}
			yield return wait;
		}


		var minVolume = -160f;
		var maxVolume = 30f;
		var targetVolume = 0f;

		var dbs = volumes.Select(vs => vs.DefaultIfEmpty(minVolume).Median() - minVolume).ToArray();
		fixCalcd = AnimationCurve.Linear(0f,1f,1f,1f);

		//TODO: Remove unhearable before plot.

		var vx = Vector3.right * (1000f / calibrationFreq / 2);
		var vy = Vector3.up * 5f;
		for(int i = 0; i<sweep.Length-1; i++) {
			var fa = sweep[i]; 
			var fb = sweep[i+1]; 
			var va = dbs[i];
			var vb = dbs[i+1];

			if(va-minVolume > detectVolThreshold) {
				var kf = new Keyframe(fa, (targetVolume-minVolume)/va);
				fixCalcd.AddKey(kf);
			}

			//AnimationUtility.SetKeyLeftTangentMode(fixCalcd, i, AnimationUtility.TangentMode.
			Debug.DrawLine(vx * fa + vy * va, vx * fb + vy * vb, Color.red, 60f);
			
			//TODO: not counting LOD => Enumerable.Range(0, 11 * lod).Select(i => 27.5f * Mathf.Pow(2, (float)i/lod));

			//var xa = fix.Evaluate(Mathf.Log(fa / 27.5f, 2f) / sweep.Length) * 100;
			//var xb = fix.Evaluate(Mathf.Log(fb / 27.5f, 2f) / sweep.Length) * 100;
			var xa = fixManual.Evaluate(fa / calibrationFreq / 2);
			var xb = fixManual.Evaluate(fb / calibrationFreq / 2);
			Debug.DrawLine(vx * fa + vy * xa * 100, vx * fb + vy * xb * 100, Color.blue, 60f);

			var fixa2 = xa * va;
			var fixb2 = xb * vb;
			Debug.DrawLine(vx * fa + vy * fixa2, vx * fb + vy * fixb2, Color.cyan, 60f);

			Debug.Log(string.Format(
				"F{0:0} avg dB = {1:0.00} (from {2} samples)"
				, sweep[i]
				, va
				, volumes[i].Count
				));
		}

		for(int i = 0; i<fixCalcd.length; i++)
			AnimationUtility.SetKeyBroken(fixCalcd, i, true);

		_step = "Pitch-volume sweep done";

		_msr.applySpectrumMap = applySpectrumMapPrev;
	}

	void OnGUI() {
		IMGUI.Begin(this, "Calibrate");
			GUILayout.Label("Step: " + _step);
			//GUILayout.Label("Delay DT: " + delayDT);
			GUILayout.Label("Delay FDT: " + delayFDT + " (" + Mathf.RoundToInt(delayFDT/Time.fixedDeltaTime) + "F)");
			GUILayout.Label("Volume");
			_asrc.volume = GUILayout.HorizontalSlider(_asrc.volume, 0f, 1f);
		
			if(GUILayout.Button("Calibrate delay"))
				StartCoroutine(DelayCalibration());

			if(GUILayout.Button("Pitch to volume calibration")) {
				var halfOctaves = Enumerable.Range(1 * lodPitch, 9 * lodPitch).Select(i => 27.5f * Mathf.Pow(2, (float)i/lodPitch));
				StartCoroutine(PitchVolumeCalibration(halfOctaves));
			}

			if(GUILayout.Button("Full range volume calibration\n(use eachDuration=0.1)")) {
				var big = Enumerable.Range(0, 256).Select(i => i * calibrationFreq/2 / 1024f);
				//var big = Enumerable.Range(0, 1024).Select(i => i * calibrationFreq/2 / 1024f);
				StartCoroutine(PitchVolumeCalibration(big));
			}
			if(GUILayout.Button("Copy fixCalcd to detector")) {
				_msr.spectrumMap = fixCalcd;
			}
			if(GUILayout.Button("Stop all")) {
				StopAllCoroutines();
				_asrc.Stop();
			}
		IMGUI.End();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

