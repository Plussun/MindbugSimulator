public class StartNextTurnEvent : GameEvent
{
    public int PlayerID;
    public bool KeepActivePlayer;

    public StartNextTurnEvent(int playerID, bool keepActivePlayer)
    {
        PlayerID = playerID;
        KeepActivePlayer = keepActivePlayer;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        gameEngine.StartNextTurn(PlayerID, KeepActivePlayer);
    }
}
