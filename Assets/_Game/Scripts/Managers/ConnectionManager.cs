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
    private ulong _localClientId;
    private readonly HashSet<ulong> _connectedClientIds = new HashSet<ulong>();

    // Get Properties
    public ConnectionState ConnectionState => _connectionState;
    public ConnectionRole ConnectionRole => _connectionRole;
    public ConnectionEndReason ConnectionEndReason => _connectionEndReason;
    public IReadOnlyCollection<ulong> ConnectedClientIds => _connectedClientIds;

    [Header("References")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Debug")]
    [SerializeField] private bool _isRawConnectionEventLoggingEnabled;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            LogManager.LogError(this, "Another ConnectionManager already exists in the scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LogManager.ContextProvider = GetLogContext;
    }

    private void Start()
    {
        if (_networkManager == null)
        {
            LogManager.LogError(this, "NetworkManager Is Not Found.");
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
        LogManager.Log(this, $"State changed: {oldConnectionState} -> {newConnectionState} | Reason: {_connectionEndReason}");
        if(_connectionState == ConnectionState.NotConnected 
            && oldConnectionState != ConnectionState.NotConnected 
            && _connectionEndReason == ConnectionEndReason.None)
        {
            LogManager.LogWarning(this, 
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
            LogManager.LogWarning(this, $"Server start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogManager.LogWarning(this, 
                $"Server start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }
        
        _connectionEndReason = ConnectionEndReason.None;
        LogManager.Log(this, "Server start requested.");
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
            LogManager.LogWarning(this, 
                $"Host start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogManager.LogWarning(this, 
                $"Host start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }

        _connectionEndReason = ConnectionEndReason.None;
        LogManager.Log(this, "Host start requested.");
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
            LogManager.LogWarning(this, 
                $"Client start rejected: a session is already active (state: {_connectionState}).");
            return false; 
        }
        if(_networkManager.IsListening || _networkManager.ShutdownInProgress)
        {
            LogManager.LogWarning(this, 
                $"Client start rejected: NetworkManager is still running or shutting down (IsListening: {_networkManager.IsListening}, ShutdownInProgress: {_networkManager.ShutdownInProgress}).");
            return false;
        }

        _connectionEndReason = ConnectionEndReason.None;
        LogManager.Log(this, "Client start requested.");
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
            LogManager.Log(this, "End session ignored: there is no active session.");
            return;
        }

        _connectedClientIds.Clear();
        _localClientId = 0;
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
        if (_isRawConnectionEventLoggingEnabled)
        {
            string peerClientIdsText = connectionEventData.PeerClientIds.IsCreated ? string.Join(", ", connectionEventData.PeerClientIds) : "-";
            LogManager.Log(this,
                $"[Raw] {connectionEventData.EventType} | ClientId: {connectionEventData.ClientId} | " +
                $"LocalClientId: {networkManager.LocalClientId} | PeerClientIds: [{peerClientIdsText}]");
        }

        switch (connectionEventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                if(connectionEventData.ClientId == networkManager.LocalClientId)
                {
                    if (networkManager.IsServer)
                    {
                        // Host(me(Connected))
                        _connectedClientIds.Add(connectionEventData.ClientId);
                        _localClientId = connectionEventData.ClientId;
                        SetConnectionRole(ConnectionRole.Host);
                        SetConnectionState(ConnectionState.Connected);
                    }
                    else
                    {
                        // Client(me(Connected))
                        _connectedClientIds.UnionWith(connectionEventData.PeerClientIds);
                        _connectedClientIds.Add(connectionEventData.ClientId);
                        _localClientId = connectionEventData.ClientId;
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
                        LogManager.Log(this, 
                            $"Client {connectionEventData.ClientId} connected. Connected clients: {_connectedClientIds.Count}");
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
                        LogManager.Log(this, 
                            $"Client {connectionEventData.ClientId} connected. Connected clients: {_connectedClientIds.Count}");
                        OnClientConnected?.Invoke(connectionEventData.ClientId);
                    }
                }
                break;

            case ConnectionEvent.ClientDisconnected:
                if(connectionEventData.ClientId == networkManager.LocalClientId)
                {
                    // Host Or Client(Me(Disconnected))
                    _connectedClientIds.Clear();
                    _localClientId = 0;
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
                        LogManager.Log(this, 
                            $"Client {connectionEventData.ClientId} disconnected. Connected clients: {_connectedClientIds.Count}");
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
                        LogManager.Log(this, 
                            $"Client {connectionEventData.ClientId} disconnected. Connected clients: {_connectedClientIds.Count}");
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
            _localClientId = 0;
            SetConnectionRole(ConnectionRole.None);
            SetConnectionEndReason(ConnectionEndReason.ServerStopped);
            SetConnectionState(ConnectionState.NotConnected);
        }
    }

    private void NetworkManager_OnTransportFailure()
    {
        // Network Failiure
        _connectedClientIds.Clear();
        _localClientId = 0;
        SetConnectionRole(ConnectionRole.None);
        SetConnectionEndReason(ConnectionEndReason.NetworkError);
        SetConnectionState(ConnectionState.NotConnected);
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

    private string GetLogContext() => ConnectionRole switch 
    {
        ConnectionRole.None => "None",
        ConnectionRole.Server => "Server",
        ConnectionRole.Host => $"Host ({_localClientId})",
        ConnectionRole.Client => $"Client ({_localClientId})",
        _ => "Unknown" 
    };

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
            LogManager.ContextProvider = null;
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
