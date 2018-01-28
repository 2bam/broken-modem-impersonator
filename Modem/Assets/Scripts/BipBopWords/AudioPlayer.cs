using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
	public AudioSource _source;
	
	[Header("SFX")]
	public AudioClip _winClip;
	public AudioClip _loseClip;
	[Tooltip("Match order of bip, bop, grr, gni to order in SoundChars")]
	public List<AudioClip> _sounds;

	private void Awake()
	{
		if (_source == null) return;
		_source.loop = false;
	}

	public void PlayEndSound(bool isWin)
	{
		Stop();
		_source.PlayOneShot(isWin ? _winClip : _loseClip);
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
