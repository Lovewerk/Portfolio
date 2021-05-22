using UnityEngine;

public class MelodyGenerator : MonoBehaviour
{
	[SerializeField] MelodicProbabilities melodicProbabilities;

	[SerializeField] Note nextNote;
	public Note NextNote => nextNote;

	[SerializeField] Note lastPlayedNote;
	public Note LastPlayedNote => lastPlayedNote;

	private bool isPlaying;
	public bool IsPlaying => isPlaying;

	int ticksUntilNextNote = -1;

	[SerializeField] private int notesPlayedTotal;
	[SerializeField] private int notesPlayedSinceEnabled;

	public int NotesPlayedTotal => notesPlayedTotal;
	public int NotesPlayedSinceEnabled => notesPlayedSinceEnabled;

	public delegate void EnemyNoteEventHandler(object obj, EnemyNoteEventArgs args);
	public event EnemyNoteEventHandler EnemyNoteEvent;

	FMOD.Studio.EventInstance enemyInstrument;
	Transform tf;

    void Awake()
    {
        if(!melodicProbabilities) 
		{ 
			Debug.LogError("A MelodicProbabilities Object must be assigned!"); 
		}
		else
		{
			melodicProbabilities?.TryInit();
		}		

		tf = transform;
		enemyInstrument = FMODUnity.RuntimeManager.CreateInstance("event:/SoundFX/Enemy/Piano Instr");
    }

	private void OnEnable()
	{
		MusicManager.DownbeatEvent -= StartPlaying; // removes existng event subscription if one exists, does not cause an error if there is none, so it's safe
		MusicManager.DownbeatEvent += StartPlaying;

		notesPlayedSinceEnabled = 0;
		
		nextNote = melodicProbabilities.GenerateNote();
	}

	private void OnDisable()
	{
		StopPlaying();
	}

	private void Update()
	{
		enemyInstrument.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(tf));
	}

	private void StartPlaying()
	{
		ticksUntilNextNote = 0; //play on next tick
		SubscribeToTicks(true); //Start listening to the tempo
		enemyInstrument.start();
		isPlaying = true;

		MusicManager.DownbeatEvent -= StartPlaying;
	}

	public void StopPlaying()
	{
		SubscribeToTicks(false);
		enemyInstrument.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		isPlaying = false;

		MusicManager.DownbeatEvent -= StartPlaying; //just in case gameObject is enabled and then disabled before start event is fired.
	}

	void SubscribeToTicks(bool subscribe)
	{
		if(subscribe)
		{
			Metronome.MetronomeTickEvent -= HandleTick;
			Metronome.MetronomeTickEvent += HandleTick;
		}
		else
		{
			Metronome.MetronomeTickEvent -= HandleTick;
		}
	}

	private void HandleTick()
	{
		if(ticksUntilNextNote == 0)
		{
			ticksUntilNextNote = nextNote.Duration;
			RaiseEnemyNoteEvent();

			notesPlayedSinceEnabled++;
			notesPlayedTotal++;
		}
		ticksUntilNextNote--;
	}

	void RaiseEnemyNoteEvent()
	{
		enemyInstrument.setParameterByName("Note Choice", nextNote.Pitch); // set the next note in FMOD
		EnemyNoteEvent.Invoke(this, new EnemyNoteEventArgs(nextNote)); 
		
		lastPlayedNote = nextNote;
		nextNote = melodicProbabilities.GenerateNote(lastPlayedNote);
	}
}

public class EnemyNoteEventArgs : EventArgs
{
	Note note;
	
	public Note Note => note;

	public EnemyNoteEventArgs(Note note)
	{
		this.note = note;
	}
}