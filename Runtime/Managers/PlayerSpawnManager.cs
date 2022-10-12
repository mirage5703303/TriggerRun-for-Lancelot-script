using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
using Opsive.UltimateCharacterController.Game;
using Opsive.UltimateCharacterController.Character.Abilities;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Opsive.UltimateCharacterController.Character;
using BehaviorDesigner.Runtime;
using Opsive.UltimateCharacterController.Traits;
using Opsive.DeathmatchAIKit;
using Opsive.Shared.Input;
using Opsive.UltimateCharacterController.AddOns.Multiplayer.Character;
using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character;
using TrickCore;
using UnityEngine.ResourceManagement.AsyncOperations;
using EventHandler = Opsive.Shared.Events.EventHandler;
using Random = UnityEngine.Random;

namespace IEdgeGames {

    public class PlayerSpawnManager : SingleCharacterSpawnManager {

        // ============================================================================================================================
        private List<SpawnPoint> m_SpawnLocations;
        private int m_LastTeamIndex = -1;

        private readonly Dictionary<Player, byte> m_PlayerTeam = new Dictionary<Player, byte>();
        private readonly Dictionary<byte, int> m_TeamMemberCount = new Dictionary<byte, int>();

        private static readonly RaiseEventOptions m_RaiseEventOthers = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        // ============================================================================================================================

        /// <summary>
        /// Overriding the start method to prevent spawn the local player without setup team first.
        /// </summary>
        void Start() {
            var fields = typeof(SpawnManagerBase).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).ToDictionary(f => f.Name);

            var kinematicObjectManager = FindObjectOfType<KinematicObjectManager>();
            fields["m_Players"].SetValue(this, new PhotonView[kinematicObjectManager.StartCharacterCount]);
            fields["m_ActorNumberByPhotonViewIndex"].SetValue(this, new Dictionary<int, int>());

            // Cache the raise event options.
            fields["m_ReliableSendOption"].SetValue(this, new SendOptions { Reliability = true });
            fields["m_RaiseEventOptions"].SetValue(this, new RaiseEventOptions {
                CachingOption = EventCaching.DoNotCache,
                Receivers = ReceiverGroup.Others
            });

            Character = ProjectParameters.TestCharacterPrefab;
            m_SpawnLocations = FindObjectsOfType<SpawnPoint>().Where(s => s.Grouping == 1 || s.Grouping == 2).ToList();

            if (PhotonNetwork.IsMasterClient) {

                PhotonNetwork.SetPlayerCustomProperties(new PhotonHashtable() { { RoomProps.LOAD_GAME_SCENE_READY, true } });
                PhotonNetwork.CurrentRoom.SetCustomProperties(new PhotonHashtable { { RoomProps.ROOM_INITIALIZED, true } });

                StartCoroutine(WaitForEveryoneToLoadScene());
            }
            else
                StartCoroutine(InitializeRoomRoutine());
        }

        IEnumerator WaitForEveryoneToLoadScene()
        {
            Debug.Log("[WaitForEveryoneToLoadScene] Waiting... (Master)");

            while (!PhotonNetwork.PlayerList.All(player => player.CustomProperties.ContainsKey(RoomProps.LOAD_GAME_SCENE_READY)))
            {
                yield return new WaitForSeconds(1.0f);
            }
            
            Debug.Log("[WaitForEveryoneToLoadScene] Everyone has the scene loaded");
            
            // spawn the master client
            Debug.Log("[Client] Spawn all players (MASTER)");
            PhotonNetwork.CurrentRoom.Players.Values.ToList().ForEach(SpawnPlayer);

            // fill with AI
            if (UISelectMode.IsTeamDeathMatch && GameSession.Instance.TeamDeathMatchAutoFillGameWithAI)
            {
                if (PhotonNetwork.CurrentRoom.MaxPlayers != 0)
                {
                    void DoSpawnAI()
                    {
                        var room = PhotonNetwork.CurrentRoom;
                        var roomProps = room.CustomProperties;

                        for (var i = 0; i < room.MaxPlayers; i++)
                            if (roomProps.TryGetValue($"player{i + 1}_ai", out object isAI) && (bool)isAI)
                            {
                                var i1 = i;
                                SpawnAI(i1 + 1);
                            }
                    }
                    DoSpawnAI();
                }
            }
        }

