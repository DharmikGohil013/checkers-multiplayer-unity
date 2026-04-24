namespace Checkers.Utilities
{
    /// <summary>
    /// Static class containing all string constants and magic numbers used across the project.
    /// Centralizes Photon room property keys, scene names, pool sizes, and network settings.
    /// </summary>
    public static class Constants
    {
        // ─── Photon Room Property Keys ───────────────────────────
        public const string ROOM_PROP_BOARD_STATE   = "BOARD_STATE";
        public const string ROOM_PROP_BOARD_SIZE    = "BOARD_SIZE";
        public const string ROOM_PROP_PLAYER_COUNT  = "PLAYER_COUNT";
        public const string ROOM_PROP_TURN_INDEX    = "TURN_INDEX";
        public const string ROOM_PROP_GAME_STATE    = "GAME_STATE";

        // ─── Scene Names ─────────────────────────────────────────
        public const string SCENE_LOBBY = "LobbyScene";
        public const string SCENE_GAME  = "GameScene";

        // ─── Object Pool Sizes ───────────────────────────────────
        /// <summary>Initial pool size for CheckersPiece objects (24 = enough for two 8x8 players).</summary>
        public const int POOL_PIECE_SIZE     = 24;

        /// <summary>Initial pool size for highlight overlays (64 = covers an entire 8x8 board).</summary>
        public const int POOL_HIGHLIGHT_SIZE = 64;

        // ─── Reconnection Settings ───────────────────────────────
        /// <summary>Maximum number of reconnection attempts before giving up.</summary>
        public const int MAX_RECONNECT_ATTEMPTS = 3;

        /// <summary>Delay in seconds between reconnection attempts.</summary>
        public const float RECONNECT_DELAY_SECONDS = 5f;

        // ─── Photon Settings ─────────────────────────────────────
        /// <summary>Default Photon application version string.</summary>
        public const string PHOTON_APP_VERSION = "1.0";

        // ─── Board Constants ─────────────────────────────────────
        /// <summary>Default board size.</summary>
        public const int DEFAULT_BOARD_SIZE = 8;

        /// <summary>Minimum supported board size.</summary>
        public const int MIN_BOARD_SIZE = 4;

        /// <summary>Maximum supported board size.</summary>
        public const int MAX_BOARD_SIZE = 8;

        /// <summary>Default number of players.</summary>
        public const int DEFAULT_PLAYER_COUNT = 2;

        /// <summary>Maximum number of players supported.</summary>
        public const int MAX_PLAYER_COUNT = 4;

        // ─── Gameplay Constants ──────────────────────────────────
        /// <summary>Z-offset for piece GameObjects (to render above cells).</summary>
        public const float PIECE_Z_OFFSET = -0.1f;

        /// <summary>Default turn time limit in seconds.</summary>
        public const float DEFAULT_TURN_TIME = 30f;
    }
}
