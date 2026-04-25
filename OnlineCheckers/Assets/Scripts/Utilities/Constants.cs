namespace Checkers.Utilities
{
    public static class Constants
    {
        public const string ROOM_PROP_BOARD_STATE   = "BOARD_STATE";
        public const string ROOM_PROP_BOARD_SIZE    = "BOARD_SIZE";
        public const string ROOM_PROP_PLAYER_COUNT  = "PLAYER_COUNT";
        public const string ROOM_PROP_TURN_INDEX    = "TURN_INDEX";
        public const string ROOM_PROP_GAME_STATE    = "GAME_STATE";
        public const string SCENE_LOBBY = "LobbyScene";
        public const string SCENE_GAME  = "GameScene";
        public const int POOL_PIECE_SIZE     = 24;
        public const int POOL_HIGHLIGHT_SIZE = 64;
        public const int MAX_RECONNECT_ATTEMPTS = 3;
        public const float RECONNECT_DELAY_SECONDS = 5f;
        public const string PHOTON_APP_VERSION = "1.0";
        public const int DEFAULT_BOARD_SIZE = 8;
        public const int MIN_BOARD_SIZE = 4;
        public const int MAX_BOARD_SIZE = 8;
        public const int DEFAULT_PLAYER_COUNT = 2;
        public const int MAX_PLAYER_COUNT = 4;
        public const float PIECE_Z_OFFSET = -0.1f;
        public const float DEFAULT_TURN_TIME = 30f;
    }
}