        IEnumerator InitializeRoomRoutine()
        {
            Debug.Log("[InitializeRoomRoutine] Waiting... (Cient)");
            
            // tell everyone that my (client) scene is loaded
            PhotonNetwork.SetPlayerCustomProperties(new PhotonHashtable() { { RoomProps.LOAD_GAME_SCENE_READY, true } });
            
            while (!PhotonNetwork.PlayerList.All(player => player.CustomProperties.ContainsKey(RoomProps.LOAD_GAME_SCENE_READY)))
            {
                yield return new WaitForSeconds(1.0f);
            }
            
            Debug.Log("[InitializeRoomRoutine] Everyone has the scene loaded");
        }

        public override void OnEnable() {
            base.OnEnable();
            PhotonNetwork.NetworkingClient.EventReceived += OnEventInternal;
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnSpawnPlayer);
        }

        public override void OnDisable() {
            base.OnDisable();
            PhotonNetwork.NetworkingClient.EventReceived -= OnEventInternal;
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnSpawnPlayer);
        }

        // ============================================================================================================================
        
        protected void GetCharacterPrefabAsync(int playerIndex, bool isAI = false, Action<GameObject> onLoaded = default)
        {
            if (Character)
            {
                onLoaded?.Invoke(Character);
                return;
            }

            CharacterDefinition characterDef;

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"player{playerIndex}_id", out object _id) && _id is int ID && ID != -1) {
                //characterDef = CharacterContent.Characters.ElementAtOrDefault(ID);
                characterDef = CharacterContent.Characters.FirstOrDefault(c => c.id == ID);
                if (characterDef != null)
                {
                    // make the prefab inactive, so we can remove components without awake being called, and then active it ourself later
                    characterDef.prefabDirect.gameObject.SetActive(false);
                    onLoaded?.Invoke(characterDef.prefabDirect);
                    characterDef.prefabDirect.gameObject.SetActive(true);
                    /*if (characterDef.prefab.Asset != null) onLoaded?.Invoke(characterDef.prefab.Asset as GameObject);
                    else onLoaded?.Invoke(characterDef.prefab.LoadAssetAsync().WaitForCompletion());*/
                    // characterDef.prefab.TRLoadAssetAsync(onLoaded);
                }
            }
            else
            {
                // characterDef = CharacterContent.Characters.ElementAtOrDefault(Random.Range(0, CharacterContent.Characters.Count));
                characterDef = CharacterContent.Characters[playerIndex % CharacterContent.Characters.Count];
                if (characterDef != null)
                {
                    // make the prefab inactive, so we can remove components without awake being called, and then active it ourself later
                    characterDef.prefabDirect.gameObject.SetActive(false);
                    onLoaded?.Invoke(characterDef.prefabDirect);
                    characterDef.prefabDirect.gameObject.SetActive(true);
                    /*if (characterDef.prefab.Asset != null) onLoaded?.Invoke(characterDef.prefab.Asset as GameObject);
                    else onLoaded?.Invoke(characterDef.prefab.LoadAssetAsync().WaitForCompletion());*/
                    // characterDef.prefab.TRLoadAssetAsync(onLoaded);
                }
            }
        }

        /*/// <summary>
        /// 
        /// </summary>
        /// <param name="newPlayer"></param>
        /// <returns></returns>
        protected override GameObject GetCharacterPrefab(Player newPlayer) {
            return GetCharacterPrefab(newPlayer.ActorNumber);
        }*/

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps) {
            Debug.Log($"PUN: <Color=Blue><b>OnPlayerPropertiesUpdate</b></color>");

            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.LocalPlayer == targetPlayer)
                return;
        }

        public override void OnPlayerEnteredRoom(Player newPlayer) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPlayer"></param>
        public new void SpawnPlayer(Player newPlayer) {
            Mode = SpawnMode.FixedLocation;

            if (m_LastTeamIndex == -1)
                m_LastTeamIndex = Random.Range(0, 2);

            m_SpawnPointGrouping = m_LastTeamIndex == 0 ? 0 : 1;
            m_LastTeamIndex = m_LastTeamIndex == 0 ? 1 : 0;

            SpawnLocation = GetSpawnPoint(m_SpawnPointGrouping);

            base.SpawnPlayer(newPlayer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="isAI"></param>
        /// <returns></returns>
        protected GameObject GetCharacterPrefab(int playerIndex, bool isAI = false) {
            /*if (isAI) {
                var aiPrefab = ProjectParameters.AICharacters.ElementAtOrDefault(Random.Range(0, ProjectParameters.AICharacters.Length));

                if (!aiPrefab)
                    Debug.LogWarning("No AI prefab defined.");

                return aiPrefab;
            }*/

            if (Character)
                return Character;

            CharacterDefinition characterDef;

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue($"player{playerIndex}_id", out object _id) && _id is int ID && ID != -1)
            {
                //characterDef = CharacterContent.Characters.ElementAtOrDefault(ID);
                characterDef = CharacterContent.Characters.FirstOrDefault(c => c.id == ID);
                if (characterDef != null) return characterDef.prefabDirect;
                /*return characterDef && characterDef.prefab.Asset 
                    ? (GameObject)characterDef.prefab.Asset 
                    : characterDef.prefab.LoadAssetAsync().WaitForCompletion();*/
            }

            characterDef = CharacterContent.Characters.ElementAtOrDefault(Random.Range(0, CharacterContent.Characters.Count));
            return characterDef != null ? characterDef.prefabDirect : null;
            // return characterDef.prefab.Asset ? (GameObject)characterDef.prefab.Asset : characterDef.prefab.LoadAssetAsync().WaitForCompletion();
        }

        protected override GameObject GetCharacterPrefab(Player newPlayer) {
            return GetCharacterPrefab(newPlayer.ActorNumber);
        }

        void OnSpawnPlayer(Player newPlayer, GameObject playerGo) {
            var trPlayer = playerGo ? playerGo.GetComponent<TRCharacter>() : null;

            if (!trPlayer || !trPlayer.photonView) {
                Debug.LogError("WTF Happened!");
                return;
            }
            
            trPlayer.IsMine = trPlayer.photonView.IsMine;
            
            if (trPlayer.IsMine) {
                var vc = FindObjectOfType<Opsive.Shared.Input.VirtualControls.VirtualControlsManager>();

                if (vc)
                    vc.Character = trPlayer.gameObject;
            }
            
            // Battle Royale init parameters
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 0) {
                if (PhotonNetwork.IsMasterClient) {
                    var position = trPlayer.transform.position;
                    position.y += 800f;
                    trPlayer.transform.position = position;
                }

                var characterDefinition = CharacterContent.Characters.FirstOrDefault(c => c.name == trPlayer.CharacterName);

                if (characterDefinition)
                {
                    //characterDefinition.brPrefab.InstantiateAsync(trPlayer.transform).WaitForCompletion();
                    Instantiate(characterDefinition.brPrefabDirect, trPlayer.transform);
                }
            }

            if (!PhotonNetwork.IsMasterClient)
                return;

            var teamID = m_SpawnPointGrouping;
            TeamManager.AddTeamMember(trPlayer.gameObject, teamID);

            playerGo.name = playerGo.name.Before("(Clone)") + (teamID == 0 ? $"#{newPlayer.ActorNumber} (Blue)" : $"#{newPlayer.ActorNumber} (Red)");

            var characterLayerManager = trPlayer.GetComponent<CharacterLayerManager>();
            var characterLocomotion = trPlayer.GetComponent<UltimateCharacterLocomotion>();
            var friendlyLayer = teamID == 0 ? 14 : 15;
            var enemyLayer = teamID == 0 ? 15 : 14;

            // Testing parameters
            if (UISelectMode.SelectedMode == GameMode.PlaySolo && ProjectParameters.FastModeEnabled) {
                characterLocomotion.GravityMagnitude = 2.5f;
                characterLocomotion.TimeScale = 2.5f;

                if (characterLocomotion.Abilities.FirstOrDefault(a => a is Jump) is Jump jump)
                    jump.Force = 2.5f;
            }

            // Set the layer of the player collider.
            trPlayer.gameObject.layer = friendlyLayer;
            characterLayerManager.CharacterLayer = 1 << friendlyLayer;

            for (var i = 0; i < characterLocomotion.ColliderCount; ++i)
                characterLocomotion.Colliders[i].gameObject.layer = friendlyLayer;

            // The player should recognize the enemy layers.
            characterLayerManager.EnemyLayers = 1 << LayerManager.Character | 1 << LayerManager.Enemy | 1 << enemyLayer;

            var spawnPosition = Vector3.zero;
            var spawnRotation = Quaternion.identity;
            var spawnPoint = GetSpawnPoint(teamID).GetComponent<SpawnPoint>();
            if (!spawnPoint.GetPlacement(playerGo, ref spawnPosition, ref spawnRotation))
            {
                spawnPosition = m_SpawnLocation.position;
                spawnRotation = m_SpawnLocation.rotation;
            }
            playerGo.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            
            var respawner = trPlayer.GetComponent<CharacterRespawner>();
            if (respawner)
            {
                respawner.Grouping = teamID + 1;
                respawner.ScheduleRespawnOnDeath = false;
                respawner.ScheduleRespawnOnDisable = false;
                Debug.LogWarning($"[SPAWN] Set player {newPlayer.ActorNumber} to spawn at grouping: {respawner.Grouping}, team:{(teamID == 0 ? "Blue" : "Red")}");
            }
            
            var playerData = new object[] { trPlayer.photonView.ViewID, teamID, newPlayer.ActorNumber };
            PhotonNetwork.RaiseEvent(PUNEvents.PLAYER_INSTANTIATION, playerData, m_RaiseEventOthers, SendOptions.SendReliable);

            Debug.Log($"Setting spawn group '{teamID}' for player <Color=Green><b>{newPlayer}</b></color>");

            GameSession.Instance.RegisterPlayer(trPlayer, newPlayer.ActorNumber, teamID, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slotIndex"></param>
        public void SpawnAI(int playerIndex) 
        {
            if (m_LastTeamIndex == -1)
                m_LastTeamIndex = Random.Range(0, 2);
            var spawnRotation = Quaternion.identity;
            var teamID = m_LastTeamIndex;
            Mode = SpawnMode.FixedLocation;
            var spawnPoint = GetSpawnPoint(teamID).GetComponent<SpawnPoint>();
            Debug.LogWarning($"[SpawnAI] {playerIndex} (spawn={spawnPoint.name})");
            m_LastTeamIndex = m_LastTeamIndex == 0 ? 1 : 0;
            
            var spawnPosition = spawnPoint.transform.position + (Random.insideUnitCircle is { } v
                ? new Vector3(v.x, 0.0f, v.y) * spawnPoint.Size
                : Vector3.zero);
            
            GetCharacterPrefabAsync(playerIndex, true, o =>
            {
                var aiAgent = Instantiate(o);
                
                aiAgent.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                var photonView = aiAgent.GetComponent<PhotonView>();
                photonView.ViewID = PhotonNetwork.AllocateViewID(0);
                //AddWayPoints(aiAgent.GetComponent<BehaviorTree>());

                //SetupAIAgent(aiAgent, photonView.ViewID, teamID, spawnPosition, spawnRotation);
                
                aiAgent.name = aiAgent.name.Before("(Clone)") + $" AI#{photonView.ViewID}" + (teamID == 0 ? " (Blue)" : " (Red)");
                
                var lookSource = aiAgent.GetComponent<ILookSource>();
                if (lookSource is PunLookSource punLookSource)
                {
                    Debug.LogWarning("[SetupAIAgent] Replacing PunLookSource with LocalLookSource");
                    DestroyImmediate(punLookSource);
                    aiAgent.gameObject.AddComponent<LocalLookSource>();
                }
                
                TeamManager.AddTeamMember(aiAgent, teamID);
                var respawner = aiAgent.GetComponent<CharacterRespawner>();
                if (respawner)
                {
                    respawner.ScheduleRespawnOnDeath = false;
                    respawner.ScheduleRespawnOnDisable = false;
                    respawner.Grouping = teamID + 1;
                    Debug.LogWarning($"[SetupAIAgent] Set AI #{photonView.ViewID} to spawn at grouping: {respawner.Grouping}, team:{(teamID == 0 ? "Blue" : "Red")}");
                }

                
                Destroy(aiAgent.GetComponent<NetworkCharacterLocomotionHandler>());
                Destroy(aiAgent.GetComponent<UnityInput>());
                

                var renderers = aiAgent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                for (var i = 0; i < renderers.Length; ++i) {
                    var materials = renderers[i].materials;

                    for (var j = 0; j < materials.Length; ++j) {
                        // Do not compare the material directly because the player may be using an instance material.
                        if (materials[j].name.Contains("Primary"))
                            materials[j].color = teamID == 0 ? Color.blue : Color.red;
                        else if (materials[j].name.Contains("Secondary"))
                            materials[j].color = teamID == 0 ? Color.blue : Color.red;
                    }
                }


                var characterLayerManager = aiAgent.GetComponent<CharacterLayerManager>();
                var characterLocomotion = aiAgent.GetComponent<UltimateCharacterLocomotion>();
                var friendlyLayer = teamID == 0 ? 14 : 15;
                var enemyLayer = teamID == 0 ? 15 : 14;

                // Set the layer of the player collider.
                aiAgent.layer = friendlyLayer;
                characterLayerManager.CharacterLayer = 1 << friendlyLayer;

                for (var i = 0; i < characterLocomotion.ColliderCount; ++i)
                    characterLocomotion.Colliders[i].gameObject.layer = friendlyLayer;

                // The player should recognize the enemy layers.
                characterLayerManager.EnemyLayers = 1 << enemyLayer;
                
                Debug.Log($"Setting spawn group '{teamID}' for ai agent <Color=Green><b>{aiAgent}</b></color>");
                aiAgent.gameObject.SetActive(true);
                characterLocomotion.SetPositionAndRotation(spawnPosition, spawnRotation);
                Debug.Log($"AI position diff = {Vector3.Distance(spawnPosition, aiAgent.transform.position)} - {aiAgent}");
                GameSession.Instance.RegisterPlayer(aiAgent.GetComponent<TRCharacter>(), playerIndex, teamID, true);
                
                var spawnData = new object[] {
                    playerIndex,
                    spawnPosition,
                    spawnRotation,
                    photonView.ViewID,
                    teamID,
                };

                PhotonNetwork.RaiseEvent(PUNEvents.AI_INSTANTIATION, spawnData, m_RaiseEventOthers, SendOptions.SendReliable);
            });
        }

        private void SetupAIAgent(GameObject aiAgent, int viewID, int teamID, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            aiAgent.name = aiAgent.name.Before("(Clone)") + $" AI#{viewID}" + (teamID == 0 ? " (Blue)" : " (Red)");
            TeamManager.AddTeamMember(aiAgent, teamID);

            // for AI, replace PunLookSource with LocalLookSource
            var lookSource = aiAgent.GetComponent<ILookSource>();
            if (lookSource is PunLookSource punLookSource)
            {
                Debug.LogWarning("[SetupAIAgent] Replacing PunLookSource with LocalLookSource");
                DestroyImmediate(punLookSource);
                aiAgent.gameObject.AddComponent<LocalLookSource>();
            }
            
            Destroy(aiAgent.GetComponent<NetworkCharacterLocomotionHandler>());
            Destroy(aiAgent.GetComponent<UnityInput>());

            var respawner = aiAgent.GetComponent<CharacterRespawner>();
            
            if (respawner)
            {
                respawner.ScheduleRespawnOnDeath = false;
                respawner.ScheduleRespawnOnDisable = false;
                respawner.Grouping = teamID + 1;
                respawner.Respawn(spawnPosition, spawnRotation, false);
                respawner.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                Debug.LogWarning($"[SetupAIAgent] Set AI #{photonView.ViewID} to spawn at grouping: {respawner.Grouping}, team:{(teamID == 0 ? "Blue" : "Red")}");
            }
            
            var renderers = aiAgent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (var i = 0; i < renderers.Length; ++i) {
                var materials = renderers[i].materials;

                for (var j = 0; j < materials.Length; ++j) {
                    // Do not compare the material directly because the player may be using an instance material.
                    if (materials[j].name.Contains("Primary"))
                        materials[j].color = teamID == 0 ? Color.blue : Color.red;
                    else if (materials[j].name.Contains("Secondary"))
                        materials[j].color = teamID == 0 ? Color.blue : Color.red;
                }
            }


            var characterLayerManager = aiAgent.GetComponent<CharacterLayerManager>();
            var characterLocomotion = aiAgent.GetComponent<UltimateCharacterLocomotion>();
            var friendlyLayer = teamID == 0 ? 14 : 15;
            var enemyLayer = teamID == 0 ? 15 : 14;

            // Set the layer of the player collider.
            aiAgent.layer = friendlyLayer;
            characterLayerManager.CharacterLayer = 1 << friendlyLayer;

            for (var i = 0; i < characterLocomotion.ColliderCount; ++i)
                characterLocomotion.Colliders[i].gameObject.layer = friendlyLayer;

            // The player should recognize the enemy layers.
            characterLayerManager.EnemyLayers = 1 << enemyLayer;
        }

        void AddWayPoints(BehaviorTree behaviorTree) {
            var waypoints = behaviorTree.GetVariable("Waypoints") as SharedGameObjectList;

            foreach (var wp in GameObject.FindGameObjectsWithTag("WayPoint"))
                waypoints.Value.Add(wp);
        }

        void OnEventInternal(EventData photonEvent) {
            switch (photonEvent.Code) {
                case PUNEvents.PLAYER_INSTANTIATION:
                {
                    var playerData = (object[])photonEvent.CustomData;
                    var playerPV = PhotonView.Find((int)playerData[0]);
                    if (playerPV == null)
                    {
                        IEnumerator WaitForView()
                        {
                            playerPV = PhotonView.Find((int)playerData[0]);
                            yield return new WaitUntil(() =>
                            {
                                playerPV = PhotonView.Find((int)playerData[0]);
                                return playerPV != null;
                            });
                            if (playerPV != null)
                            {
                                Debug.LogWarning($"[OnEventInternal] PLAYER_INSTANTIATION view object found (WaitForView)!");
                                Loaded(playerPV.gameObject, (int)playerData[0]);
                            }
                        }
                        StartCoroutine(WaitForView());
                        Debug.LogWarning($"[OnEventInternal] PLAYER_INSTANTIATION view with id {(int)playerData[0]} not found. Waiting for it...");
                    }
                    else
                    {
                        Loaded(playerPV.gameObject, (int)playerData[0]);
                        Debug.LogWarning($"[OnEventInternal] PLAYER_INSTANTIATION view object found (Direct)!");
                    }
                    
                    void Loaded(GameObject o, int viewID)
                    {
                        var teamID = (int)playerData[1];
                        TeamManager.AddTeamMember(o.gameObject, teamID);
                        /*o.GetComponent<PhotonView>().ViewID = viewID;*/
                        var respawner = o.GetComponent<CharacterRespawner>();

                        o.name = o.name.Before("(Clone)") + (teamID == 0 ? $"#{(int)playerData[2]} (Blue)" : $"#{(int)playerData[2]} (Red)");


                        if (respawner)
                        {
                            respawner.Grouping = (int)playerData[1] + 1;
                            respawner.ScheduleRespawnOnDeath = false;
                            respawner.ScheduleRespawnOnDisable = false;
                        }

                        var characterLayerManager = o.GetComponent<CharacterLayerManager>();
                        var characterLocomotion = o.GetComponent<UltimateCharacterLocomotion>();
                        var friendlyLayer = (int)playerData[1] == 0 ? 14 : 15;
                        var enemyLayer = (int)playerData[1] == 0 ? 15 : 14;

                        // Set the layer of the player collider.
                        o.layer = friendlyLayer;
                        characterLayerManager.CharacterLayer = 1 << friendlyLayer;

                        for (var i = 0; i < characterLocomotion.ColliderCount; ++i)
                            characterLocomotion.Colliders[i].gameObject.layer = friendlyLayer;

                        // The player should recognize the enemy layers.
                        characterLayerManager.EnemyLayers = 1 << LayerManager.Character | 1 << LayerManager.Enemy | 1 << enemyLayer;

                        o.gameObject.SetActive(true);
                        GameSession.Instance.RegisterPlayer(o.GetComponent<TRCharacter>(), (int)playerData[2], (int)playerData[1], false);
                    }
                }
                    break;

                case PUNEvents.AI_INSTANTIATION:
                {
                    var spawnData = (object[])photonEvent.CustomData;
                    GetCharacterPrefabAsync((int)spawnData[0], true, o =>
                    {
                        var aiAgent = Instantiate(o, (Vector3)spawnData[1], (Quaternion)spawnData[2]);
                        var view = aiAgent.GetComponent<PhotonView>();
                        int teamID = (int)spawnData[4];
                        int playerIndex = (int)spawnData[0];
                        view.ViewID = (int)spawnData[3];
                        aiAgent.name = aiAgent.name.Before("(Clone)") + $" AI#{view.ViewID}" + (teamID == 0 ? " (Blue)" : " (Red)");
                        //AddWayPoints(aiAgent.GetComponent<BehaviorTree>());

                        //SetupAIAgent(aiAgent, photonView.ViewID, teamID, (Vector3)spawnData[1], (Quaternion)spawnData[2]);
                        
                        var lookSource = aiAgent.GetComponent<ILookSource>();
                        if (lookSource is PunLookSource punLookSource)
                        {
                            Debug.LogWarning("[SetupAIAgent] Replacing PunLookSource with LocalLookSource");
                            DestroyImmediate(punLookSource);
                            aiAgent.gameObject.AddComponent<LocalLookSource>();
                        }
                        
                        Destroy(aiAgent.GetComponent<NetworkCharacterLocomotionHandler>());
                        Destroy(aiAgent.GetComponent<UnityInput>());
                        
                        var respawner = aiAgent.GetComponent<CharacterRespawner>();

                        if (respawner)
                        {
                            respawner.Grouping = teamID + 1;
                            respawner.ScheduleRespawnOnDeath = false;
                            respawner.ScheduleRespawnOnDisable = false;
                            Debug.LogWarning($"[SetupAIAgent] Set AI #{view.ViewID} to spawn at grouping: {respawner.Grouping}, team:{(teamID == 0 ? "Blue" : "Red")}");
                        }

                        var renderers = aiAgent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                        for (var i = 0; i < renderers.Length; ++i) {
                            var materials = renderers[i].materials;

                            for (var j = 0; j < materials.Length; ++j) {
                                // Do not compare the material directly because the player may be using an instance material.
                                if (materials[j].name.Contains("Primary"))
                                    materials[j].color = (int)spawnData[4] == 0 ? Color.blue : Color.red;
                                else if (materials[j].name.Contains("Secondary"))
                                    materials[j].color = (int)spawnData[4] == 0 ? Color.blue : Color.red;
                            }
                        }

                        var characterLayerManager = aiAgent.GetComponent<CharacterLayerManager>();
                        var characterLocomotion = aiAgent.GetComponent<UltimateCharacterLocomotion>();
                        var friendlyLayer = (int)spawnData[4] == 0 ? 14 : 15;
                        var enemyLayer = (int)spawnData[4] == 0 ? 15 : 14;

                        // Set the layer of the player collider.
                        aiAgent.layer = friendlyLayer;
                        characterLayerManager.CharacterLayer = 1 << friendlyLayer;

                        for (var i = 0; i < characterLocomotion.ColliderCount; ++i)
                            characterLocomotion.Colliders[i].gameObject.layer = friendlyLayer;

                        // The player should recognize the enemy layers.
                        characterLayerManager.EnemyLayers = 1 << enemyLayer;
                        aiAgent.gameObject.SetActive(true);
                        characterLocomotion.SetPositionAndRotation((Vector3)spawnData[1], (Quaternion)spawnData[2]);
                        GameSession.Instance.RegisterPlayer(aiAgent.GetComponent<TRCharacter>(), playerIndex, teamID, true);
                    });
                }
                    break;
            }
        }

        Transform GetSpawnPoint(int teamID) {
            var spawnLocation = m_SpawnLocations.Where(s => s.Grouping == teamID + 1).ToArray();
            var random = Random.Range(0, spawnLocation.Length);
            return spawnLocation.Length > 0 ? spawnLocation[random].transform : null;
        }
    }
}
