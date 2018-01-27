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
}

