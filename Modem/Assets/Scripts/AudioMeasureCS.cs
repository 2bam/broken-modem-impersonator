﻿using UnityEngine;

public class AudioMeasureCS : MonoBehaviour
{
	public float RmsValue;
	public float DbValue;
	public float PitchValue;

	private const int QSamples = 512;//1024 * 8;//1024;
	private const float RefValue = 0.1f;
	private const float Threshold = 0.02f;

	float[] _samples;
	private float[] _spectrum;
	private float _fSample;

	void Start()
	{
		_samples = new float[QSamples];
		_spectrum = new float[QSamples];
		_fSample = AudioSettings.outputSampleRate;
	}

	void Update()
	{
		AnalyzeSound();
	}

	void AnalyzeSound()
	{
		GetComponent<AudioSource>().GetOutputData(_samples, 0); // fill array with samples

		float sum = 0;
		for (var i = 0; i < QSamples; i++)
		{
			sum += _samples[i] * _samples[i]; // sum squared samples
		}
		RmsValue = Mathf.Sqrt(sum / QSamples); // rms = square root of average
		DbValue = 20 * Mathf.Log10(RmsValue / RefValue); // calculate dB
		if (DbValue < -160) DbValue = -160; // clamp it to -160dB min
											// get sound spectrum
		//GetComponent<AudioSource>().GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
		GetComponent<AudioSource>().GetSpectrumData(_spectrum, 0, FFTWindow.Hamming);
		float maxV = 0;
		var maxN = 0;
		for (var i = 0; i < QSamples; i++)
		{ // find max 
			if (!(_spectrum[i] > maxV) || !(_spectrum[i] > Threshold))
				continue;

			maxV = _spectrum[i];
			maxN = i; // maxN is the index of max
		}
		float freqN = maxN; // pass the index to a float variable
		if (maxN > 0 && maxN < QSamples - 1)
		{ // interpolate index using neighbours
			var dL = _spectrum[maxN - 1] / _spectrum[maxN];
			var dR = _spectrum[maxN + 1] / _spectrum[maxN];
			freqN += 0.5f * (dR * dR - dL * dL);
		}
		PitchValue = freqN * (_fSample / 2) / QSamples; // convert index to frequency

		var spectrum = _spectrum;
		var offset = Vector3.zero;// var offset = Vector3.up * 5f;
		for (var i = 1; i < spectrum.Length - 1; i++)
		{
			Debug.DrawLine(offset + new Vector3(i - 1, spectrum[i] + 10, 0), offset + new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
			Debug.DrawLine(offset + new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), offset + new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
			Debug.DrawLine(offset + new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), offset + new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
			Debug.DrawLine(offset + new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), offset + new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
		}
	}
}