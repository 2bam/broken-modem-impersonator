using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WordView : MonoBehaviour
{
	[SerializeField] Color _listeningColor = Color.green;
	[SerializeField] Color _recordingColor = Color.red;

	[SerializeField] Text _text;
	[SerializeField] EmissionController _emitter;
	[SerializeField] List<RectTransform> _chars;
	[SerializeField] RectTransform _highlightWord;
	[SerializeField] RectTransform _highlightChar;

	private Word _guessedWord;
	private Word _word;
	private int _index;

	private void Awake()
	{
		_index = transform.GetSiblingIndex();
		ShowCharHighlight(false);
		ShowWordHighlight(false);
	}

	public void Set(Word word)
	{
		_word = word;
		_text.text = word.Text;
	}

	public void Clear(bool keepIfCorrect)
	{
		_text.text = !keepIfCorrect || _guessedWord != _word ? "XXXXXXX" : _word.Text;
	}

	public void OnEnterWord(int index)
	{
		var isThis = index == _index;
		ShowWordHighlight(isThis);
		ShowCharHighlight(isThis);
	}

	public void OnExitWord(int index)
	{
		//if (_index != index) return;

		ShowWordHighlight(false);
		ShowCharHighlight(false);
	}

	public void OnEnterWord()
	{
		ShowWordHighlight(true);
	}

	public void OnExitWord()
	{
		ShowWordHighlight(false);
	}

	public void OnEnterChar(int index, int charIndex)
	{
		PostitionChar(charIndex);
	}

	public void OnExitChar(int index, int charIndex)
	{
		//if (_index != index) return;
		PostitionChar(charIndex);
	}

	private void ShowCharHighlight(bool v, int charIndex = 0)
	{
		if (_highlightChar == null) return;
		_highlightChar.gameObject.SetActive(v);
		PostitionChar(charIndex);
	}

	private void PostitionChar(int charIndex)
	{
		if (_chars == null || charIndex >= _chars.Count) return;

		var y = _highlightChar.anchoredPosition.y;
		var newPos = _chars[charIndex].anchoredPosition;
		newPos.y = y;
		_highlightChar.anchoredPosition = newPos;
	}

	public void ShowWordHighlight(bool v)
	{
		_highlightWord.gameObject.SetActive(v);
	}

	public void OnReceiveWord(Word word)
	{
		_guessedWord = word;
		_text.color = (word == _word) ? Color.green : Color.red;
		_text.text = word.Text;
	}

	public void SetMode(Mode mode)
	{
		_text.color = Color.grey;
		_highlightWord.GetComponent<Image>().color = mode == Mode.Recording ? _recordingColor : _listeningColor;
	}
}
