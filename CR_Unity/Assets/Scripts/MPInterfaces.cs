public interface MPLobbyListener 
{
    void SetLobbyStatusMessage(string message);
    void HideLobby();
}

public interface MPUpdateListener 
{
    void UpdateReceived(string participantId, int messageNum, float posX, float posY, float velX, float velY, float rotZ);

    void PlayerFinished(string senderId, float finalTime);

    void LeftRoomConfirmed();

    void PlayerLeftRoom(string participantId);
}
