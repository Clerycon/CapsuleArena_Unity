using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{   
    // Singleton Pattern
    public static ConnectionManager Instance { get; private set; }
    
    // Enums
    private ConnectionState _connectionState = ConnectionState.NotConnected;
    private ConnectionRole _connectionRole = ConnectionRole.None;
    private ConnectionEndReason _connectionEndReason = ConnectionEndReason.None;

    // Events
    public event Action<ConnectionState> OnConnectionStateChanged;
    public event Action<ulong> OnClientConnected;
    public event Action<ulong> OnClientDisconnected;

    // Connected Client Ids
    private readonly HashSet<ulong> _connectedClientIds = new HashSet<ulong>();

    // Get Properties
    public ConnectionState ConnectionState => _connectionState;
    public ConnectionRole ConnectionRole => _connectionRole;
    public ConnectionEndReason ConnectionEndReason => _connectionEndReason;
    public IReadOnlyCollection<ulong> ConnectedClientIds => _connectedClientIds;

    [Header("References")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Settings")]
    [SerializeField] private bool _isLoggingEnabled;
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_networkManager == null)
        {
            Debug.LogError("NetworkManager Is Not Found!");
            return;
        }

        _networkManager.OnServerStarted += NetworkManager_OnServerStarted;
        _networkManager.OnConnectionEvent += NetworkManager_OnConnectionEvent;
        _networkManager.OnServerStopped += NetworkManager_OnServerStopped;
        _networkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
    }
    private void SetConnectionState(ConnectionState newConnectionState)
    {
        if(_connectionState == newConnectionState) { return; }
        ConnectionState oldConnectionState = _connectionState;
        _connectionState = newConnectionState;
        LogConnectionManager($"State changed: {oldConnectionState} -> {newConnectionState} | Reason: {_connectionEndReason}");
        if(_connectionState == ConnectionState.NotConnected 
            && oldConnectionState != ConnectionState.NotConnected 
            && _connectionEndReason == ConnectionEndReason.None)
        {
            LogWarningConnectionManager(
                $"Session ended without a reason (previous state: {oldConnectionState}). An end reason should be set before the state changes.");
        }
        OnConnectionStateChanged?.Invoke(_connectionState);
    }

    private void SetConnectionRole(ConnectionRole newConnectionRole)
    {
        if(_connectionRole == newConnectionRole) { return; }
        _connectionRole = newConnectionRole;
    }

    private void SetConnectionEndReason(ConnectionEndReason connectionEndReason)
    {
        if(_connectionState == ConnectionState.NotConnected) { return; }
        _connectionEndReason = connectionEndReason;
    }

    public bool StartServerSession()
    {
        if(_connectionState != ConnectionState.NotConnected) 
        { 
            LogWarningConnectionManager($"Server start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogWarningConnectionManager($"Server start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }
        
        _connectionEndReason = ConnectionEndReason.None;
        LogConnectionManager("Server start requested.");
        SetConnectionState(ConnectionState.Connecting);
        bool isServerStarted = _networkManager.StartServer();
        if (!isServerStarted)
        {
            SetConnectionEndReason(ConnectionEndReason.StartFailed);
            SetConnectionState(ConnectionState.NotConnected);
        }
        return isServerStarted; 
    }
    public bool StartHostSession()
    {
        if(_connectionState != ConnectionState.NotConnected) 
        { 
            LogWarningConnectionManager($"Host start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogWarningConnectionManager($"Host start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }

        _connectionEndReason = ConnectionEndReason.None;
        LogConnectionManager("Host start requested.");
        SetConnectionState(ConnectionState.Connecting);
        bool isHostStarted = _networkManager.StartHost();
        if (!isHostStarted)
        {
            SetConnectionEndReason(ConnectionEndReason.StartFailed);
            SetConnectionState(ConnectionState.NotConnected);
        }
        return isHostStarted;
    }
    public bool StartClientSession()
    {
        if(_connectionState != ConnectionState.NotConnected) 
        { 
            LogWarningConnectionManager($"Client start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogWarningConnectionManager($"Client start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }

        _connectionEndReason = ConnectionEndReason.None;
        LogConnectionManager("Client start requested.");
        SetConnectionState(ConnectionState.Connecting);
        bool isClientStarted = _networkManager.StartClient();
        if (!isClientStarted)
        {
            SetConnectionEndReason(ConnectionEndReason.StartFailed);
            SetConnectionState(ConnectionState.NotConnected);
        }
        return isClientStarted;
    }

    public void EndSession()
    {
        if(_connectionState == ConnectionState.NotConnected)
        {
            LogConnectionManager("End session ignored: there is no active session.");
            return;
        }

        _connectedClientIds.Clear();
        SetConnectionRole(ConnectionRole.None);
        SetConnectionEndReason(ConnectionEndReason.SessionEnded);
        SetConnectionState(ConnectionState.NotConnected);
        _networkManager.Shutdown();
    }

    private void NetworkManager_OnServerStarted()
    {
        if(_networkManager.IsServer && !_networkManager.IsClient)
        {
            SetConnectionRole(ConnectionRole.Server);
            SetConnectionState(ConnectionState.Connected);
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
                        // Host(me(Connected))
                        
                        _connectedClientIds.Add(connectionEventData.ClientId);
                        SetConnectionRole(ConnectionRole.Host);
                        SetConnectionState(ConnectionState.Connected);
                    }
                    else
                    {
                        // Client(me(Connected))
                        _connectedClientIds.UnionWith(connectionEventData.PeerClientIds);
                        _connectedClientIds.Add(connectionEventData.ClientId);
                        SetConnectionRole(ConnectionRole.Client);
                        SetConnectionState(ConnectionState.Connected);
                    }
                }
                else
                {
                    // Server Or Host(Other(Im Getting an Info))
                    bool isClientIdAdded = _connectedClientIds.Add(connectionEventData.ClientId);
                    if (isClientIdAdded)
                    {
                        LogConnectionManager($"Client {connectionEventData.ClientId} connected. Connected clients: {_connectedClientIds.Count}");
                        OnClientConnected?.Invoke(connectionEventData.ClientId);
                    }
                }
                break;

            case ConnectionEvent.PeerConnected:
                if (!networkManager.IsServer)
                {
                    // Client(Other(Im Getting an Info))
                    bool isClientIdAdded = _connectedClientIds.Add(connectionEventData.ClientId);
                    if (isClientIdAdded)
                    {
                        LogConnectionManager($"Client {connectionEventData.ClientId} connected. Connected clients: {_connectedClientIds.Count}");
                        OnClientConnected?.Invoke(connectionEventData.ClientId);
                    }
                }
                break;

            case ConnectionEvent.ClientDisconnected:
                if(connectionEventData.ClientId == networkManager.LocalClientId)
                {
                    // Host Or Client(Me(Disconnected))
                    _connectedClientIds.Clear();
                    SetConnectionRole(ConnectionRole.None);
                    if(_connectionState == ConnectionState.Connecting)
                    {
                        SetConnectionEndReason(ConnectionEndReason.CouldNotConnect);
                    }
                    else if(_connectionState == ConnectionState.Connected)
                    {
                        SetConnectionEndReason(ConnectionEndReason.ConnectionLost);
                    }
                    SetConnectionState(ConnectionState.NotConnected);
                }
                else
                {
                    // Server Or Host(Other(Im Getting an Info))
                    bool isClientIdRemoved = _connectedClientIds.Remove(connectionEventData.ClientId);
                    if (isClientIdRemoved)
                    {
                        LogConnectionManager($"Client {connectionEventData.ClientId} disconnected. Connected clients: {_connectedClientIds.Count}");
                        OnClientDisconnected?.Invoke(connectionEventData.ClientId);
                    }
                }
                break;

            case ConnectionEvent.PeerDisconnected:
                if (!networkManager.IsServer)
                {
                    // Client(Other(Im Getting an Info))
                    bool isClientIdRemoved = _connectedClientIds.Remove(connectionEventData.ClientId);
                    if (isClientIdRemoved)
                    {
                        LogConnectionManager($"Client {connectionEventData.ClientId} disconnected. Connected clients: {_connectedClientIds.Count}");
                        OnClientDisconnected?.Invoke(connectionEventData.ClientId);
                    }
                }
                break;
        }
    }

    private void NetworkManager_OnServerStopped(bool isHost)
    {
        if (!isHost)
        {
            _connectedClientIds.Clear();
            SetConnectionRole(ConnectionRole.None);
            SetConnectionEndReason(ConnectionEndReason.ServerStopped);
            SetConnectionState(ConnectionState.NotConnected);
        }
    }

    private void NetworkManager_OnTransportFailure()
    {
        // Network Failiure
        _connectedClientIds.Clear();
        SetConnectionRole(ConnectionRole.None);
        SetConnectionEndReason(ConnectionEndReason.NetworkError);
        SetConnectionState(ConnectionState.NotConnected);
    }

    private void LogConnectionManager(string message)
    {
        if(!_isLoggingEnabled) { return; }
        Debug.Log($"[ConnectionManager][{_connectionRole}]  {message}");
    }

    private void LogWarningConnectionManager(string message)
    {
        if(!_isLoggingEnabled) { return; }
        Debug.LogWarning($"[ConnectionManager][{_connectionRole}]  {message}");
    }

    public string GetConnectionEndReasonText() => ConnectionEndReason switch
    {
        ConnectionEndReason.None => "Not connected",
        ConnectionEndReason.StartFailed => "Could not start the session",
        ConnectionEndReason.CouldNotConnect => "Could not connect to the server/host",
        ConnectionEndReason.ConnectionLost => "Connection lost",
        ConnectionEndReason.ServerStopped => "Server stopped",
        ConnectionEndReason.NetworkError => "Network error",
        ConnectionEndReason.SessionEnded => "Session ended",
        _ => "Unknown"

    };

    public string GetConnectionRoleText() => ConnectionRole switch
    {
        ConnectionRole.None => "Not connected",
        ConnectionRole.Server => "Server",
        ConnectionRole.Host => "Host",
        ConnectionRole.Client => "Client",
        _ => "Unknown"  
    };

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }

        if (_networkManager == null)
        {
            return;
        }

        _networkManager.OnServerStarted -= NetworkManager_OnServerStarted;
        _networkManager.OnConnectionEvent -= NetworkManager_OnConnectionEvent;
        _networkManager.OnServerStopped -= NetworkManager_OnServerStopped;
        _networkManager.OnTransportFailure -= NetworkManager_OnTransportFailure;
    }
}
