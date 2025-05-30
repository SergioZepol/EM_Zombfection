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
    public int numberOfRooms = 4;
    public int roomWidth = 9;
    public int roomLenght = 9;
    public float itemsDensity = 20;
    public float coinsDensity = 20;

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
        else if ((NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) && SceneManager.GetActiveScene().name != "GameScene")
        {
            mostrarBox = false;
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            canvas.GetComponent<Canvas>().enabled = false; // Desactiva el Canvas si ya es cliente o servidor
            StatusLabels();
            GameConfig();
        }
        else
        {
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            canvas.GetComponent<Canvas>().enabled = true; // Desactiva el Canvas si ya es cliente o servidor
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        
        if (GUILayout.Button("Host") && joinName.Length <30) StartHost();
        if (GUILayout.Button("Client") && joinName.Length < 30) StartClient();

        // Campos de texto para código y nombre
        joinCode = GUILayout.TextField(joinCode);
        joinName = GUILayout.TextField(joinName);

    }
    private async void StartHost()
    {
        if (joinName == "Player Name..." || joinName == "" || joinName.Contains("Client"))
        {
            joinName = "HostPlayer"; // Nombre por defecto si no se ingresa uno
        }
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
        if (joinName == "Player Name..." || joinName == "")
        {
            joinName = "ClientPlayer" + Random.Range(1, 1000); // Nombre por defecto si no se ingresa uno
        }

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
    }

    private void GameConfig()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<PlayerState>();
        if (localPlayer == null) return;

        if (GUILayout.Button(localPlayer.isReady.Value ? "Not Ready" : "Ready", GUILayout.Width(200)))
        {
            // Llama al ServerRpc para cambiar el estado
            localPlayer.SetReadyServerRpc(!localPlayer.isReady.Value);
        }

        if (IsHost || IsServer)
        {
            // Sliders para enteros
            GUILayout.Label("Number of Rooms: " + numberOfRooms);
            numberOfRooms = (int)GUILayout.HorizontalSlider(numberOfRooms, 4, 50);

            GUILayout.Label("Room Width: " + roomWidth);
            roomWidth = (int)GUILayout.HorizontalSlider(roomWidth, 5, 20);

            GUILayout.Label("Room Length: " + roomLenght);
            roomLenght = (int)GUILayout.HorizontalSlider(roomLenght, 5, 20);

            // Sliders para floats
            GUILayout.Label("Items Density: " + itemsDensity.ToString("F1"));
            itemsDensity = GUILayout.HorizontalSlider(itemsDensity, 5f, 30f);

            GUILayout.Label("Coins Density: " + coinsDensity.ToString("F1"));
            coinsDensity = GUILayout.HorizontalSlider(coinsDensity, 5f, 30f);
        }
    }
}
