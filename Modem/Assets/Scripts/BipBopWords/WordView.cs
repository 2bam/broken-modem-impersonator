using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordView : MonoBehaviour
{
	[SerializeField] EmissionController _emitter;
	[SerializeField] List<RectTransform> _chars;
	[SerializeField] RectTransform _highlightWord;
	[SerializeField] RectTransform _highlightChar;

	private int _index;

	private void OnEnable()
	{
		if (_emitter == null) _emitter = FindObjectOfType<EmissionController>();

		_emitter.OnEnterWord += OnEnterWord;
		_emitter.OnExitWord  += OnExitWord;
		_emitter.OnEnterChar += OnEnterChar;
		_emitter.OnExitChar  += OnExitChar;
	}

	private void OnDisable()
	{
		_emitter.OnEnterWord -= OnEnterWord;
		_emitter.OnExitWord  -= OnExitWord;
		_emitter.OnEnterChar -= OnEnterChar;
		_emitter.OnExitChar  -= OnExitChar;
	}

	private void Awake()
	{
		_index = transform.GetSiblingIndex();
		ShowCharHighlight(false);
		ShowWordHighlight(_index == 0);
	}

	public void OnEnterWord(int index)
	{
		var isThis = index == _index;
		ShowWordHighlight(isThis);
		ShowCharHighlight(isThis);
	}

	public void OnExitWord(int index)
	{
		if (_index != index) return;

		ShowWordHighlight(false);
		ShowCharHighlight(false);
	}

	public void OnEnterChar(int index, int charIndex)
	{
		PostitionChar(charIndex);
	}

	public void OnExitChar(int index, int charIndex)
	{
		if (_index != index) return;
		PostitionChar(charIndex);
	}

	private void ShowCharHighlight(bool v, int charIndex = 0)
	{
		_highlightChar.gameObject.SetActive(v);
		PostitionChar(charIndex);
	}

	private void PostitionChar(int charIndex)
	{
		var y = _highlightChar.anchoredPosition.y;
		var newPos = _chars[charIndex].anchoredPosition;
		newPos.y = y;
		_highlightChar.anchoredPosition = newPos;
	}

	private void ShowWordHighlight(bool v)
	{
		_highlightWord.gameObject.SetActive(v);
	}
}
