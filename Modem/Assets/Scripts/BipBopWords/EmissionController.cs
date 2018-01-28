using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Mode
{
	Playing,
	Recording
}

public class EmissionController : MonoBehaviour
{
	[SerializeField] Button _emitButton;
	[SerializeField] Button _defeatButton;

	[Header("Emission")]
	[SerializeField] float _seconds;
	[SerializeField] float _spacingSeconds;
	[SerializeField] AudioPlayer _audioPlayer;

	[Header("Words Display")]
	[SerializeField] RectTransform _wordsContainer;
	[SerializeField] GameObject _wordPrefab;

	[Header("Ending")]
	[SerializeField] float _waitBeforeEnd;

	List<Word> _currentWords;
	List<WordView> _views = new List<WordView>(10);

	SoundChars[] _bipBopValues = new SoundChars[Word.MAX_DIGITS];
	Coroutine _wordEmission;

	int _listeningWordsIndex;
	MicProxy _microphone;

	int _matchCount;
	bool _ended;

	//public event Action<int> OnEnterWord;
	//public event Action<int> OnExitWord;
	//public event Action<int, int> OnEnterChar;
	//public event Action<int, int> OnExitChar;


	private void OnEnable()
	{
		_defeatButton.onClick.AddListener(Defeat);
		_emitButton.onClick.AddListener(Emit);
	}

	private void OnDisable()
	{
		_defeatButton.onClick.RemoveListener(Defeat);
		_emitButton.onClick.RemoveListener(Emit);
	}

	private void Start()
	{
		if (_microphone == null) _microphone = FindObjectOfType<MicProxy>();

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

	private void Emit()
	{
		PlayAudio();
	}

	private void Win()
	{
		_ended = true;
		BlockUI();
		StartCoroutine(GoToEnd(true));
	}

	private void Defeat()
	{
		_ended = true;
		print("<color=red>Someone cut the cable... loser!</color>");

		ResetRecording();
		StopEmitting();
		RevealAnswer();
		BlockUI();

		StartCoroutine(GoToEnd(false));
	}

	private void BlockUI()
	{
		_emitButton.interactable = false;
		_defeatButton.interactable = false;
	}

	private IEnumerator GoToEnd(bool isWin)
	{
		_audioPlayer.PlayEndSound(isWin);
		yield return new WaitForSeconds(_waitBeforeEnd);
		SceneManager.LoadScene(isWin ? "EndWin" : "EndLose");
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
				_views[i].OnEnterChar(i, j);
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
		_emitButton.interactable = false;

		StopEmitting();
		_wordEmission = StartCoroutine(EmitWords(_currentWords));

		SetMode(Mode.Playing);
		ClearWords(true);
		SetEmitButton("Emitting...");
	}

	void RevealAnswer()
	{
		_views.ForEach(v =>
		{
			v.ShowWordHighlight(false);
			v.Reveal();
		});
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
		SetEmitButton("Listening...");
	}

	public void Feed(Word word)
	{
		if (_ended) return;

		// Receive word by word.
		// Check if it's the same word.
		if (word == _currentWords[_listeningWordsIndex]) _matchCount++;

		_views[_listeningWordsIndex].OnReceiveWord(word);

		_views[_listeningWordsIndex].ShowWordHighlight(false);
		_listeningWordsIndex++;

		if(_listeningWordsIndex >= _currentWords.Count)
		{
			// End.
			if (_matchCount == _currentWords.Count) Win();
			ResetRecording();
		}
		else
		{
			_views[_listeningWordsIndex].ShowWordHighlight(true);
		}
	}

	private void ResetRecording()
	{
		_listeningWordsIndex = 0;
		_matchCount = 0;

		_views[_listeningWordsIndex].ShowWordHighlight(false);
		_emitButton.interactable = !_ended && (_matchCount != _currentWords.Count);
		_microphone.microphoneEnabled = false;

		SetEmitButton("EMIT SOUND");
	}

	private void SetEmitButton(string text)
	{
		_emitButton.GetComponentInChildren<Text>().text = text;
	}

	public void OnBeginChar(int index)
	{
		_views[_listeningWordsIndex].OnEnterChar(_listeningWordsIndex, index);
	}
}
