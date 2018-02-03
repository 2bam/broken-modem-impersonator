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
	[SerializeField] AudioPlayer _audioPlayer;

	[Header("Words Display")]
	[SerializeField] RectTransform _wordsContainer;
	[SerializeField] GameObject _wordPrefab;
	[SerializeField] float _wordCharSize = 100f;

	[Header("Ending")]
	[SerializeField] float _waitBeforeEnd;

	// Game State
	List<Word> _currentWords;

	int _listeningWordsIndex;

	List<bool> _matchedWords;
	int _matchCount;
	bool _ended;

	// Emission control
	SoundChars[] _bipBopValues = new SoundChars[Word.MAX_DIGITS];
	Coroutine _wordEmission;

	// Mic
	MicProxy _microphone;

	// Views.
	List<WordView> _views = new List<WordView>(10);

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

		// Initialize matched words to false.
		if (_matchedWords == null) _matchedWords = new List<bool>();
		_matchedWords.Clear();

		for (var i = 0; i < _currentWords.Count; i++)
		{
			_matchedWords.Add(false);
		}

		GenerateWordViews(_currentWords);
	}

	private void GenerateWordViews(List<Word> words)
	{
		// Match size to amount of words
		var grid = _wordsContainer.GetComponent<GridLayoutGroup>();
		grid.cellSize = new Vector2(_wordCharSize * Word.MAX_DIGITS, grid.cellSize.y);

		foreach (var word in words)
		{
			var wordView = Instantiate(_wordPrefab, _wordsContainer, false).GetComponent<WordView>();
			wordView.Set(word);
			_views.Add(wordView);
		}

		ClearWords(false);
	}

	private void Defeat()
	{
		StopEmitting();
		ResetRecording(false);

		RevealAnswer();

		GameEnded(false);
	}

	private void GameEnded(bool isWin)
	{
		_ended = true;

		// Block UI
		_emitButton.interactable = false;
		_defeatButton.interactable = false;

		StartCoroutine(GoToEnd(isWin));
	}

	private IEnumerator GoToEnd(bool isWin)
	{
		_audioPlayer.PlayEndSound(isWin);
		yield return new WaitForSeconds(_waitBeforeEnd);
		SceneManager.LoadScene(isWin ? "EndWin" : "EndLose");
	}

	#region Playing

	private void Emit()
	{
		// Playing sound for the player.
		_microphone.microphoneEnabled = false;
		_emitButton.interactable = false;

		StopEmitting();
		_wordEmission = StartCoroutine(EmitWords(_currentWords));

		SetMode(Mode.Playing);
		SetEmitButton("Emitting...");
	}

	private void StopEmitting()
	{
		if (_wordEmission != null) StopCoroutine(_wordEmission);
		_audioPlayer.Stop();
	}

	private IEnumerator EmitWords(List<Word> words)
	{
		ClearWords(true);

		for (var i = 0; i < words.Count; i++)
		{
			// Skip already matched words.
			if (_matchedWords[i])
			{
				_views[i].OnExitWord();
				continue;
			}

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
				var sound = (int) _bipBopValues[j];
				_audioPlayer.Play(sound);
				_views[i].OnEnterChar(i, j);
				yield return new WaitForSeconds(_seconds);
			}

			print("Exit word: " + i);
			_views[i].OnExitWord();
		}

		StartListening();

		_wordEmission = null;
	}

	#endregion

	#region Listening

	public void Feed(Word word)
	{
		// Receive detected words.

		if (_ended) return;

		// Check if it's the same word we're expecting.
		if (word == _currentWords[_listeningWordsIndex])
		{
			_matchedWords[_listeningWordsIndex] = true;
			_matchCount++;
		}

		// Inform the view.
		_views[_listeningWordsIndex].OnReceiveWord(word);

		_listeningWordsIndex++;
		CheckForSkipsOrEnd();
	}

	private void StartListening()
	{
		_microphone.microphoneEnabled = true;
		SetMode(Mode.Recording);
		SetEmitButton("Listening...");

		// Skip first matches.
		CheckForSkipsOrEnd();
	}

	private void CheckForSkipsOrEnd()
	{
		// Just hightlight the word if it's the first and wasn't guessed yet.
		if(_listeningWordsIndex == 0 && !_matchedWords[0])
		{
			_views[0].OnEnterWord();
			return;
		}

		if(WinOrReset()) return;

		while (_listeningWordsIndex < _currentWords.Count && _matchedWords[_listeningWordsIndex])
		{
			// User doesn't need to utter the sounds.
			_views[_listeningWordsIndex].OnReceiveWord(_currentWords[_listeningWordsIndex]);
			_listeningWordsIndex++;
		}

		// Check if we reached the last one with the skips.
		if (WinOrReset()) return;

		// Highlight the next not matched word.
		_views[_listeningWordsIndex].OnEnterWord();
	}

	private bool WinOrReset()
	{
		if (_listeningWordsIndex < _currentWords.Count) return false;

		// End listening. Either we won or need to replay the sounds.
		if (_matchCount == _currentWords.Count)
		{
			GameEnded(true);
			return true;
		}

		ResetRecording(!_ended && (_matchCount != _currentWords.Count));
		return true;
	}

	private void ResetRecording(bool allowEmission)
	{
		_listeningWordsIndex = 0;

		// Allow re-emitting words if the player didn't get them all right.
		_emitButton.interactable = allowEmission;
		_microphone.microphoneEnabled = false;

		SetEmitButton("EMIT SOUND");
	}

	#endregion

	#region Views

	public void OnBeginChar(int index)
	{
		if (_ended) return;
		_views[_listeningWordsIndex].OnEnterChar(_listeningWordsIndex, index);
	}

	private void RevealAnswer()
	{
		_views.ForEach(v =>
		{
			v.Reveal();
		});
	}

	private void ClearWords(bool keepIfCorrect)
	{
		_views.ForEach(v => v.Clear(keepIfCorrect));
	}

	private void SetMode(Mode mode)
	{
		_views.ForEach(v => v.SetMode(mode));
	}

	private void SetEmitButton(string text)
	{
		_emitButton.GetComponentInChildren<Text>().text = text;
	}

	#endregion
}
