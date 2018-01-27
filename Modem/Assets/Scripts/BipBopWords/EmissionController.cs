using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmissionController : MonoBehaviour
{
	[SerializeField] InputField _input;
	[SerializeField] Button _button;

	[SerializeField] float _seconds;
	[SerializeField] float _spacingSeconds; 
	[SerializeField] BipBopAudio _audioPlayer;

	[Header("Debug")]
	public bool PlayTestWords;
	public List<int> testWords = new List<int>() { 1, 2, 5, 4 };
	[SerializeField] RectTransform _wordsContainer;
	[SerializeField] GameObject _wordPrefab;

	SoundChars[] _bipBopValues = new SoundChars[Word.MAX_DIGITS];
	Coroutine _wordEmission;

	public event Action<int> OnEnterWord;
	public event Action<int> OnExitWord;
	public event Action<int, int> OnEnterChar;
	public event Action<int, int> OnExitChar;


	private void OnEnable()
	{
		_button.onClick.AddListener(Click);
	}

	private void OnDisable()
	{
		_button.onClick.RemoveListener(Click);
	}

	private void Awake()
	{
		GenerateWordViews(testWords);
	}

	private void GenerateWordViews(List<int> words)
	{
		foreach(var word in words)
		{
			var wordView = Instantiate(_wordPrefab, _wordsContainer, false);
		}
	}

	private void Click()
	{
		StopEmitting();
		_wordEmission = PlayTestWords ? StartCoroutine(EmitWords(testWords)) : StartCoroutine(EmitWord());
	}

	void StopEmitting()
	{
		if (_wordEmission != null) StopCoroutine(_wordEmission);
		_audioPlayer.Stop();
	}

	IEnumerator EmitWords(List<int> wordIndices)
	{
		for (var i = 0; i < wordIndices.Count; i++)
		{
			var wordValue = wordIndices[i];
			if (OnEnterWord != null)
			{
				print("Entered word: " + i);
				OnEnterWord(i);
			}

			if (!Word.UpdateBipBopValues(wordValue, Word.BASE, Word.MAX_DIGITS, _bipBopValues))
			{
				Debug.LogFormat("Can't play: {0} is over the max supported value.", wordValue);
				_wordEmission = null;
				yield break;
			}

			for (var j = 0; j < _bipBopValues.Length; j++)
			{
				var sound = (int)_bipBopValues[j];

				if (OnEnterChar != null)
				{
					print("Entered char: " + j);
					OnEnterChar(wordValue, j);
				}

				_audioPlayer.Play(sound);

				yield return new WaitForSeconds(_seconds);
			}

			if (OnExitWord != null)
			{
				print("Exit word: " + i);
				OnExitWord(i);
			}
		}
	}

	IEnumerator EmitWord()
	{
		var index = int.Parse(_input.text);

		if (!Word.UpdateBipBopValues(index, Word.BASE, Word.MAX_DIGITS, _bipBopValues))
		{
			Debug.LogFormat("Can't play: {0} is over the max supported value.", index);
			_wordEmission = null;
			yield break;
		}

		Debug.LogFormat("Playing: {0}", index);
		for (var i = 0; i < _bipBopValues.Length; i++)
		{
			var sound = (int) _bipBopValues[i];
			_audioPlayer.Play(sound);
			yield return new WaitForSeconds(_seconds);
		}

		_wordEmission = null;
	}
}
