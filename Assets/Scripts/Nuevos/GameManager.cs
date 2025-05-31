using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
//using Cinemachine;

public class GameManager : NetworkBehaviour
{
    #region Variables

    // Referencia al NetworkManager
    public NetworkManager _networkManager;

    // Prefabricado del personaje
    [SerializeField] private GameObject _human;
    //private int nextSpawnIndex = 0;

    // Contador de clientes conectados
    public NetworkVariable<int> clientes = new NetworkVariable<int>();
    public NetworkVariable<bool> endHumanWin = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> endZombieWin = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // Instancia estática del GameManager
    public static GameManager Instance { get; private set; }

    public NetworkVariable<GameMode> currentMode = new NetworkVariable<GameMode>(GameMode.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> MonedasRestantes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TiempoRestante = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> ZombiesVivos = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> HumanosVivos = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    #region Unity Callbacks

    void Start()
    {
        // El humano va a ser de entre la lista de prefabs al jugador
        _human = _networkManager.NetworkConfig.Prefabs.Prefabs[0].Prefab;

        // Eventos de inicio de servidor y cliente conectado
        _networkManager.OnServerStarted += onServerStarted;
        _networkManager.OnClientConnectedCallback += onClientConnected;
        _networkManager.OnClientDisconnectCallback += onClientDisconnect;
    }

    void Awake()
    {
        // Si no se ha instanciado el GameManager, se instancia y se marca para no destruirse al cargar una nueva escena
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
 
        }
        // Si ya existe una instancia de GameManager, se destruye este objeto duplicado
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Eventos del Servidor y Cliente

    // Evento cuando el servidor se ha iniciado
    private void onServerStarted()
    {
        print("El servidor está listo");
        clientes.Value = 0;
    }

    // Evento cuando un cliente se ha conectado
    private void onClientConnected(ulong obj)
    {
        // Solo si eres el servidor decides instanciar a los clientes
        Debug.Log("!IsServer: Clientes conectados: " + clientes.Value);
        if (_networkManager.IsServer)
        {
            clientes.Value += 1;
            Debug.Log("Clientes conectados: " + clientes.Value);

            //Spawn del jugador
            var playerObject = Instantiate(_human);
            NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(obj);
        }
    }

    // Evento cuando un cliente se ha desconectado
    private void onClientDisconnect(ulong clientId)
    {
        StartCoroutine(HandleDisconnect());
    }

    private IEnumerator HandleDisconnect()
    {
        // Espera un frame (puedes aumentar a 0.1f si sigue fallando)
        yield return null;

        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        int humanosVivos = 0;
        int zombiesVivos = 0;

        foreach (var player in allPlayers)
        {
            if (player.name.Contains("character-human"))
            {
                humanosVivos++;
            }
            else if (player.name.Contains("character-orc"))
            {
                zombiesVivos++;
            }
        }

        GameManager.Instance.ZombiesVivos.Value = zombiesVivos;
        GameManager.Instance.HumanosVivos.Value = humanosVivos;
        Debug.Log($"Humanos vivos: {humanosVivos}, Orcos vivos: {zombiesVivos}");

        if (zombiesVivos == 0)
        {
            Debug.Log("No quedan orcos. Los humanos ganan.");
            endHumanWin.Value = true;
        }
        else if (humanosVivos == 0)
        {
            Debug.Log("No quedan humanos. Los orcos ganan.");
            endZombieWin.Value = true;
        }

        clientes.Value = Mathf.Max(0, clientes.Value - 1);
        Debug.Log("Clientes conectados: " + clientes.Value);
    }


    #endregion

    #region Métodos Públicos

    public void CheckAllReady()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClientsList.Count < 2) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerState>();
            if (player == null || !player.isReady.Value)
                return; // Al menos uno no está listo
        }

        // Todos están listos, cambiamos de escena

        StartCoroutine(DespawnAndLoadScene());
    }

    private IEnumerator DespawnAndLoadScene()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            if (player.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Despawn();
            }
        }

        // Esperar 1 frame (mínimo)
        yield return null;

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    // Devuelve el número de clientes conectados
    public int GetClients()
    {
        print($"Hay {clientes} clientes conectados");
        return clientes.Value;
    }

    #endregion

    #region Métodos Privados

    // Finaliza la partida
    public void EndGame()
    {
        Debug.Log("Fin de Partida: Un jugador restante");
        /*
        GameEnded();
        */
    }

    public void ResetGameState()
    {
        Debug.Log("Reiniciando el estado del juego...");

        // Reinicia todas las variables relevantes

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                var playerState = playerObject.GetComponent<PlayerState>();
                if (playerState != null)
                {
                    playerState.isReady.Value = false;
                }
            }
        }

        endHumanWin.Value = false;
        endZombieWin.Value = false;
        HumanosVivos.Value = 0;
        ZombiesVivos.Value = 0;
        MonedasRestantes.Value = 0;
        TiempoRestante.Value = 0;
        currentMode.Value = GameMode.None;

        NetworkManager.Singleton.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);




    }
    #endregion
}
