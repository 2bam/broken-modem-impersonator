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
	public const int MAX_DIGITS = 3;

	public string Word { get; private set; }
	public int Id { get; private set; }
	public int[] BipBopValues { get; private set; }

	public BipBopWord(string word, int id)
	{
		Word = word;
		Id = id;

		if (BipBopValues == null) BipBopValues = new int[MAX_DIGITS];
		IntToBipBop(Id, 4, MAX_DIGITS, BipBopValues);
	}

	public static void IntToBipBop(int @decimal, int @base, int maxDigits, int[] values)
	{
		if (@decimal > Mathf.Pow(@base, maxDigits))
		{
			Debug.LogError("Value would require more digits than allowed.");
			return;
		}

		// Clear to zeroes.
		for (var i = 0; i < values.Length; i++) values[i] = 0;

		if (@decimal <= 0) return;

		// Fill from right to left.
		var currentDigit = maxDigits - 1;
		while (@decimal > 0)
		{
			int digit = @decimal % @base;
			values[currentDigit]= digit;
			@decimal = @decimal / @base;
			currentDigit--;
		}
	}
}
