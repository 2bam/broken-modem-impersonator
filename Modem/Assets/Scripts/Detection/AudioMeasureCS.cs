using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioMeasureCS : MonoBehaviour
{
	public float RmsValue;
	public float DbValue;
	public float PitchValueX;
	public float PitchValueY;
	public int pitchIndex;

	public int QSamples = 2048;//512;
	private const float RefValue = 0.1f;
	private const float Threshold = 0.000001f;//0.02f;

	public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

	//RR = 0.032 - 0.040s
	float[] _samples;
	private float[] _spectrum;
	private float[] _spectrumLerped;
	private float _freqSampleRate;

	public float spectrumLerpFactor;

	[Range(8, 15)]
	public int pow2samples = 12;
	public AnimationCurve spectrumMap = AnimationCurve.Linear(0, 0, 1, 1);

	AudioSource _asrc;

	void OnGUI() {
		IMGUI.Begin(this, "AudioMeasureCS");
			if (GUILayout.Button("Press Me"))
				Debug.Log("Hello!");
		IMGUI.End();
    }

	void Start()
	{
		QSamples = (int)Mathf.Pow(2f, pow2samples);
		
		_samples = new float[QSamples];
		_spectrum = new float[QSamples];
		_spectrumLerped = new float[QSamples];

		_freqSampleRate = AudioSettings.outputSampleRate;
		_asrc = GetComponent<AudioSource>();

		Debug.Log("Output sample rate=" + _freqSampleRate);
		Debug.Log("Mic sample rate=" + _freqSampleRate);
		Debug.Log("Spectrum bins=" + QSamples);
		Debug.Log("fixed dt=" + Time.fixedDeltaTime);
	}

	void FixedUpdate()
	{
		AnalyzeSound();
	}


	void DrawData()
	{
		if (!Application.isEditor)
			return;

		var offset = Vector3.up * 50f;
		var offset2 = Vector3.up * 350f;
		var scale = new Vector2(100f, 10000f);

		float maxV1=0f, maxV2=0f;
		Vector3 maxP1, maxP2;
		maxP1 = maxP2 = Vector3.zero;
		var spectrum = _spectrum;
		for (var i = 1; i < spectrum.Length - 1; i++)
		{
			if (spectrum[i] > maxV1)
			{
				maxV1 = spectrum[i];
				maxP1 = new Vector3(Mathf.Log(i) * scale.x, spectrum[i] * scale.y, 1);
			}
			if (_spectrumLerped[i] > maxV2)
			{
				maxV2 = _spectrumLerped[i];
				maxP2 = new Vector3(Mathf.Log(i) * scale.x, _spectrumLerped[i] * scale.y, 1);
			}
			

			//Debug.DrawLine(offset + new Vector3(i - 1, spectrum[i] + 10, 0), offset + new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
			//Debug.DrawLine(offset + new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), offset + new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
			//Debug.DrawLine(offset + new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), offset + new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
			Debug.DrawLine(
				offset + new Vector3(
					Mathf.Log(i-1) * scale.x
					, spectrum[i - 1] * scale.y
					)
				, offset + new Vector3(
					Mathf.Log(i) * scale.x
					, spectrum[i] * scale.y
					)
				, Color.green
				);
			Debug.DrawLine(
				offset2 + new Vector3(
					Mathf.Log(i-1) * scale.x
					, _spectrumLerped[i - 1] * scale.y
					)
				, offset2 + new Vector3(
					Mathf.Log(i) * scale.x
					, _spectrumLerped[i] * scale.y
					)
				, Color.magenta
				);
		}
		Debug.DrawLine(offset + maxP1 + Vector3.up * 100f, offset + maxP1 + Vector3.down * 100f, Color.green);
		Debug.DrawLine(offset2 + maxP2 + Vector3.up * 100f, offset2 + maxP2 + Vector3.down * 100f, Color.magenta);

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
		_asrc.GetSpectrumData(_spectrum, 0, fftWindow);
		//_asrc.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
		// _asrc.GetSpectrumData(_spectrum, 0, FFTWindow.Hamming);

		//Patch data from hearing curve and accumulate with interpolation.
		var t = spectrumLerpFactor * Time.fixedDeltaTime;
		for (int i = 0; i < _spectrum.Length; i++) {
			_spectrum[i] = spectrumMap.Evaluate((float)i / QSamples * _freqSampleRate / 2) * _spectrum[i];
			_spectrumLerped[i] = Mathf.Lerp(_spectrumLerped[i], _spectrum[i], t);
		}

		DrawData();

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
		//if(maxV > 0)
		//	Debug.Log("maxV=" + maxV);
		float freqN = maxN; // pass the index to a float variable
		PitchValueY = maxV;
		if (maxN > 0 && maxN < QSamples - 1)
		{ // interpolate index using neighbours
			//var dL = spec[maxN - 1] / _spectrum[maxN];
			//var dR = spec[maxN + 1] / _spectrum[maxN];
			//freqN += 0.5f * (dR * dR - dL * dL);
			var dL = spec[maxN - 1] / (spec[maxN]+1f);
			var dR = spec[maxN + 1] / (spec[maxN]+1f);
			freqN += 0.5f * (dR * dR - dL * dL);
			PitchValueY += (dR * dR - dL * dL);
		}

		//TODO: Make linear instead of log?
		PitchValueY = Mathf.Max(-160f, 20 * Mathf.Log10(PitchValueY / RefValue)); // calculate dB
		PitchValueX = freqN * (_freqSampleRate / 2) / QSamples; // convert index to frequency
														//PitchValue = (Mathf.Exp(freqN / QSamples)-1) * _fSample; // convert index to frequency
		pitchIndex = maxN;
	}
}