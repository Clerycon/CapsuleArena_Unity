using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private List<Transform> _allSpawnPoints;

    private List<Transform> _availableSpawnPoints = new List<Transform>();

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            LogManager.LogError(this, "Another SpawnManager already exists in the scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if(ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }

        ConnectionManager.Instance.SpawnPoseProvider = GetRandomSpawnPose;
    }

    public Pose? GetRandomSpawnPose(ulong clientId)
    {
        if(_availableSpawnPoints.Count == 0 && _allSpawnPoints != null)
        {
            _availableSpawnPoints.AddRange(_allSpawnPoints);
            LogManager.Log(this, "All spawn points used, refilling.");
        }

        if(_availableSpawnPoints.Count == 0)
        {
            LogManager.LogWarning(this, "No spawn points assigned.");
            return null;
        }

        int randomIndex = Random.Range(0, _availableSpawnPoints.Count);

        Transform selectedSpawnPoint = _availableSpawnPoints[randomIndex];
        LogManager.Log(this, $"Spawn point selected: {selectedSpawnPoint.name} {selectedSpawnPoint.position}");

        _availableSpawnPoints.RemoveAt(randomIndex);

        return new Pose(selectedSpawnPoint.position, selectedSpawnPoint.rotation);
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
            
        }

        if(ConnectionManager.Instance != null && ConnectionManager.Instance.SpawnPoseProvider == GetRandomSpawnPose)
        {
            ConnectionManager.Instance.SpawnPoseProvider = null;
        } 
        
    }
}
