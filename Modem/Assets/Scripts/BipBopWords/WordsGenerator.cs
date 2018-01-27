using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WordsGenerator : MonoBehaviour
{
	public int debugIndex;

	[SerializeField] private TextAsset _wordsDefinition;
	private List<BipBopWord> _availableWords;

	void Awake()
	{
		List<string> words = new List<string>();

		using (var reader = new StringReader(_wordsDefinition.text))
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				words.Add(line);
			}
		}

		_availableWords = new List<BipBopWord>(words.Count);
		for (var i = 0; i < words.Count; i++)
		{
			_availableWords.Add(new BipBopWord(words[i], i));
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
		{
			if (debugIndex > _availableWords.Count) return;
			print("<color=white>------------------------------------</color>");
			print("Int: " + debugIndex + " Word: " + _availableWords[debugIndex].Word);
			foreach(var b in _availableWords[debugIndex].BipBopValues)
			{
				print(((SoundChars)b).ToString());
			}
		}
	}
}
