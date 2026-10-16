using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _connectionPanel;
    [SerializeField] private Button _serverButton;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private Button _endSessionButton;
    [SerializeField] private TMP_Text _statusText;

    private void Awake()
    {
        _serverButton.onClick.AddListener(OnServerButtonClicked);
        _hostButton.onClick.AddListener(OnHostButtonClicked);
        _clientButton.onClick.AddListener(OnClientButtonClicked);
        _endSessionButton.onClick.AddListener(OnEndSessionButtonClicked);
    }

    private void Start()
    {
        if(ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }

        ConnectionManager.Instance.OnConnectionStateChanged += ConnectionManager_OnConnectionStateChanged;
        ConnectionManager_OnConnectionStateChanged(ConnectionManager.Instance.ConnectionState);
    }

    private void OnServerButtonClicked()
    {
        if (ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }
        ConnectionManager.Instance.StartServerSession();

    }

    private void OnHostButtonClicked()
    {
        if (ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }
        ConnectionManager.Instance.StartHostSession();
    }

    private void OnClientButtonClicked()
    {
        if (ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }
        ConnectionManager.Instance.StartClientSession();
    }

    private void OnEndSessionButtonClicked()
    {
        if (ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, "ConnectionManager instance not found. Start the game from the Bootstrap scene.");
            return;
        }
        ConnectionManager.Instance.EndSession();
    }

    private void ConnectionManager_OnConnectionStateChanged(ConnectionState connectionState)
    {
        switch (connectionState)
        {
            case ConnectionState.NotConnected:
                _connectionPanel.SetActive(true);
                _endSessionButton.gameObject.SetActive(false);
                SetButtonsInteractable(true);
                _statusText.text = $"Status: {ConnectionManager.Instance.GetConnectionEndReasonText()}";
                break;

            case ConnectionState.Connecting:
                _connectionPanel.SetActive(true);
                _endSessionButton.gameObject.SetActive(false);
                SetButtonsInteractable(false);
                _statusText.text = "Status: Connecting";
                break;

            case ConnectionState.Connected:
                _connectionPanel.SetActive(false);
                _endSessionButton.gameObject.SetActive(true);
                SetButtonsInteractable(false);
                _statusText.text = $"Status: {ConnectionManager.Instance.GetConnectionRoleText()}";
                break;
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        _serverButton.interactable = isInteractable;
        _hostButton.interactable = isInteractable;
        _clientButton.interactable = isInteractable;
    }

    private void OnDestroy()
    {
        if(ConnectionManager.Instance == null)
        {
            return;
        }

        ConnectionManager.Instance.OnConnectionStateChanged -= ConnectionManager_OnConnectionStateChanged;
    }
}
