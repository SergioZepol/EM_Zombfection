using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Netcode;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    NetworkManager _NetworkManager;

    void OnGUI()
    {
        int width = 300;
        int height = 300;
        int x = (Screen.width - width) / 2 + 50;
        int y = (Screen.height - height) / 2;

        GUILayout.BeginArea(new Rect(x, y, width, height));
        if (!_NetworkManager.IsClient && !_NetworkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
            SceneManager.LoadScene("GameScene"); // Cambia "MainScene" por el nombre de tu escena principal
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) StartHost();
        if (GUILayout.Button("Client")) StartClient();
        if (GUILayout.Button("Server")) StartServer();
    }

    void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    void StatusLabels()
    {
        var mode = _NetworkManager.IsHost ?
            "Host" : _NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            _NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}
