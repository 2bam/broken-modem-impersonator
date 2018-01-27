using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mic : MonoBehaviour {

	public bool microphoneEnabled;
	public EmissionController emissionController;

	T Choice<T>(IList<T> v)
	{
		return v[Random.Range(0, v.Count)];
	}
	// Use this for initialization
	IEnumerator Start () {
		while (true)
		{
			//Debug test, press T to feed random words at random times to the selected word amount.
			yield return new WaitUntil(()=>Input.GetKeyDown("T") && microphoneEnabled);
			for (int i = 0; i < AppData.Instance.SelectedWords.Count; i++)
			{
				//emissionController.Feed(Choice(AppData.Instance.AvailableWords));
				yield return new WaitForSeconds(Random.Range(1f, 4f));
			}
		}
	}
	
	// Update is called once per frame
	void Update () {

	}
}
