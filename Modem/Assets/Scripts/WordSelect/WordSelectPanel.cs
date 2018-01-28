using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class WordSelectPanel : MonoBehaviour
{
	public RectTransform container;
	public GameObject categoryPrefab;
	public WordSelectButton wordButtonPrefab;
	public Text txtPhrase;
	public Vector2 spacing;

	public Button buttonDone;
	//Should the word be shown as "???" (Same size of SelectedWords)
	public List<bool> selectedHidden;

	// Use this for initialization
	IEnumerator Start () {
		Clear();

		var buttons = new List<WordSelectButton>();

		var wordsByCat = AppData.Instance.AvailableWords
			.OrderBy(w => w.Category)
			.ThenBy(w => w.Text);

		foreach (var word in wordsByCat)
		{
			var copy = Instantiate(wordButtonPrefab);
			copy.transform.SetParent(container, false);
			copy.Setup(this, word);
			var b = copy.GetComponent<WordSelectButton>();
			var t = b.GetComponentInChildren<Text>();
			t.text = word.Text;//Enumerable.Repeat("A", Random.Range(5, 10)).Aggregate("", (a, x) => a + x);
			buttons.Add(b);
		}

		yield return null;  //Delay for rects to work :rolleyes:


		var accumulatedHeight = 0f;
		var headersSpacing = 20f;

		var panelWidth = container.rect.width;
		var pos = Vector2.zero;
		string lastCat = null;
		foreach (var b in buttons)
		{
			var t = b.GetComponentInChildren<Text>();
			var brt = b.transform as RectTransform;
			var yMove = -brt.rect.height;

			if (b.word.Category != lastCat)
			{
				//Add category label
				lastCat = b.word.Category;

				var catCopy = Instantiate(categoryPrefab, container, false).transform as RectTransform;
				if (pos.x != 0)
				{
					pos.x = 0;
					pos.y -= catCopy.rect.height;
				}

				catCopy.anchoredPosition = pos;
				catCopy.GetComponent<Text>().text = b.word.Category;
				pos.y -= catCopy.rect.height - headersSpacing;
			}

			var xMove = brt.rect.width;
			if (pos.x != 0 && pos.x + xMove > panelWidth)
			{
				pos.x = 0;
				pos.y += yMove + spacing.y;
			}

			brt.anchoredPosition = pos;
			pos.x += xMove + spacing.x;

			accumulatedHeight = pos.y - brt.rect.height;
		}

		container.sizeDelta = new Vector2(container.sizeDelta.x, Mathf.Abs(accumulatedHeight));
	}

	public void RefreshText()
	{
		Debug.Assert(AppData.Instance.SelectedWords != null);
		txtPhrase.text = AppData.Instance.SelectedWords
			.Zip(selectedHidden, (w, m) => m ? "  (???)  " : w.Text)
			.Aggregate("", (a, x) => a + x + " ");
	}

	public void Clear()
	{
		AppData.Instance.SelectedWords = new List<Word>();
		selectedHidden = new List<bool>();
		buttonDone.interactable = false;
		RefreshText();
	}

	public void Done()
	{
		//TODO: Grey out button, for now just don't work.
		if(AppData.Instance.SelectedWords.Any())
			SceneManager.LoadScene(1);
	}

	public void AddRandomMisteryWord() {
		AppData.Instance.SelectedWords.Add(Utility.Choice(AppData.Instance.AvailableWords));
		selectedHidden.Add(true);
		buttonDone.interactable = true;
		RefreshText();
	}

	public void OnWordClick(Word word)
	{
		AppData.Instance.SelectedWords.Add(word);
		selectedHidden.Add(false);
		buttonDone.interactable = true;
		RefreshText();
	}

}
