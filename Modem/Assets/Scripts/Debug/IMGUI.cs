using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//
// IMPORTANT: Make first in script order
//

public class IMGUI : Singleton<IMGUI> {

	public float guiSize = 10f;
	float _separation = 0;
	MonoBehaviour _script;

	Dictionary<MonoBehaviour, float> _offsets = new Dictionary<MonoBehaviour, float>();

	public static void Begin(MonoBehaviour script, string name = null) {
		//Debug.Log("BEGIN " + name + " X " + Instance._separation);
		Instance._script = script;
		GUI.matrix  = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Instance.guiSize);
		GUILayout.BeginVertical();
		GUILayout.Space(Instance._separation);
		if(name!=null) {
			var t = GUI.color;
			GUI.color = Color.magenta;
			GUILayout.Box(name);
			GUI.color = t;
		}
	}

	void DoEnd() {
		GUILayout.Space(15);
		GUILayout.EndVertical();

		//Only valid size after repaint, so remember it when not painting...
		if(Event.current.type == EventType.Repaint) {
			var h = GUILayoutUtility.GetLastRect().height;
			//Debug.Log("SET H=" + h + " IDX="+_index + " NAME=" + _name);
			_offsets[_script] = h;
			_separation += h;
		}
		else {
			//Debug.Log("GET H=" + _offsets[_index] + " IDX="+_index + " NAME=" + _name);
			_offsets.TryGetValue(_script, out _separation);
		}
	}

	public static void End() {
		Instance.DoEnd();
	}

	void OnGUI() {
		_script = null;
		_separation = 0;

		//Begin("IMGUI");
        //hv = GUILayout.HorizontalSlider(hv, 0.0f, 10.0f);
        //GUILayout.Label("This text makes just space");
		//End();	
	}

}
