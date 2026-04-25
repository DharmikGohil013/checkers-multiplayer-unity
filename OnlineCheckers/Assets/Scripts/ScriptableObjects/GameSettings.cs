using UnityEngine;
namespace Checkers.Data
{
    [CreateAssetMenu(fileName = "NewGameSettings", menuName = "Checkers/Game Settings", order = 2)]
    public class GameSettings : ScriptableObject
    {
        [Header("Turn Settings")]
        [Tooltip("Time limit per turn in seconds. 0 = no time limit.")]
        [Range(0f, 120f)]
        public float turnTimeLimit = 30f;
        [Header("Rules")]
        [Tooltip("If true, players must capture if a capture move is available (mandatory capture).")]
        public bool forceCapture = true;
        [Tooltip("If true, pieces reaching the opposite back row are promoted to king.")]
        public bool kingPromotion = true;
        [Header("Photon Settings")]
        [Tooltip("Photon application version string for matchmaking compatibility.")]
        public string photonAppVersion = "1.0";
    }
}
