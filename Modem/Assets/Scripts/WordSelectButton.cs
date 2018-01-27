using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordSelectButton : MonoBehaviour {
	public Word word;
	WordSelectPanel _panel;

	public void Setup(WordSelectPanel panel, Word word)
	{
		_panel = panel;
		this.word = word;	
	}

	public void OnClick() {
		_panel.OnWordClick(word);
	}
}
