using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
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
    public int numberOfRooms = 6;
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

    private void Update()
    {
        if (IsHost)
        {
            int minRooms = Mathf.Max(6, (int)Mathf.Pow(GameManager.Instance.clientes.Value, 2));
            if (numberOfRooms < minRooms)
            {
                numberOfRooms = minRooms;
            }
        }


    }


    void OnGUI()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float areaWidth = 300;
        float areaHeight = 300;

        Rect areaGUImenu = new Rect((screenWidth - areaWidth) / 2 + 50, (screenHeight - areaHeight) / 2, areaWidth, areaHeight);

        // Ocupa toda la pantalla con márgenes si quieres
        Rect areaGUI = new Rect(10, 10, screenWidth, screenHeight);

        // Puedes usar esto si quieres controlar cuándo se muestra el box (igual que en el segundo código)
        bool mostrarBox = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;
        bool enabled = false;

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
            /*
            if (!enabled)
            {
                Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
                canvas.GetComponent<Canvas>().enabled = true; // Desactiva el Canvas si ya es cliente o servidor
                enabled = true;
            }
            */
            GameHUD();
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {

        if (GUILayout.Button("Host") && joinName.Length < 30) StartHost();
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

        if (IsServer || IsHost)
        {
            if (GUILayout.Button("Tiempo", GUILayout.Width(200)))
            {
                GameManager.Instance.currentMode.Value = GameMode.Tiempo;
            }

            if (GUILayout.Button("Monedas", GUILayout.Width(200)))
            {
                GameManager.Instance.currentMode.Value = GameMode.Monedas;
            }
            // Solo muestra el botón "Ready" si ya se ha elegido un modo de juego
            if (GameManager.Instance.currentMode.Value == GameMode.Tiempo || GameManager.Instance.currentMode.Value == GameMode.Monedas)
            {
                if (GUILayout.Button(localPlayer.isReady.Value ? "Not Ready" : "Ready", GUILayout.Width(200)))
                {
                    // Llama al ServerRpc para cambiar el estado
                    localPlayer.SetReadyServerRpc(!localPlayer.isReady.Value);
                }
            }
        }
        else
        {
            if (GUILayout.Button(localPlayer.isReady.Value ? "Not Ready" : "Ready", GUILayout.Width(200)))
            {
                // Llama al ServerRpc para cambiar el estado
                localPlayer.SetReadyServerRpc(!localPlayer.isReady.Value);
            }
        }

        if (IsHost || IsServer)
        {
            // Sliders para enteros
            GUILayout.Label("Number of Rooms: " + numberOfRooms, GUILayout.Width(300));
            numberOfRooms = (int)GUILayout.HorizontalSlider(numberOfRooms, 6, 50, GUILayout.Width(300));

            GUILayout.Label("Room Width: " + roomWidth, GUILayout.Width(300));
            roomWidth = (int)GUILayout.HorizontalSlider(roomWidth, 5, 20, GUILayout.Width(300));

            GUILayout.Label("Room Length: " + roomLenght, GUILayout.Width(300));
            roomLenght = (int)GUILayout.HorizontalSlider(roomLenght, 5, 20, GUILayout.Width(300));

            GUILayout.Label("Items Density: " + itemsDensity.ToString("F1"), GUILayout.Width(300));
            itemsDensity = GUILayout.HorizontalSlider(itemsDensity, 5f, 30f, GUILayout.Width(300));

            if (GameManager.Instance.currentMode.Value == GameMode.Monedas)
            {
                GUILayout.Label("Coins Density: " + coinsDensity.ToString("F1"), GUILayout.Width(300));
                coinsDensity = GUILayout.HorizontalSlider(coinsDensity, 5f, 30f, GUILayout.Width(300));
            }
        }
    }

    private void GameHUD()
    {
        if (GameManager == null)
            return;

        GUILayout.BeginHorizontal();

        // Mostrar solo la info del modo activo
        var mode = GameManager.Instance.currentMode.Value;

        if (mode == GameMode.Tiempo)
        {
            GUILayout.Label($"Tiempo restante: <{GameManager.Instance.TiempoRestante.Value.ToString("D2")}>", GUILayout.Width(200));
        }

        if (mode == GameMode.Monedas)
        {
            GUILayout.Label($"Monedas recogidas: <{GameManager.Instance.MonedasRestantes.Value.ToString("D2")}>", GUILayout.Width(200));
        }

        GUILayout.Label($"Zombis: <{GameManager.Instance.ZombiesVivos.Value.ToString("D2")}>", GUILayout.Width(100));
        GUILayout.Label($"Humanos: <{GameManager.Instance.HumanosVivos.Value.ToString("D2")}>", GUILayout.Width(100));

        GUILayout.EndHorizontal();

        if (GameManager.Instance.endHumanWin.Value)
        {
            GUILayout.Label("LOS HUMANOS HAN GANADO");
            if (IsHost)
            {
                if (GUILayout.Button("Volver al Menú", GUILayout.Width(200)))
                {
                    GameManager.Instance.ResetGameState();
                }
            }
        }

        if (GameManager.Instance.endZombieWin.Value)
        {
            GUILayout.Label("LOS ZOMBIES HAN GANADO");
            if (IsHost)
            {
                if (GUILayout.Button("Volver al Menú", GUILayout.Width(200)))
                {
                    GameManager.Instance.ResetGameState();
                }
            }
        }

    }


}
