using TMPro;
using UnityEngine;

public class PlayerCanvasController : MonoBehaviour
{   
    [SerializeField] private Transform _playerCanvasTransform;
    [SerializeField] private TMP_Text _playerNameText;

    private void LateUpdate()
    {
        if(Camera.main == null) { return; }
        _playerCanvasTransform.transform.rotation = Camera.main.transform.rotation;
    }

    public void SetPlayerName(string playerName)
    {
        _playerNameText.text = playerName;
    } 
}
