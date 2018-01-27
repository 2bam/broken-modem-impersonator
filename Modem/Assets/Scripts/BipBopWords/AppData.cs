using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class AppData : Singleton<AppData>
{
	//[SerializeField] private TextAsset _wordsDefinition;
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
				if (line.StartsWith("-- "))
				{
					category = line.Substring(3);
				}
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
	}
}
