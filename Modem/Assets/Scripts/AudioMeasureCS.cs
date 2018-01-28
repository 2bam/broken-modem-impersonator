using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioMeasureCS : MonoBehaviour
{
	public float RmsValue;
	public float DbValue;
	public float PitchValue;
	public int pitchIndex;

	public int QSamples = 2048;//512;
	private const float RefValue = 0.1f;
	private const float Threshold = 0.02f;


	//RR = 0.032 - 0.040s
	float[] _samples;
	private float[] _spectrum;
	private float[] _spectrumLerped;
	private float _fSample;

	public float spectrumLerpFactor;

	[Range(8, 15)]
	public int pow2samples = 12;
	public AnimationCurve spectrumMap = AnimationCurve.Linear(0, 0, 1, 1);

	AudioSource _asrc;

	void Start()
	{
		QSamples = (int)Mathf.Pow(2f, pow2samples);
		_samples = new float[QSamples];
		_spectrum = new float[QSamples];
		_spectrumLerped = new float[QSamples];
		_fSample = AudioSettings.outputSampleRate;
		Debug.Log("Output sample rate " + _fSample);
		_asrc = GetComponent<AudioSource>();
	}

	void Update()
	{
		AnalyzeSound();
	}


	void DrawData(float[] spectrum)
	{



		var offset = Vector3.zero;// var offset = Vector3.up * 5f;
		var offset2 = Vector3.up * 5f;// var offset = Vector3.up * 5f;
		var scale = 50f;

		float maxV1=0f, maxV2=0f;
		Vector3 maxP1, maxP2;
		maxP1 = maxP2 = Vector3.zero;
		for (var i = 1; i < spectrum.Length - 1; i++)
		{
			if (spectrum[i] > maxV1)
			{
				maxV1 = spectrum[i];
				maxP1 = new Vector3(Mathf.Log(i), spectrum[i] * scale - 10, 1);
			}
			if (_spectrumLerped[i] > maxV2)
			{
				maxV2 = _spectrumLerped[i];
				maxP2 = new Vector3(Mathf.Log(i), _spectrumLerped[i] * scale - 10, 1);
			}
			

			//Debug.DrawLine(offset + new Vector3(i - 1, spectrum[i] + 10, 0), offset + new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
			//Debug.DrawLine(offset + new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), offset + new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
			Debug.DrawLine(offset + new Vector3(Mathf.Log(i - 1), spectrum[i - 1] * scale - 10, 1) , offset + new Vector3(Mathf.Log(i), spectrum[i] * scale - 10, 1) , Color.green);
			//Debug.DrawLine(offset + new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), offset + new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
			Debug.DrawLine(offset2 + new Vector3(Mathf.Log(i - 1), _spectrumLerped[i - 1] * scale - 10, 1) , offset2 + new Vector3(Mathf.Log(i), _spectrumLerped[i] * scale - 10, 1) , Color.magenta);
		}
		Debug.DrawLine(offset + maxP1 + Vector3.up, offset + maxP1 + Vector3.down, Color.green);
		Debug.DrawLine(offset2 + maxP2 + Vector3.up, offset2 + maxP2 + Vector3.down, Color.magenta);

	}

	void AnalyzeSound()
	{
		_asrc.GetOutputData(_samples, 0); // fill array with samples


		float sum = 0;
		for (var i = 0; i < QSamples; i++)
		{
			sum += _samples[i] * _samples[i]; // sum squared samples
		}
		RmsValue = Mathf.Sqrt(sum / QSamples); // rms = square root of average
		DbValue = 20 * Mathf.Log10(RmsValue / RefValue); // calculate dB
		if (DbValue < -160) DbValue = -160; // clamp it to -160dB min
											// get sound spectrum

		//Read the spectrum
		_asrc.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
		// _asrc.GetSpectrumData(_spectrum, 0, FFTWindow.Hamming);

		//Patch data from hearing curve and accumulate with interpolation.
		var t = spectrumLerpFactor * Time.deltaTime;
		for (int i = 0; i < _spectrum.Length; i++) {
			_spectrum[i] = spectrumMap.Evaluate((float)i / _spectrum.Length) * (_spectrum[i]);
			_spectrumLerped[i] = Mathf.Lerp(_spectrumLerped[i], _spectrum[i], t);
		}

		DrawData(_spectrum);

		AnalyzeSpectrum(_spectrumLerped);
	}

	void AnalyzeSpectrum(float[] spec)
	{
		float maxV = 0;
		var maxN = 0;
		for (var i = 0; i < QSamples; i++)
		{ // find max 
			if (spec[i] <= maxV || spec[i] <= Threshold)
				continue;

			maxV = spec[i];
			maxN = i; // maxN is the index of max
		}
		float freqN = maxN; // pass the index to a float variable
		if (maxN > 0 && maxN < QSamples - 1)
		{ // interpolate index using neighbours
			var dL = spec[maxN - 1] / _spectrum[maxN];
			var dR = spec[maxN + 1] / _spectrum[maxN];
			freqN += 0.5f * (dR * dR - dL * dL);
		}
		PitchValue = freqN * (_fSample / 2) / QSamples; // convert index to frequency
														//PitchValue = (Mathf.Exp(freqN / QSamples)-1) * _fSample; // convert index to frequency
		pitchIndex = maxN;
	}
}