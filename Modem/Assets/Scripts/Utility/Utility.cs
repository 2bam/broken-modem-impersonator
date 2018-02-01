using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Utility
{
	public static IEnumerable<TR> Zip<T1, T2, TR>(
		this IEnumerable<T1> l1
		, IEnumerable<T2> l2
		, System.Func<T1, T2, TR> selector
	)
	{
		var e1 = l1.GetEnumerator();
		var e2 = l2.GetEnumerator();
		while (e1.MoveNext() && e2.MoveNext())
		{
			yield return selector(e1.Current, e2.Current);
		}
	}

	public static IEnumerable<TR> Zip<T1, T2, T3, TR>(
		this IEnumerable<T1> l1
		, IEnumerable<T2> l2
		, IEnumerable<T3> l3
		, System.Func<T1, T2, T3, TR> selector
	)
	{
		var e1 = l1.GetEnumerator();
		var e2 = l2.GetEnumerator();
		var e3 = l3.GetEnumerator();
		while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
		{
			yield return selector(e1.Current, e2.Current, e3.Current);
		}
	}

	public static T Choice<T>(IList<T> v)
	{
		return v[Random.Range(0, v.Count)];
	}

	public static void LogInfo(string str)
	{
		if (Application.isEditor)
		{
			//Debug.Log(str);
		}
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

