using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class AppData : Singleton<AppData>
{
	[SerializeField] private TextAsset _wordsDefinition;
	public List<Word> AvailableWords { get; private set; }
	public List<Word> SelectedWords;

	void Awake()
	{
		List<string> words = new List<string>();

		using (var reader = new StringReader(_wordsDefinition.text))
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if(line.StartsWith("-- "))
				{
					var newline = line.Substring(3);
					// Add to different categories until finding a new category.
				}
				else words.Add(line);
			}
		}

		AvailableWords = new List<Word>(words.Count);
		for (var i = 0; i < words.Count; i++)
		{
			AvailableWords.Add(new Word(words[i], i));
		}

		SelectedWords = AvailableWords
			.OrderBy(x => Random.value)
			.Take(4)
			.ToList();
	}
}
