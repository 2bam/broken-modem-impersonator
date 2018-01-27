using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mic : MonoBehaviour
{
	public bool forceWin;
	public bool microphoneEnabled;
	public EmissionController emissionController;

	// Use this for initialization
	IEnumerator Start () {
		while (true)
		{
			//Debug test, press T to feed random words at random times to the selected word amount.
			yield return new WaitUntil(()=>Input.GetKeyDown(KeyCode.T) && microphoneEnabled);

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
