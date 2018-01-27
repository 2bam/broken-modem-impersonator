using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum Mode
{
	Playing,
	Recording
}

public class EmissionController : MonoBehaviour
{
	[SerializeField] InputField _input;
	[SerializeField] Button _button;

	[SerializeField] float _seconds;
	[SerializeField] float _spacingSeconds; 
	[SerializeField] BipBopAudio _audioPlayer;

	[SerializeField] RectTransform _wordsContainer;
	[SerializeField] GameObject _wordPrefab;

	List<Word> _currentWords;
	List<WordView> _views = new List<WordView>(10);

	SoundChars[] _bipBopValues = new SoundChars[Word.MAX_DIGITS];
	Coroutine _wordEmission;

	int _listeningWordsIndex;
	Mic _microphone;

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

	private void Start()
	{
		if (_microphone == null) _microphone = FindObjectOfType<Mic>();

		// Read this from AvailableWords.
		_currentWords = AppData.Instance.SelectedWords;

		GenerateWordViews(_currentWords);
	}

	private void GenerateWordViews(List<Word> words)
	{
		foreach(var word in words)
		{
			var wordView = Instantiate(_wordPrefab, _wordsContainer, false).GetComponent<WordView>();
			wordView.Set(word);
			_views.Add(wordView);
		}

		ClearWords(false);
	}

	private void Click()
	{
		PlayAudio();
	}

	void StopEmitting()
	{
		if (_wordEmission != null) StopCoroutine(_wordEmission);
		_audioPlayer.Stop();
	}

	IEnumerator EmitWords(List<Word> words)
	{
		for (var i = 0; i < words.Count; i++)
		{
			var wordValue = words[i].Id;

			print("Entered word: " + i);
			_views[i].OnEnterWord();

			if (!Word.UpdateBipBopValues(wordValue, Word.BASE, Word.MAX_DIGITS, _bipBopValues))
			{
				Debug.LogFormat("Can't play: {0} is over the max supported value.", wordValue);
				_wordEmission = null;
				yield break;
			}

			for (var j = 0; j < _bipBopValues.Length; j++)
			{
				var sound = (int)_bipBopValues[j];
				_audioPlayer.Play(sound);

				yield return new WaitForSeconds(_seconds);
			}

			print("Exit word: " + i);
			_views[i].OnExitWord();
		}

		Record();
	}

	void PlayAudio()
	{
		_microphone.microphoneEnabled = false;
		_button.interactable = false;

		StopEmitting();
		_wordEmission = StartCoroutine(EmitWords(_currentWords));

		SetMode(Mode.Playing);
		ClearWords(true);
	}

	void ClearWords(bool keepIfCorrect)
	{
		_views.ForEach(v => v.Clear(keepIfCorrect));
	}

	void SetMode(Mode mode)
	{
		_views.ForEach(v => v.SetMode(mode));
	}

	void Record()
	{
		_microphone.microphoneEnabled = true;
		SetMode(Mode.Recording);
		_views[0].ShowWordHighlight(true);
	}

	public void Feed(Word word)
	{
		// Receive word by word.
		// Check if it's the same word.
		if (word == _currentWords[_listeningWordsIndex]) print("Correct");
		else print("Wrong");

		_views[_listeningWordsIndex].OnReceiveWord(word);

		_views[_listeningWordsIndex].ShowWordHighlight(false);
		_listeningWordsIndex++;

		if(_listeningWordsIndex >= _currentWords.Count)
		{
			// End.
			_listeningWordsIndex = 0;
			_views[_listeningWordsIndex].ShowWordHighlight(false);
			_button.interactable = true;
		}
		else
		{
			_views[_listeningWordsIndex].ShowWordHighlight(true);
		}
	}
}
