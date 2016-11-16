using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames.BasicApi.Multiplayer;

public class GameController : MonoBehaviour, MPUpdateListener 
{
	public GameObject opponentPrefab;
	
	private bool _multiplayerReady;
	private string _myParticipantId;
	private Vector2 _startingPoint = new Vector2(0.09675431f, -1.752321f);
	private float _startingPointYOffset = 0.2f;
	private Dictionary<string, OpponentCarController> _opponentScripts;

	public GameObject myCar;
	public GuiController guiObject;
	public GUISkin guiSkin;
	public GameObject background;
	public Sprite[] backgroundSprites;
	public float[] startTimesPerLevel;
	public int[] lapsPerLevel;

	public bool _paused;
	private float _timeLeft;
	private float _timePlayed;
	private int _lapsRemaining;
	private bool _showingGameOver;
	private bool _multiplayerGame;
	private string gameOvertext;
	private float _nextCarAngleTarget = Mathf.PI;
	private const float FINISH_TARGET = Mathf.PI;

	private float _nextBroadcastTime = 0;
	private Dictionary<string, float> _finishTimes;

	public float timeOutThreshold = 5.0f;
	private float _timeOutCheckInterval = 1.0f;
	private float _nextTimeoutCheck = 0.0f;

	// Use this for initialization
	void Start () {
		RetainedUserPicksScript userPicksScript = RetainedUserPicksScript.Instance;
		_multiplayerGame = userPicksScript.multiplayerGame;
		if (! _multiplayerGame) {
			// Can we get the car number from the previous menu?
			myCar.GetComponent<CarController>().SetCarChoice(userPicksScript.carSelected, false);
			// Set the background
			background.GetComponent<SpriteRenderer>().sprite = backgroundSprites[userPicksScript.diffSelected - 1];
			// Set our time left and laps remaining
			_timeLeft = startTimesPerLevel[userPicksScript.diffSelected - 1];
			_lapsRemaining = lapsPerLevel[userPicksScript.diffSelected - 1];

			guiObject.SetTime (_timeLeft);
			guiObject.SetLaps (_lapsRemaining);
		} else {
            SetupMultiplayerGame();
		}

	}

    void SetupMultiplayerGame() 
	{
		MultiplayerController.Instance.updateListener = this;
        // 1
		_myParticipantId = MultiplayerController.Instance.GetMyParticipantId();
		// 2
		List<Participant> allPlayers = MultiplayerController.Instance.GetAllPlayers();
		_opponentScripts = new Dictionary<string, OpponentCarController>(allPlayers.Count - 1); 

		_finishTimes = new Dictionary<string, float>(allPlayers.Count);

		for (int i =0; i < allPlayers.Count; i++) 
		{
			string nextParticipantId = allPlayers[i].ParticipantId;
			_finishTimes[nextParticipantId] = -1;   // <-- New line here! 
			Debug.Log("Setting up car for " + nextParticipantId);
			// 3
			Vector3 carStartPoint = new Vector3(_startingPoint.x, _startingPoint.y + (i * _startingPointYOffset), 0);
			if (nextParticipantId == _myParticipantId) 
			{
				// 4
				myCar.GetComponent<CarController> ().SetCarChoice(i + 1, true);
				myCar.transform.position = carStartPoint;
			} 
			else 
			{
				// 5
				GameObject opponentCar = (Instantiate(opponentPrefab, carStartPoint, Quaternion.identity) as GameObject);
				OpponentCarController opponentScript = opponentCar.GetComponent<OpponentCarController>();
				opponentScript.SetCarNumber(i+1);
				// 6
				_opponentScripts[nextParticipantId] = opponentScript;
			}
		}
		// 7
		_lapsRemaining = 3;
		_timePlayed = 0; 
		guiObject.SetLaps(_lapsRemaining);
		guiObject.SetTime(_timePlayed);
		_multiplayerReady = true;
    }


	void PauseGame() {
		_paused = true;
		myCar.GetComponent<CarController>().SetPaused(true);
	}
	
	void ShowGameOver(bool didWin) {
		gameOvertext = (didWin) ? "Woo hoo! You win!" : "Awww... better luck next time";
		PauseGame ();
		_showingGameOver = true;
		Invoke ("StartNewGame", 3.0f);
	}

	void StartNewGame() 
	{
		Application.LoadLevel ("MainMenu");
	}

