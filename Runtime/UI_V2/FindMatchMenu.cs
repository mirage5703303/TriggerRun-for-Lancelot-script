using System;
using System.Collections.Generic;
using System.Linq;
using IEdgeGames;
using Michsky.UI.Shift;
using Photon.Pun;
using Photon.Realtime;
using TrickCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class FindMatchMenu : UIMenu
{
    public RectTransform GameModeSelector;
    public Dictionary<GameMode, Button> GameModeButtons;
    public HorizontalSelector Selector;
    public Image MapSprite;
    public UIPanelLobby PanelLobby;
    
    protected override void AddressablesStart()
    {
        base.AddressablesStart();
        
        foreach (var pair in GameModeButtons)
        {
            pair.Value.onClick.AddListener(() => FindMatch(pair.Key));
        }
        int sceneCount = SceneManager.sceneCountInBuildSettings;     
        string[] scenes = new string[sceneCount];

        for( int i = 0; i < sceneCount; i++ )
        {
            scenes[i] = SceneUtility.GetScenePathByBuildIndex(i);
        }

        var sceneList = scenes.ToList();
        
        Selector.itemList = ProjectParameters.Maps.Where(info => sceneList.Contains(info.map.Path)).Select(info =>
        {
            var item = new HorizontalSelector.Item()
            {
                itemTitle = info.map.Name
            };
            item.onValueChanged.AddListener(() => Call(info));
            return item;
        }).ToList();
        
        MapSprite.sprite = ProjectParameters.Maps.First().icon;
        Matchmaking.SelectedMap = ProjectParameters.Maps.First().map;

        Selector.UpdateUI();
        
        GameModeSelector.gameObject.SetActive(true);
        PanelLobby.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        Matchmaking.OnConnectToMaster += MatchmakingOnOnConnectToMaster;
        Matchmaking.OnDisconnect += MatchmakingOnOnDisconnect;
        Matchmaking.OnCancelMatch += MatchmakingOnOnCancelMatch;
        Matchmaking.OnFillRoom += MatchmakingOnOnFillRoom;
        Matchmaking.OnRoomReady += MatchmakingOnOnRoomReady;
        Matchmaking.OnPlayerReady += MatchmakingOnOnPlayerReady;
        Matchmaking.OnBeginLoadLevel += MatchmakingOnOnBeginLoadLevel;
        Matchmaking.OnPlayerJoined += MatchmakingOnOnPlayerJoined;
        Matchmaking.OnPlayerLeft += MatchmakingOnOnPlayerLeft;
        Matchmaking.OnPlayerEnteredRoomEvent += MatchmakingOnOnPlayerEnteredRoomEvent;
    }

    private void OnDisable()
    {
        Matchmaking.OnConnectToMaster -= MatchmakingOnOnConnectToMaster;
        Matchmaking.OnDisconnect -= MatchmakingOnOnDisconnect;
        Matchmaking.OnCancelMatch -= MatchmakingOnOnCancelMatch;
        Matchmaking.OnFillRoom -= MatchmakingOnOnFillRoom;
        Matchmaking.OnRoomReady -= MatchmakingOnOnRoomReady;
        Matchmaking.OnPlayerReady -= MatchmakingOnOnPlayerReady;
        Matchmaking.OnBeginLoadLevel -= MatchmakingOnOnBeginLoadLevel;
        Matchmaking.OnPlayerJoined -= MatchmakingOnOnPlayerJoined;
        Matchmaking.OnPlayerLeft -= MatchmakingOnOnPlayerLeft;
        Matchmaking.OnPlayerEnteredRoomEvent -= MatchmakingOnOnPlayerEnteredRoomEvent;
    }

    private void Call(MapInfo map)
    {
        MapSprite.sprite = map.icon;
        Matchmaking.SelectedMap = map.map;
    }

    private void MatchmakingOnOnPlayerEnteredRoomEvent(Player obj)
    {
        Debug.Log("[MatchmakingOnOnPlayerEnteredRoomEvent]");
        
        PanelLobby.UpdateLobby(PhotonNetwork.CurrentRoom.Players, false);
    }

    private void MatchmakingOnOnPlayerLeft(Dictionary<int, Player> obj)
    {
        Debug.Log("[MatchmakingOnOnPlayerLeft]");
    }

    private void MatchmakingOnOnPlayerJoined(Dictionary<int, Player> obj)
    {
        Debug.Log("[MatchmakingOnOnPlayerJoined]");
        
        PanelLobby.UpdateLobby(obj, false);
    }

    private void MatchmakingOnOnBeginLoadLevel(int i)
    {
        Debug.Log("[MatchmakingOnOnBeginLoadLevel]");
        UIManager.Instance.ShowOnly<LoadingMenu>().WaitForSceneLoad(null, i, () =>
        {
            UIManager.Instance.ShowOnly<GameMenu>().FadeIn();
        });
    }

    private void MatchmakingOnOnPlayerReady(Player arg1, int arg2, int arg3)
    {
        Debug.Log("[MatchmakingOnOnPlayerReady]");
        
        PanelLobby.UpdateLobby(PhotonNetwork.CurrentRoom.Players, !Equals(arg1, PhotonNetwork.LocalPlayer));
    }

    private void MatchmakingOnOnRoomReady()
    {
        Debug.Log("[MatchmakingOnOnRoomReady]");
    }

    private void MatchmakingOnOnFillRoom()
    {
        Debug.Log("[MatchmakingOnOnFillRoom]");
        
        PanelLobby.UpdateLobby(PhotonNetwork.CurrentRoom.Players, true);
    }

    private void MatchmakingOnOnCancelMatch()
    {
        Debug.Log("[MatchmakingOnOnDisconnect]");
    }

    private void MatchmakingOnOnDisconnect(DisconnectCause obj)
    {
        Debug.Log("[MatchmakingOnOnDisconnect] : " + obj);
    }

    private void MatchmakingOnOnConnectToMaster()
    {
        Debug.Log("[MatchmakingOnOnConnectToMaster]");
    }

    public void FindMatch(GameMode gameMode)
    {
        UISelectMode.SelectedMode = gameMode;

        PanelLobby.UpdateLobby(new Dictionary<int, Player>(), false);
        if (gameMode == GameMode.PlaySolo)
        {
            Matchmaking.Connect(true, () => Matchmaking.PlaySolo());
        }
        else
        {
            Matchmaking.FindMatch(gameMode, true);
        }

        TrickVisualHelper.FadeOut(GameModeSelector);
        TrickVisualHelper.FadeIn(PanelLobby);
    }

    public override UIMenu Show()
    {
        TrickVisualHelper.FadeIn(GameModeSelector);
        TrickVisualHelper.FadeOut(PanelLobby);
        
        return base.Show();
    }

    public override void Hide()
    {
        base.Hide();
    }
}