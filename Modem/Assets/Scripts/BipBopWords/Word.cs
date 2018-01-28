using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum SoundChars
{
	Bip = 0,
	Bop = 1,
	Rrr = 2,

	Silence = 1000
}

public class Word
{
	public const int BASE = 2;
	public const int MAX_DIGITS = 6;

	public string Text { get; private set; }
	public string Category { get; private set; }
	public int Id { get; private set; }
	public SoundChars[] BipBopValues { get; private set; }

	public Word(string wordText, int id, string category)
	{
		Text = wordText;
		Category = category;
		Id = id;

		if (BipBopValues == null) BipBopValues = new SoundChars[MAX_DIGITS];
		UpdateBipBopValues(Id, BASE, MAX_DIGITS, BipBopValues);
	}

	public static int[] IntToBipBop(int @decimal, int @base, int maxDigits)
	{
		int[] values = new int[maxDigits];

		if (@decimal > Mathf.Pow(@base, maxDigits))
		{
			Debug.LogError("Value would require more digits than allowed.");
			return null;
		}

		// Clear to zeroes.
		for (var i = 0; i < values.Length; i++) values[i] = 0;

		if (@decimal <= 0) return values;

		// Fill from right to left.
		var currentDigit = maxDigits - 1;
		while (@decimal > 0)
		{
			int digit = @decimal % @base;
			values[currentDigit] = digit;
			@decimal = @decimal / @base;
			currentDigit--;
		}

		return values;
	}

	public static bool UpdateBipBopValues(int @decimal, int @base, int maxDigits, SoundChars[] values)
	{
		var delta =  @decimal - Mathf.Pow(@base, maxDigits) + 1;
		if (delta > 0)
		{
			Debug.LogError("Value would require more digits than allowed. Over by " + delta);
			return false;
		}

		// Clear to zeroes.
		for (var i = 0; i < values.Length; i++) values[i] = 0;

		if (@decimal <= 0) return true;

		// Fill from right to left.
		var currentDigit = maxDigits - 1;
		while (@decimal > 0)
		{
			int digit = @decimal % @base;
			values[currentDigit]= (SoundChars) digit;
			@decimal = @decimal / @base;
			currentDigit--;
		}

		return true;
	}
}
