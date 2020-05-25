synth.exe: synth.cs
	(cd PortAudio.Net; $(MAKE))
	cp PortAudio.Net/build/PortAudio.Net.dll .
	csc /r:System.Numerics.dll /r:PortAudio.Net.dll $^
