using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;

namespace IEdgeGames {

    public class PUNAddressableScene {

        internal const string CurrentSceneProperty = "curScn";
        internal const string CurrentScenePropertyLoadAsync = "curScnLa";

        // for asynchronous network synched loading.
        private static AsyncOperation _AsyncLevelLoadingOperation;

        /// <summary>Internally used to flag if the message queue was disabled by a "scene sync" situation (to re-enable it).</summary>
        internal static bool loadingLevelAndPausedNetwork = false;

        public static LoadBalancingClient NetworkingClient;

        /// <summary>This method wraps loading a level asynchronously and pausing network messages during the process.</summary>
        /// <remarks>
        /// While loading levels in a networked game, it makes sense to not dispatch messages received by other players.
        /// LoadLevel takes care of that by setting PhotonNetwork.IsMessageQueueRunning = false until the scene loaded.
        ///
        /// To sync the loaded level in a room, set PhotonNetwork.AutomaticallySyncScene to true.
        /// The Master Client of a room will then sync the loaded level with every other player in the room.
        /// Note that this works only for a single active scene and that reloading the scene is not supported.
        /// The Master Client will actually reload a scene but other clients won't.
        ///
        /// You should make sure you don't fire RPCs before you load another scene (which doesn't contain
        /// the same GameObjects and PhotonViews).
        ///
        /// LoadLevel uses SceneManager.LoadSceneAsync().
        ///
        /// Check the progress of the LevelLoading using PhotonNetwork.LevelLoadingProgress.
        ///
        /// Calling LoadLevel before the previous scene finished loading is not recommended.
        /// If AutomaticallySyncScene is enabled, PUN cancels the previous load (and prevent that from
        /// becoming the active scene). If AutomaticallySyncScene is off, the previous scene loading can finish.
        /// In both cases, a new scene is loaded locally.
        /// </remarks>
        /// <param name='levelNumber'>
        /// Build-index number of the level to load. When using level numbers, make sure they are identical on all clients.
        /// </param>
        public static void LoadLevel(int levelNumber) {
            if (PhotonHandler.AppQuits) {
                return;
            }

            if (PhotonNetwork.AutomaticallySyncScene) {
                SetLevelInPropsIfSynced(levelNumber);
            }

            PhotonNetwork.IsMessageQueueRunning = false;
            loadingLevelAndPausedNetwork = true;
            _AsyncLevelLoadingOperation = SceneManager.LoadSceneAsync(levelNumber, LoadSceneMode.Single);
        }

        internal static void SetLevelInPropsIfSynced(object levelId) {
            if (!PhotonNetwork.AutomaticallySyncScene || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) {
                return;
            }
            if (levelId == null) {
                Debug.LogError("Parameter levelId can't be null!");
                return;
            }


            // check if "current level" is already set in the room properties (then we don't set it again)
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CurrentSceneProperty)) {
                object levelIdInProps = PhotonNetwork.CurrentRoom.CustomProperties[CurrentSceneProperty];
                //Debug.Log("levelId (to set): "+ levelId + " levelIdInProps: " + levelIdInProps + " SceneManagerHelper.ActiveSceneName: "+ SceneManagerHelper.ActiveSceneName);

                if (levelId.Equals(levelIdInProps)) {
                    //Debug.LogWarning("The levelId equals levelIdInProps. Don't set property again.");
                    return;
                }
                else {
                    // if the new levelId does not equal the level in properties, there is a chance that build index and scene name refer to the same scene.
                    // as Unity does not provide all scenes with build index, we only check for the currently loaded scene (with a high chance this is the correct one).
                    int scnIndex = SceneManagerHelper.ActiveSceneBuildIndex;
                    string scnName = SceneManagerHelper.ActiveSceneName;

                    if ((levelId.Equals(scnIndex) && levelIdInProps.Equals(scnName)) || (levelId.Equals(scnName) && levelIdInProps.Equals(scnIndex))) {
                        //Debug.LogWarning("The levelId and levelIdInProps refer to the same scene. Don't set property for it.");
                        return;
                    }
                }
            }


