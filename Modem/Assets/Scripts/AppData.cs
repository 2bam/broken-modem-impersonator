using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class AppData : Singleton<AppData>
{
	public List<Word> AvailableWords { get; private set; }
	public List<Word> SelectedWords;

	void Awake()
	{
		var wordsDefinition = Resources.Load("Words") as TextAsset;
		AvailableWords = new List<Word>();
		using (var reader = new StringReader(wordsDefinition.text))
		{
			string line;
			int index = 0;
			string category = null;
			while ((line = reader.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.Length == 0)
					continue;

				if (line.StartsWith("-- "))
					category = line.Substring(3);
				else {
					if (category == null)
						throw new System.Exception("No category for word");
					AvailableWords.Add(new Word(line, index++, category));
				}
			}
		}

		SelectedWords = AvailableWords
			.OrderBy(x => Random.value)
			.Take(4)
			.ToList();

		var gap = Mathf.Pow(Word.BASE, Word.MAX_DIGITS) - AvailableWords.Count;
		if (gap != 0)
			Debug.Log("WORD AMOUNT GAP = " + gap);
	}
}
