using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCanvas {
	public int width = 128;
	public int height = 1024;
	public bool enabled = true;
	public Renderer _rend;
	Texture2D _tex;

	public TexCanvas(Renderer rend, int width, int height) {
		try {
			_rend = rend;
			_tex = new Texture2D(width, height);
			_rend.material.mainTexture = _tex;
		}
		catch {
			Debug.Log("Disabling TexCanvas");
			enabled = false;
		}
	}

	public void Apply() {
		_tex.Apply();
	}

	public void VLine(int x, int y0, int y1, Color color, int enlarge=5) {
		if(!enabled) return;

		if (y0 > y1)
		{
			var t = y0;
			y0 = y1;
			y1 = t;
		}
		y0 = Mathf.Clamp(0, y0-enlarge, _tex.height);
		y1 = Mathf.Clamp(0, y1+enlarge, _tex.height);

		for (int y = y0; y <= y1; y++)
			_tex.SetPixel(x, y, color);
	}

	//Bressenham's line
	public void Line(int x0, int y0, int x1, int y1, Color col) {
		if(!enabled) return;
		
		var dy = y1-y0;
		var dx = x1-x0;
		int stepx, stepy;
   
		if (dy < 0) {dy = -dy; stepy = -1;}
		else {stepy = 1;}
		if (dx < 0) {dx = -dx; stepx = -1;}
		else {stepx = 1;}
		dy <<= 1;
		dx <<= 1;
   
		_tex.SetPixel(x0, y0, col);
		if (dx > dy) {
			var fraction = dy - (dx >> 1);
			while (x0 != x1) {
				if (fraction >= 0) {
					y0 += stepy;
					fraction -= dx;
				}
				x0 += stepx;
				fraction += dy;
				_tex.SetPixel(x0, y0, col);
			}
		}
		else {
			var fraction = dx - (dy >> 1);
			while (y0 != y1) {
				if (fraction >= 0) {
					x0 += stepx;
					fraction -= dy;
				}
				y0 += stepy;
				fraction += dx;
				_tex.SetPixel(x0, y0, col);
			}
		}
	}
}
