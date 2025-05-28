using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : NetworkBehaviour
{
    //[SerializeField]
    //NetworkManager _NetworkManager;
    int maxConnections = 4;
    string joinCode = "Enter room code...";
    public string joinName = "Player Name...";
    public bool mostrarBox = true;

    public NetworkVariable<int> readyPlayersNT = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool ready = false;

    public NetworkVariable<bool> gameStartedNT = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> endedGameNT = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public GameManager GameManager;

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

    private void Update()
    {
        if (gameStartedNT.Value)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }


    void OnGUI()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float areaWidth = 300;
        float areaHeight = 300;

        Rect areaGUImenu = new Rect((screenWidth - areaWidth) / 2 + 50, (screenHeight - areaHeight) / 2, areaWidth, areaHeight);
        Rect areaGUI = new Rect(10, 10, areaWidth, areaHeight);

        // Puedes usar esto si quieres controlar cuándo se muestra el box (igual que en el segundo código)
        bool mostrarBox = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;

        if (mostrarBox)
        {
            GUI.Box(areaGUImenu, "Menú");
        }

        GUILayout.BeginArea(mostrarBox ? areaGUImenu : areaGUI);
        GUILayout.Space(mostrarBox ? 40 : 0); // Añade espacio si es el menú
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            mostrarBox = false;
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) StartHost();
        if (GUILayout.Button("Client")) StartClient();

        // Campos de texto para código y nombre
        joinCode = GUILayout.TextField(joinCode);
        joinName = GUILayout.TextField(joinName);

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
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("Join code: " + joinCode);

        MostrarReadyButton();
    }

    private void MostrarReadyButton()
    {
        if (!gameStartedNT.Value || endedGameNT.Value)
        {
            if (GUILayout.Button(ready ? "Not Ready" : "Ready", GUILayout.Width(200)))
            {
                ready = !ready;
                ReadyServerRPC(ready);
            }

            GUILayout.Space(20); // Espacio entre secciones
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRPC(bool isReady)
    {
        if (isReady)
        {
            readyPlayersNT.Value += 1;
            Debug.Log("Jugadores listos: " + readyPlayersNT.Value + " de " + GameManager.clientes.Value);
            if (GameManager.EqualsReadyConnected(readyPlayersNT.Value))
            {
                Debug.Log("Todos los jugadores están listos.");
                gameStartedNT.Value = true;
            }
        }
        else
        {
            readyPlayersNT.Value -= 1;
            Debug.Log("Jugadores listos: " + readyPlayersNT.Value + " de " + GameManager.clientes.Value);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void EndGameServerRPC(bool end)
    {
        if (end)
        {
            endedGameNT.Value = true;
            gameStartedNT.Value = false;
        }
    }
}
