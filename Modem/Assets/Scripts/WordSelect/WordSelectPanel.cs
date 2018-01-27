using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class WordSelectPanel : MonoBehaviour {
	public WordSelectButton wordButtonPrefab;
	public Text txtPhrase;
	public Vector2 spacing;

	// Use this for initialization
	IEnumerator Start () {
		Clear();

		var panelXf = transform as RectTransform;
		var buttons = new List<Button>();
		foreach(var word in AppData.Instance.AvailableWords) {
			var copy = Instantiate(wordButtonPrefab);
			copy.transform.SetParent(panelXf, false);
			copy.Setup(this, word);
			var b = copy.GetComponent<Button>();
			var t = b.GetComponentInChildren<Text>();
			t.text = word.Text;//Enumerable.Repeat("A", Random.Range(5, 10)).Aggregate("", (a, x) => a + x);
			buttons.Add(b);
		}

		yield return null;	//Delay for rects to work :rolleyes:

		
		var panelWidth = panelXf.rect.width;
		var pos = panelXf.position;
		foreach(var b in buttons) {
			var t = b.GetComponentInChildren<Text>();
			var lo = b.GetComponent<LayoutGroup>();
			var brt = b.transform as RectTransform;

			var btnWidth = brt.rect.width * brt.localScale.x;
			if (pos.x != 0 && pos.x + btnWidth > panelWidth)
			{
				pos.x = 0;
				pos.y -= brt.rect.height * brt.localScale.y + spacing.y;
			}

			brt.position = pos;
			pos.x += btnWidth + spacing.x;

			Debug.Log("R1 " + lo.preferredWidth);
			Debug.Log("R2 " + brt.rect.width);

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
		SceneManager.LoadScene(1);
	}

	public void OnWordClick(Word word)
	{
		AppData.Instance.SelectedWords.Add(word);
		RefreshText();
	}

}
