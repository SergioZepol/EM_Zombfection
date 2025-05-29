using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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

    // Posiciones de inicio/spawn de los jugadores en la partida
    GameObject startPos;
    GameObject startPos1;
    GameObject startPos2;
    GameObject startPos3;
    GameObject startPos4;

    // Número máximo de jugadores permitidos
    public int numPlayers = 4;

    // Instancia estática del GameManager
    public static GameManager Instance { get; private set; }

    #endregion

    #region Unity Callbacks

    void Start()
    {
        // Cacheo del NetworkManager
        //_networkManager = NetworkManager.Singleton;

        // El humano va a ser de entre la lista de prefabs al jugador
        _human = _networkManager.NetworkConfig.Prefabs.Prefabs[0].Prefab;

        // Eventos de inicio de servidor y cliente conectado
        _networkManager.OnServerStarted += onServerStarted;
        _networkManager.OnClientConnectedCallback += onClientConnected;
        _networkManager.OnClientDisconnectCallback += onClientDisconnect;

        //if (NetworkManager.Singleton.IsServer)
        //{
        //    NetworkManager.Singleton.SceneManager.OnLoadComplete += OnClientFinishedLoadingScene;
        //}
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

            // Spawn del jugador
           // var playerObject = Instantiate(_human);
           // NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
           // networkObject.SpawnAsPlayerObject(obj);

            /*
            Player player = playerObject.GetComponent<Player>();
            player.ID = obj;
            */
        }
    }

    private void OnClientFinishedLoadingScene(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (sceneName == "GameScene")
        {
            Debug.Log($"Cliente {clientId} ha cargado GameScene. Haciendo spawn.");

            var playerObject = Instantiate(_human);
            var networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId);
        }
        /*
        if (sceneName == "GameScene")
        {
            Debug.Log($"Cliente {clientId} ha cargado GameScene. Haciendo spawn.");

            // Encontrar LevelBuilder en la escena
            LevelBuilder levelBuilder = FindObjectOfType<LevelBuilder>();
            if (levelBuilder == null)
            {
                Debug.LogError("No se encontró LevelBuilder en la escena.");
                return;
            }

            List<Vector3> humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();

            if (nextSpawnIndex >= humanSpawnPoints.Count)
            {
                Debug.LogWarning("No quedan puntos de spawn disponibles.");
                return;
            }

            Vector3 spawnPos = humanSpawnPoints[nextSpawnIndex++];

            // Instanciar jugador en esa posición
            GameObject playerObject = Instantiate(_human, spawnPos, Quaternion.identity);
            var networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId);
        }*/
    }

    // Evento cuando un cliente se ha desconectado
    private void onClientDisconnect(ulong obj)
    {
        // Solo durante la partida, si alguien se desconecta, el servidor determina que se ha ido y se encarga de modificar las variables
        if (_networkManager.IsServer)
        {
            clientes.Value -= 1;
            Debug.Log("Clientes conectados: " + clientes.Value);
            /*
            if (raceStartedNT.Value)
            { // Si solo queda un jugador, fin de la partida
                if (clientes.Value == 1)
                {
                    EndGame();
                }
            }
            */
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer && IsOwner) 
        {
            TestServerRpc(0, NetworkObjectId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TestClientRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"El cliente recibe el RPC #{value} en NetworkObject #{sourceNetworkObjectId}");
        if (IsOwner) 
        {
            TestServerRpc(value + 1, sourceNetworkObjectId);
        }
    }

    [Rpc(SendTo.Server)]
    void TestServerRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"El server recibe el RPC #{value} en NetworkObject #{sourceNetworkObjectId}");
        TestClientRpc(value, sourceNetworkObjectId);
    }



    #endregion

    #region Métodos Públicos

    // Comprueba si el número de jugadores listos es igual al número total de clientes conectados
    public bool EqualsReadyConnected(int ready)
    {
        if (clientes.Value <= 1)
        {
            return false;
        }
        return ready == clientes.Value;
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
        RaceEnded();
        */
    }

    #endregion
}
