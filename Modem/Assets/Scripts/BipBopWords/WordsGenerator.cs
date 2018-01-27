using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WordsGenerator : MonoBehaviour
{
	public int debugIndex;

	[SerializeField] private TextAsset _wordsDefinition;
	public List<BipBopWord> AvailableWords { get; private set; }

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

		AvailableWords = new List<BipBopWord>(words.Count);
		for (var i = 0; i < words.Count; i++)
		{
			AvailableWords.Add(new BipBopWord(words[i], i));
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
		{
			if (debugIndex > AvailableWords.Count) return;
			print("<color=white>------------------------------------</color>");
			print("Int: " + debugIndex + " Word: " + AvailableWords[debugIndex].Word);
			foreach(var b in AvailableWords[debugIndex].BipBopValues)
			{
				print(((SoundChars)b).ToString());
			}
		}
	}
}