	void OnGUI() 
	{
		if (_multiplayerGame) 
		{
			if (GUI.Button (new Rect (0.0f, 0.0f, Screen.width * 0.1f, Screen.height * 0.1f), "Quit")) {
		
				// Tell the multiplayer controller to leave the game
				MultiplayerController.Instance.LeaveGame();
			} 
		}
		if (_showingGameOver) 
		{
			GUI.skin = guiSkin;
			GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f), gameOvertext);

		}
	}
    
    
    void DoMultiplayerUpdate() 
	{
		if (Time.time > _nextTimeoutCheck) 
		{
			CheckForTimeOuts();
			_nextTimeoutCheck = Time.time + _timeOutCheckInterval;
		}
        // In a multiplayer game, time counts up!
        _timePlayed += Time.deltaTime;
        guiObject.SetTime(_timePlayed);
        
		if (Time.time > _nextBroadcastTime)
		{
			MultiplayerController.Instance.SendMyUpdate(myCar.transform.position.x, 
														myCar.transform.position.y,
														myCar.GetComponent<Rigidbody2D>().velocity, 
														myCar.transform.rotation.eulerAngles.z);
			_nextBroadcastTime = Time.time + .16f;
		}
    }
	
	void Update () 
	{
		if (_paused) 
		{
			return;
		}

		if (_multiplayerGame) 
		{
            DoMultiplayerUpdate();
		} 
		else 
		{
			_timeLeft -= Time.deltaTime;
			guiObject.SetTime (_timeLeft);
			if (_timeLeft <= 0) 
			{
				ShowGameOver (false);
			}
		}

		float carAngle = Mathf.Atan2 (myCar.transform.position.y, myCar.transform.position.x) + Mathf.PI;
		if (carAngle >= _nextCarAngleTarget && (carAngle - _nextCarAngleTarget) < Mathf.PI / 4) 
		{
			_nextCarAngleTarget += Mathf.PI / 2;
			if (Mathf.Approximately(_nextCarAngleTarget, 2*Mathf.PI)) _nextCarAngleTarget = 0;
			if (Mathf.Approximately(_nextCarAngleTarget, FINISH_TARGET)) 
			{
				_lapsRemaining -= 1;
				Debug.Log("Next lap finished!");
				guiObject.SetLaps (_lapsRemaining);
				myCar.GetComponent<CarController>().PlaySoundForLapFinished();
				if (_lapsRemaining <= 0) 
				{
					if (_multiplayerGame) 
					{
						// 1
						myCar.GetComponent<CarController>().Stop();
						// 2
						MultiplayerController.Instance.SendMyUpdate(myCar.transform.position.x,
																	myCar.transform.position.y,
																	new Vector2(0,0),
															myCar.transform.rotation.eulerAngles.z);
						// 3
						MultiplayerController.Instance.SendFinishMessage(_timePlayed);
						PlayerFinished(_myParticipantId, _timePlayed);
					} 
					else 
					{
						ShowGameOver(true);
					}
				}
			}
		}
	}

	public void UpdateReceived(string senderId, int messageNum, float posX, float posY, float velX, float velY, float rotZ) 
	{
		if (_multiplayerReady) 
		{
			OpponentCarController opponent = _opponentScripts[senderId];
			if (opponent != null) {
				opponent.SetCarInformation (messageNum, posX, posY, velX, velY, rotZ);
			}
		}
	}

	public void PlayerFinished(string senderId, float finalTime) 
	{
		Debug.Log ("Participant " + senderId + " has finished with a time of " + finalTime);
		if (_finishTimes[senderId] < 0) {
			_finishTimes[senderId] = finalTime;
		}
		CheckForMPGameOver();
	}

	void CheckForMPGameOver() 
	{
		float myTime = _finishTimes [_myParticipantId];
		int fasterThanMe = 0;
		foreach (float nextTime in _finishTimes.Values) 
		{
			if (nextTime < 0) 
			{ // Somebody's not done yet
				return; 
			}
			if (nextTime < myTime) 
			{
				fasterThanMe++;
			}
		}
		string[] places = new string[]{"1st", "2nd", "3rd", "4th"};
		gameOvertext = "Game over! You are in " + places[fasterThanMe] + " place!";
		PauseGame(); // Should be redundant at this point
		_showingGameOver = true;
		// TODO: Leave the room and go back to the main menu
		Invoke ("LeaveMPGame", 3.0f);
	}

	void LeaveMPGame() 
	{
    	MultiplayerController.Instance.LeaveGame();
	}

	public void LeftRoomConfirmed() 
	{
		MultiplayerController.Instance.updateListener = null;
		Application.LoadLevel ("MainMenu");
	}

	public void PlayerLeftRoom(string participantId) 
	{
		if (_finishTimes[participantId] < 0)
		{
			_finishTimes[participantId] = 999999.0f;
			if (_opponentScripts[participantId] != null) 
			{
				_opponentScripts[participantId].HideCar();
			}
			CheckForMPGameOver();
		}
	}

	void CheckForTimeOuts() 
	{
		foreach (string participantId in _opponentScripts.Keys) 
		{
			// We can skip anybody who's finished.
			if (_finishTimes[participantId] < 0) 
			{
				if (_opponentScripts[participantId].lastUpdateTime < Time.time - timeOutThreshold) 
				{ 
					// Haven't heard from them in a while!
					Debug.Log("Haven't heard from " + participantId + " in " + timeOutThreshold + 
							" seconds! They're outta here!");
					PlayerLeftRoom(participantId);
				}
			}
		}
	}
}