            // if the new levelId does not match the current room-property, we can cancel existing loading (as we start a new one)
            if (_AsyncLevelLoadingOperation != null) {
                if (!_AsyncLevelLoadingOperation.isDone) {
                    Debug.LogWarning("PUN cancels an ongoing async level load, as another scene should be loaded. Next scene to load: " + levelId);
                }

                _AsyncLevelLoadingOperation.allowSceneActivation = false;
                _AsyncLevelLoadingOperation = null;
            }


            // current level is not yet in props, or different, so this client has to set it
            var setScene = new PhotonHashtable();

            if (levelId is int @int)
                setScene[CurrentSceneProperty] = @int;
            else if (levelId is string @string)
                setScene[CurrentSceneProperty] = @string;
            else
                Debug.LogError("Parameter levelId must be int or string!");

            PhotonNetwork.CurrentRoom.SetCustomProperties(setScene);
            SendAllOutgoingCommands(); // send immediately! because: in most cases the client will begin to load and pause sending anything for a while
        }

        /// <summary>
        /// Can be used to immediately send the RPCs and Instantiates just called, so they are on their way to the other players.
        /// </summary>
        /// <remarks>
        /// This could be useful if you do a RPC to load a level and then load it yourself.
        /// While loading, no RPCs are sent to others, so this would delay the "load" RPC.
        /// You can send the RPC to "others", use this method, disable the message queue
        /// (by IsMessageQueueRunning) and then load.
        /// </remarks>
        public static void SendAllOutgoingCommands() {
            if (!VerifyCanUseNetwork()) {
                return;
            }

            while (NetworkingClient.LoadBalancingPeer.SendOutgoingCommands()) {
            }
        }


        /// <summary>
        /// Helper function which is called inside this class to erify if certain functions can be used (e.g. RPC when not connected)
        /// </summary>
        /// <returns></returns>
        private static bool VerifyCanUseNetwork() {
            if (PhotonNetwork.IsConnected) {
                return true;
            }

            Debug.LogError("Cannot send messages when not connected. Either connect to Photon OR use offline mode!");
            return false;
        }






        /// <summary>Internally used to detect the current scene and load it if PhotonNetwork.AutomaticallySyncScene is enabled.</summary>
        internal static void LoadLevelIfSynced() {
            if (!PhotonNetwork.AutomaticallySyncScene || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) {
                return;
            }

            // check if "current level" is set in props
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CurrentSceneProperty)) {
                return;
            }

            // if loaded level is not the one defined by master in props, load that level
            object sceneId = PhotonNetwork.CurrentRoom.CustomProperties[CurrentSceneProperty];
            if (sceneId is int) {
                if (SceneManagerHelper.ActiveSceneBuildIndex != (int)sceneId) {
                    PhotonNetwork.LoadLevel((int)sceneId);
                }
            }
            else if (sceneId is string) {
                if (SceneManagerHelper.ActiveSceneName != (string)sceneId) {
                    PhotonNetwork.LoadLevel((string)sceneId);
                }
            }
        }


        private static void OnOperation(OperationResponse opResponse) {
            switch (opResponse.OperationCode) {
                case OperationCode.GetRegions:
                    /*if (opResponse.ReturnCode != 0) {
                        if (PhotonNetwork.LogLevel >= PunLogLevel.Full) {
                            Debug.Log("OpGetRegions failed. Will not ping any. ReturnCode: " + opResponse.ReturnCode);
                        }
                        return;
                    }
                    if (ConnectMethod == ConnectMethod.ConnectToBest) {
                        string previousBestRegionSummary = PhotonNetwork.BestRegionSummaryInPreferences;

                        if (PhotonNetwork.LogLevel >= PunLogLevel.Informational) {
                            Debug.Log("PUN got region list. Going to ping minimum regions, based on this previous result summary: " + previousBestRegionSummary);
                        }
                        NetworkingClient.RegionHandler.PingMinimumOfRegions(OnRegionsPinged, previousBestRegionSummary);
                    }*/
                    break;
                case OperationCode.JoinGame:
                    if (PhotonNetwork.Server == ServerConnection.GameServer) {
                        LoadLevelIfSynced();
                    }
                    break;
            }
        }
    }
}
