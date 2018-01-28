using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Utility
{
	public static T Choice<T>(IList<T> v)
	{
		return v[Random.Range(0, v.Count)];
	}

	public static float Median(this IEnumerable<float> source)
	{
		if (source == null)
			throw new System.Exception("median error");
		var data = source.OrderBy(n => n).ToArray();
		if (data.Length == 0)
			throw new System.Exception("median error 2");
		if (data.Length % 2 == 0)
			return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0f;
		return data[data.Length / 2];
	}
}

