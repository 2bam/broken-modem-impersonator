using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
	[SerializeField] string _sceneName;
	[SerializeField] int _screenInt;
	
	public void LoadName()
	{
		SceneManager.LoadScene(_sceneName);
	}

	public void LoadIndex()
	{
		SceneManager.LoadScene(_screenInt);
	}
}
