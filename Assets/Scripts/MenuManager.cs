using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    //Cargar el UI Manager
    public GameObject UIManager;

    public void Awake()
    {
        Time.timeScale = 1f; // Aseg�rate de que el tiempo est� restaurado al cargar la escena
    }

    public void StartGame()
    {
        UIManager.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
