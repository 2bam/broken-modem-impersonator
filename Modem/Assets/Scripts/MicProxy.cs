using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MicProxy : MonoBehaviour
{
	public bool forceWin;
	public bool microphoneEnabled;
	public bool simulateInput;
	public EmissionController emissionController;

	public Image[] soundCharImages;
	public Color[] soundCharImagesColoring;

	public void ForceFeed()
	{
		forceWin = false;
		simulateInput = true;
	}

	public void ForceFeedWin()
	{
		forceWin = true;
		simulateInput = true;
	}

	public void OnEndChar(int currentIndex)
	{
		if (microphoneEnabled) emissionController.OnBeginChar(currentIndex);

		foreach (var img in soundCharImages)
			img.color = Color.gray;
	}

	public void OnBeginChar(SoundChars current, int currentIndex)
	{
		//Debug.Log("OnBeginChar " + current.ToString() + " index=" + currentIndex);   //delete this line, eventually

		foreach (var img in soundCharImages)
			img.color = Color.gray;
		int index = (int)current;
		if (0 <= index && index < soundCharImages.Length)
		{
			soundCharImages[index].color = soundCharImagesColoring[index];
		}
	}

	public void SetInstantSoundingChar(SoundChars current)
	{
		//foreach (var img in soundCharImages)
		//	img.color = Color.gray;
		//int index = (int)current;
		//if (0 <= index && index < soundCharImages.Length)
		//	soundCharImages[index].color = soundCharImagesColoring[index];
	}

	public void Feed(Word word)
	{
		Debug.Log("FEED " + word.Text + " (" + microphoneEnabled + ")");
		if(microphoneEnabled)
			emissionController.Feed(word);
	}

	// Use this for initialization
	IEnumerator Start () {
		Debug.Assert(soundCharImages.Length == soundCharImagesColoring.Length);
		SetInstantSoundingChar(SoundChars.Silence);
		while (true)
		{
			//Debug test, press T to feed random words at random times to the selected word amount.
			yield return new WaitUntil(()=> (Input.GetKeyDown(KeyCode.T) || simulateInput) && microphoneEnabled);
			simulateInput = false;

			if (forceWin)
			{
				for (int i = 0; i < AppData.Instance.SelectedWords.Count; i++)
				{
					emissionController.Feed(AppData.Instance.SelectedWords[i]);
					yield return new WaitForSeconds(Random.Range(1f, 4f));
				}
				continue;
			}

			for (int i = 0; i < AppData.Instance.SelectedWords.Count; i++)
			{
				emissionController.Feed(Utility.Choice(AppData.Instance.AvailableWords));
				yield return new WaitForSeconds(Random.Range(1f, 4f));
			}
		}
	}
}
