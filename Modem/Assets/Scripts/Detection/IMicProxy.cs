using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IMicProxy {
	bool microphoneEnabled {get;set;}
	void Feed(Word word);
	void OnLongSilence();
	void SetInstantSoundingChar(SoundChars current);
	void OnBeginChar(SoundChars current, int currentIndex);
	void OnEndChar(int currentIndex);
}
