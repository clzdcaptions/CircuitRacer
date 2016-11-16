using UnityEngine;
using System.Collections;

public class MainMenuScript : MonoBehaviour, MPLobbyListener
{
	public Texture2D[] buttonTextures;
    private float buttonWidth;
    private float buttonHeight;	
	public Texture2D signOutButton;
	public GUISkin guiSkin;
	private bool _showLobbyDialog;
	private string _lobbyMessage;
	
	void OnGUI() 
	{
		if (!_showLobbyDialog)
		{		
			for (int i = 0; i < 2; i++) 
			{
				if (GUI.Button (new Rect ((float)Screen.width * 0.5f - (buttonWidth / 2),
										(float)Screen.height * (0.6f + (i * 0.2f)) - (buttonHeight / 2),
										buttonWidth,
										buttonHeight), buttonTextures[i])) 
				{
					Debug.Log("Mode " + i + " was clicked!");

					if (i == 0) 
					{
						// Single player mode!
						RetainedUserPicksScript.Instance.multiplayerGame = false;
						Application.LoadLevel("PickCarMenu");
					} 
					else if (i == 1) 
					{
						RetainedUserPicksScript.Instance.multiplayerGame = true;
						_lobbyMessage = "Starting a multi-player game...";
						_showLobbyDialog = true;
						MultiplayerController.Instance.lobbyListener = this;
						MultiplayerController.Instance.SignInAndStartMPGame();
					}
				}
			}
		}

		if (MultiplayerController.Instance.IsAuthenticated()) 
		{
			if (GUI.Button(new Rect(Screen.width  - (buttonWidth * 0.75f),
								Screen.height - (buttonHeight * 0.75f),
								buttonWidth * 0.75f,
								buttonHeight * 0.75f), signOutButton)) 
			{
				MultiplayerController.Instance.SignOut();
			}
		}

		if (_showLobbyDialog) 
		{
			GUI.skin = guiSkin;
			GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.4f, Screen.width * 0.5f, Screen.height * 0.5f), _lobbyMessage);
			if (GUI.Button(new Rect(Screen.width * 0.6f, 
										Screen.height * 0.76f, 
										Screen.width * 0.1f, 
										Screen.height * 0.07f), "Cancel")) 
			{
				MultiplayerController.Instance.LeaveGame();
				HideLobby();
			}
		}
	}
    
    void Start() {
        
        // I know that 301x55 looks good on a 660-pixel wide screen, so we can extrapolate from there
        buttonWidth = 301.0f * Screen.width / 660.0f;
        buttonHeight = 55.0f * Screen.width / 660.0f;
        
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

		MultiplayerController.Instance.TrySilentSignIn();
    }
	
	public void SetLobbyStatusMessage(string message) 
	{
    	_lobbyMessage = message;
	}

	public void HideLobby() 
	{
		_lobbyMessage = "";
		_showLobbyDialog = false;
	}
}
