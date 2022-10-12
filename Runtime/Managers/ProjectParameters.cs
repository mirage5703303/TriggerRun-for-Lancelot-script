using System.Linq;
using UnityEngine;

namespace IEdgeGames {

    [CreateAssetMenu(fileName = "ProjectParameters", menuName = "IEdge Shooter Game/Project Parameters")]
    public class ProjectParameters : ScriptableObject { //SingletonScriptableObject<ProjectParameters> {

        [Header("Testing")]
        [SerializeField] private bool m_LockWeapons;
        [SerializeField] private bool m_FastModeEnabled;
        [SerializeField] private GameObject m_TestCharacterPrefab;

        [Header("Maps")]
        [SerializeField] private MapInfo m_BattleRoyaleMap;
        [SerializeField] private MapInfo[] m_Maps;

        [Header("Characters")]
        [SerializeField] private GameObject[] m_AICharacters;

        /// <summary>
        /// 
        /// </summary>
        public static ProjectParameters Instance => m_Instance ? m_Instance : m_Instance = Resources.Load<ProjectParameters>("ProjectParameters");

        private static ProjectParameters m_Instance;

        /// <summary>
        /// 
        /// </summary>
        public static bool LockWeapons => Instance ? Instance.m_LockWeapons : false;

        /// <summary>
        /// 
        /// </summary>
        public static bool FastModeEnabled=> Instance ? Instance.m_FastModeEnabled : false;

        /// <summary>
        /// 
        /// </summary>
        public static GameObject TestCharacterPrefab => Instance ? Instance.m_TestCharacterPrefab : null;

        /// <summary>
        /// 
        /// </summary>
        public static MapInfo BattleRoyaleMap => Instance ? Instance.m_BattleRoyaleMap : null;

        /// <summary>
        /// 
        /// </summary>
        public static MapInfo[] Maps => Instance ? Instance.m_Maps : null;

        /// <summary>
        /// 
        /// </summary>
        public static GameObject[] AICharacters => Instance ? Instance.m_AICharacters : null;
    }
}
