using System.Linq;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using UnityEngine;

public class StartHostClient : MonoBehaviour
{
    private void Start()
    {
        if (CurrentPlayer.Tags.Contains("Host"))
        {
            NetworkManager.Singleton.StartHost();
        }
        else if (CurrentPlayer.Tags.Contains("Client"))
        {
            NetworkManager.Singleton.StartClient();
        }
    }
}
