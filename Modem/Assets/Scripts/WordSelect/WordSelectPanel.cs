using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class WordSelectPanel : MonoBehaviour {
	public GameObject categoryPrefab;
	public WordSelectButton wordButtonPrefab;
	public Text txtPhrase;
	public Vector2 spacing;

	// Use this for initialization
	IEnumerator Start () {
		Clear();

		var panelXf = transform as RectTransform;
		var buttons = new List<WordSelectButton>();

		var wordsByCat = AppData.Instance.AvailableWords
			.OrderBy(w => w.Category)
			.ThenBy(w => w.Text)
			;

		foreach (var word in wordsByCat) {
			var copy = Instantiate(wordButtonPrefab);
			copy.transform.SetParent(panelXf, false);
			copy.Setup(this, word);
			var b = copy.GetComponent<WordSelectButton>();
			var t = b.GetComponentInChildren<Text>();
			t.text = word.Text;//Enumerable.Repeat("A", Random.Range(5, 10)).Aggregate("", (a, x) => a + x);
			buttons.Add(b);
		}

		yield return null;	//Delay for rects to work :rolleyes:

		
		var panelWidth = panelXf.rect.width;
		var pos = panelXf.position;
		string lastCat = null;
		foreach (var b in buttons) {
			var t = b.GetComponentInChildren<Text>();
			var lo = b.GetComponent<LayoutGroup>();
			var brt = b.transform as RectTransform;
			var yMove = -brt.rect.height * brt.localScale.y;

			if (b.word.Category != lastCat)
			{
				//Add category label
				lastCat = b.word.Category;
				var catCopy = Instantiate(categoryPrefab);
				catCopy.transform.SetParent(panelXf, false);
				if (pos.x != 0)
				{
					pos.x = 0;
					pos.y += yMove;
				}
				catCopy.transform.position = pos;
				catCopy.GetComponent<Text>().text = b.word.Category;
				pos.y -= (catCopy.transform as RectTransform).rect.height * catCopy.transform.localScale.y;
			}

			var xMove = brt.rect.width * brt.localScale.x;
			if (pos.x != 0 && pos.x + xMove > panelWidth)
			{
				pos.x = 0;
				pos.y += yMove;
			}

			brt.position = pos;
			pos.x += xMove + spacing.x;
		}
	}

	public void RefreshText()
	{
		Debug.Assert(AppData.Instance.SelectedWords != null);
		txtPhrase.text = AppData.Instance.SelectedWords
			.Aggregate("", (a, x) => a + x.Text + " ");
	}

	public void Clear()
	{
		AppData.Instance.SelectedWords = new List<Word>();
		RefreshText();
	}

	public void Done()
	{
		//TODO
	}

	public void OnWordClick(Word word)
	{
		AppData.Instance.SelectedWords.Add(word);
		RefreshText();
	}

}
