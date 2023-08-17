using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	public class SoundtrackManifest {
		//Name of the soundtrack
		public string         Name;
		//Path of the soundtrack in the filesystem
		public string         Path;
		//GUID of the soundtrack
		public string         Guid;
		//List of potential Hold songs
		public HoldData[]	  Holds;
		//List of potential Take songs
		public TakeData[]     Takes;
	}
	
	//Audio files used during the Hold phase
	public class HoldData {
		//Timing dictates when and where the sequence may be played
		public string    Timing;
		//Name. Doesn't do much of anything, really.
		public string    Name;
		//Right when you touch the hold cube
		public AudioClip Intro;
		//Plays after intro
		public AudioClip Lo;
		//Transition to MedHi
		public AudioClip Transition;
		//Plays at later stages of the hold
		public AudioClip MedHi;
		//End of the hold
		public AudioClip End;
	}
	
	//Metadata for the Take phase
	public class TakeData {
		//Timing dictates when and where the sequence may be played
		public string    Timing;
		//Name. Doesn't do much of anything, really.
		public string    Name;
		//Looping track during the take
		public AudioClip Track;
	}
}