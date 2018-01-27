using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum SoundChars
{
	Bip = 0,
	Bop = 1,
	Grr = 2,
	Gni = 3
}

public class BipBopWord
{
	public const int BASE = 4;
	public const int MAX_DIGITS = 3;

	public string Word { get; private set; }
	public int Id { get; private set; }
	public int[] BipBopValues { get; private set; }

	public BipBopWord(string word, int id)
	{
		Word = word;
		Id = id;

		if (BipBopValues == null) BipBopValues = new int[MAX_DIGITS];
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

	public static bool UpdateBipBopValues(int @decimal, int @base, int maxDigits, int[] values)
	{
		if (@decimal > Mathf.Pow(@base, maxDigits))
		{
			Debug.LogError("Value would require more digits than allowed.");
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
			values[currentDigit]= digit;
			@decimal = @decimal / @base;
			currentDigit--;
		}

		return true;
	}
}
