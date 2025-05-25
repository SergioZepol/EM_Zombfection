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
    //declaraciones
    [SerializeField]
    NetworkManager _NetworkManager;
    int maxConnections = 4;
    string joinCode = null;

    public static UIManager Instance { get; private set; }

    //verifica una unica existencia de UIManager
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

    // Crea botones (host/client)
    // etiquetas de estado
    // cambio de escena al conectar
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

        //logica del host
        private async void StartHost()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        //Relay
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        TextEditor te = new TextEditor();
        te.text = joinCode;
        te.SelectAll();
        te.Copy();

        NetworkManager.Singleton.StartHost();
    }

        //logica del cliente
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

    /*
        private async void StartClient()
{
    // Verificar si se ingresó un código
    if (string.IsNullOrEmpty(joinCodeInputField.text))
    {
        Debug.LogError("Por favor ingresa un código de unión");
        return;
    }

    try {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(
            joinCode: joinCodeInputField.text);
            
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            new RelayServerData(joinAllocation, "dtls"));
            
        NetworkManager.Singleton.StartClient();
    }
    catch (Exception e) {
        Debug.LogError($"Error al conectar: {e.Message}");
        // Aquí podrías mostrar un mensaje en pantalla al jugador
    }
}
    */
    
    //muestra el estado
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
