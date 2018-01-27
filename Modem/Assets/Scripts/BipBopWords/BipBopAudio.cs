using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BipBopAudio : MonoBehaviour
{
	public AudioSource _source;
	[Tooltip("Match order of bip, bop, grr, gni to order in SoundChars")]
	public List<AudioClip> _sounds;
	private void Awake()
	{
		if (_source == null) return;
		_source.loop = false;
	}

	public void Play(int sound)
	{
		if (_source == null || _sounds == null || sound >= _sounds.Count) return;

		Stop();
		_source.clip = _sounds[sound];
		_source.Play();

		print("SFX: " + ((SoundChars)sound).ToString());
	}

	public void Stop()
	{
		if (_source == null) return;

		_source.Stop();
		_source.clip = null;
	}
}
