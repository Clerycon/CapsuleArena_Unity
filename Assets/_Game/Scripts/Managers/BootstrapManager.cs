using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene(Consts.SceneNames.MENU_SCENE);
    }
}
