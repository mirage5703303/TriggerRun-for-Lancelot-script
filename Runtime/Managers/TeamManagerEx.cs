using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Opsive.DeathmatchAIKit;
using BehaviorDesigner.Runtime;


namespace IEdgeGames {

    /*[Serializable]
    public class MatchTeam {

        public PlayerTeam team;
        public Color color = Color.white;

        /// <summary>
        /// 
        /// </summary>
        public int ID => TeamManagerEx.Teams.IndexOf(this) + 1;
    }*/

    public class TeamManagerEx : SingletonBehaviour<TeamManagerEx> {

        // ============================================================================================================================

        //[SerializeField] private List<MatchTeam> m_Teams;

        // ============================================================================================================================

        /// <summary>
        /// 
        /// </summary>
        //public static List<MatchTeam> Teams => Instance ? Instance.m_Teams : null;

        // ============================================================================================================================

        private TeamManager m_OpsiveTeamManager;
        private FieldInfo m_TeamMembers;
        private FieldInfo m_TeamMembership;
        private FieldInfo m_FormationGroups;

        // ============================================================================================================================

        protected override void SingletonAwake() {
            var tm = typeof(TeamManager);
            var formationGroupClass = tm.Assembly.GetTypes().FirstOrDefault(t => t.FullName == $"{t.Namespace}.{tm.Name}+FormationGroup");

            m_OpsiveTeamManager = (TeamManager)tm.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            m_TeamMembers = tm.GetField("m_TeamMembers", BindingFlags.NonPublic | BindingFlags.Instance);
            m_TeamMembership = tm.GetField("m_TeamMembership", BindingFlags.NonPublic | BindingFlags.Instance);
            m_FormationGroups = tm.GetField("m_FormationGroups", BindingFlags.NonPublic | BindingFlags.Instance);

            m_TeamMembers.SetValue(m_OpsiveTeamManager, new List<List<Behavior>> { new List<Behavior>(), new List<Behavior>() });
            m_TeamMembership.SetValue(m_OpsiveTeamManager, new List<HashSet<GameObject>> { new HashSet<GameObject>(), new HashSet<GameObject>() });

            var list = CreateIList(CreateIList(formationGroupClass).GetType());

            list.Add(CreateIList(formationGroupClass));
            list.Add(CreateIList(formationGroupClass));

            m_FormationGroups.SetValue(m_OpsiveTeamManager, list);
        }

        IList CreateIList(Type t) {
            var listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(t);
            return (IList)Activator.CreateInstance(constructedListType);
        }

        // ============================================================================================================================

        /*/// <summary>
        /// 
        /// </summary>
        /// <param name="teamID"></param>
        /// <returns></returns>
        public static MatchTeam GetTeamByID(int teamID) {
            return Teams.FirstOrDefault(t => t.ID == teamID);
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static PlayerTeam GetPlayerTeam(PhotonView player) {
            if (!Instance)
                return PlayerTeam.Null;

            // Faster way of getting the team
            var character = player.GetComponent<TRCharacter>();
            if (character != null) return character.Team;

            /*var teams = (List<HashSet<GameObject>>)Instance.m_TeamMembership.GetValue(Instance.m_OpsiveTeamManager);

            for (var teamID = 0; teamID < teams.Count; teamID++) {
                var teamMembers = teams[teamID];

                for (var i = 0; i < teamMembers.Count; i++) {
                    var member = teamMembers.ElementAtOrDefault(i);

                    if (!member)
                        continue;

                    var pv = member.GetComponent<PhotonView>();

                    if (pv && pv == player)
                        return (PlayerTeam)teamID;
                        //return Teams.ElementAtOrDefault(teamID);
                }
            }*/

            return PlayerTeam.Null;
        }

        // ============================================================================================================================

        /*/// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="teamIndex"></param>
        public static void JoinTeam(PhotonView player, int teamIndex) {
            if (!PhotonNetwork.IsMasterClient || !player)
                return;

            var roomProps = m_Room.CustomProperties;
            var key = $"player{player.ViewID}_team";

            if (roomProps == null)
                roomProps = new PhotonHashtable();

            roomProps[key] = teamIndex;
            m_Room.SetCustomProperties(roomProps);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="teamIndex"></param>
        public static void JoinTeam(GameObject player, int teamIndex) => JoinTeam(player.GetComponent<PhotonView>(), teamIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public static void SetLeader(PhotonView player) {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstPlayer"></param>
        /// <param name="secondPlayer"></param>
        /// <returns></returns>
        public static bool IsTeammate(PhotonView firstPlayer, PhotonView secondPlayer) {
            if (!firstPlayer || !secondPlayer)
                return false;

            return m_Room.CustomProperties.TryGetValue($"player{firstPlayer.ViewID}_team", out object team1)
                && m_Room.CustomProperties.TryGetValue($"player{secondPlayer.ViewID}_team", out object team2)
                && (int)team1 == (int)team2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstPlayer"></param>
        /// <param name="secondPlayer"></param>
        /// <returns></returns>
        public static bool IsTeammate(GameObject firstPlayer, GameObject secondPlayer) {
            return IsTeammate(firstPlayer.GetComponent<PhotonView>(), secondPlayer.GetComponent<PhotonView>());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTeamIndexForPlayer(PhotonView player) {
            return player && m_Room.CustomProperties.TryGetValue($"player{player.ViewID}_team", out object teamIndex) ? (int)teamIndex : -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTeamIndexForPlayer(GameObject player) {
            return GetTeamIndexForPlayer(player.GetComponent<PhotonView>());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public static PhotonView[] GetTeamMembers(int teamId) {
            var result = new List<GameObject>();

            return m_Room.CustomProperties.TryGetValue($"room_team{teamId}", out object value) && value is int[] members
                   ? members.Select(m => PhotonView.Find(m)).ToArray()
                   : new PhotonView[0];
        }*/

        // ============================================================================================================================
    }
}
