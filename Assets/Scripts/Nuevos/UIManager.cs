using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    NetworkManager _NetworkManager;
    int maxConnections = 4;
    string joinCode = null;

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // Evita duplicados
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Persiste entre escenas
    }

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
            if (SceneManager.GetActiveScene().name != "GameScene")
            {
                SceneManager.LoadScene("GameScene");
            }
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) StartHost();
        if (GUILayout.Button("Client")) StartClient();
    }
        private async void StartHost()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        TextEditor te = new TextEditor();
        te.text = joinCode;
        te.SelectAll();
        te.Copy();

        NetworkManager.Singleton.StartHost();
    }


        private async void StartClient()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        NetworkManager.Singleton.StartClient();
    }
    

    void StatusLabels()
    {
        var mode = _NetworkManager.IsHost ?
            "Host" : _NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            _NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("Join code: " + joinCode);
    }
}
