using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private RectTransform _blackBackgroundTransform;
    [SerializeField] private Button _serverButton;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private TMP_Text _statusText;

    private HashSet<ulong> _connectedClientIds = new HashSet<ulong>();

    private void Awake()
    {
        _statusText.text = "Status: Not Connected";

        _serverButton.onClick.AddListener(OnServerButtonClicked);
        _hostButton.onClick.AddListener(OnHostButtonClicked);
        _clientButton.onClick.AddListener(OnClientButtonClicked);
    }

    private void OnEnable()
    {
        if(NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager Is Not Found!");
            return;
        }

        NetworkManager.Singleton.OnServerStarted += NetworkManager_OnServerStarted;
        NetworkManager.Singleton.OnConnectionEvent += NetworkManager_OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped += NetworkManager_OnServerStopped;
        NetworkManager.Singleton.OnTransportFailure += NetworkManager_OnTransportFailure;
    }

    

    private void OnServerButtonClicked()
    {
        _statusText.text = "Status: Creating Server";
        SetButtonsInteractable(false);
        if (!NetworkManager.Singleton.StartServer())
        {
            _statusText.text = "Status: Server Creation Failed";
            SetButtonsInteractable(true);
        }
    }

    private void OnHostButtonClicked()
    {
        _statusText.text = "Status: Creating Host";
        SetButtonsInteractable(false);
        if (!NetworkManager.Singleton.StartHost())
        {
            _statusText.text = "Status: Host Creation Failed";
            SetButtonsInteractable(true);
        }
    }

    private void OnClientButtonClicked()
    {
        _statusText.text = "Status: Connecting Server/Host";
        SetButtonsInteractable(false);
        if(!NetworkManager.Singleton.StartClient())
        {
            _statusText.text = "Status: Connection To Server/Host Failed";
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        _serverButton.interactable = isInteractable;
        _hostButton.interactable = isInteractable;
        _clientButton.interactable = isInteractable;
    }

    private void NetworkManager_OnServerStarted()
    {
        if(NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            _statusText.text = "Status: Server";
            _blackBackgroundTransform.gameObject.SetActive(false);
        }
    }

    private void NetworkManager_OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
    {
        switch (connectionEventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                if(connectionEventData.ClientId == networkManager.LocalClientId)
                {
                    if (networkManager.IsServer)
                    {
                        _statusText.text = "Status: Host";
                        _blackBackgroundTransform.gameObject.SetActive(false);
                        _connectedClientIds.Add(connectionEventData.ClientId);
                    }
                    else
                    {
                        _statusText.text = "Status: Client";
                        _blackBackgroundTransform.gameObject.SetActive(false);
                        _connectedClientIds.UnionWith(connectionEventData.PeerClientIds);
                        _connectedClientIds.Add(connectionEventData.ClientId);
                    }
                }
                else
                {
                    Debug.Log($"Client Is Connected With Id: {connectionEventData.ClientId}");
                    _connectedClientIds.Add(connectionEventData.ClientId);
                } 
                break;

            case ConnectionEvent.PeerConnected:
                if (!networkManager.IsServer)
                {
                    _connectedClientIds.Add(connectionEventData.ClientId);
                }
                break;
                

            case ConnectionEvent.ClientDisconnected:
                if(connectionEventData.ClientId == networkManager.LocalClientId)
                {
                    _statusText.text = "Status: Connection Was Lost";
                    _blackBackgroundTransform.gameObject.SetActive(true);
                    SetButtonsInteractable(true);
                    _connectedClientIds.Clear();
                }
                else
                {
                    Debug.Log($"Client Is Disconnected With Id: {connectionEventData.ClientId}");
                    _connectedClientIds.Remove(connectionEventData.ClientId);
                }
                break;

            case ConnectionEvent.PeerDisconnected:
                if (!networkManager.IsServer)
                {
                    _connectedClientIds.Remove(connectionEventData.ClientId);
                }
                break;


        }
    }

    private void NetworkManager_OnServerStopped(bool isHost)
    {
        if (!isHost)
        {
            _statusText.text = "Status: Connection Was Lost";
            _blackBackgroundTransform.gameObject.SetActive(true);
            SetButtonsInteractable(true);
            _connectedClientIds.Clear();
        }
    }

    private void NetworkManager_OnTransportFailure()
    {
        _statusText.text = "Status: Network Error";
        _blackBackgroundTransform.gameObject.SetActive(true);
        SetButtonsInteractable(true);
        _connectedClientIds.Clear();
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnServerStarted -= NetworkManager_OnServerStarted;
        NetworkManager.Singleton.OnConnectionEvent -= NetworkManager_OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped -= NetworkManager_OnServerStopped;
        NetworkManager.Singleton.OnTransportFailure -= NetworkManager_OnTransportFailure;
    }
}
