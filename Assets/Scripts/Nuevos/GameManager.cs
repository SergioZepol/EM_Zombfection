using UnityEngine;
using Unity.Netcode;
//using Cinemachine;

public class GameManager : NetworkBehaviour
{
    #region Variables

    // Referencia al NetworkManager
    NetworkManager _networkManager;

    // Prefabricado del coche
    GameObject _human;

    // Contador de clientes conectados
    public NetworkVariable<int> clientes = new NetworkVariable<int>();

    // Posiciones de inicio/spawn de los jugadores en la pista
    GameObject startPos;
    GameObject startPos1;
    GameObject startPos2;
    GameObject startPos3;
    GameObject startPos4;

    // N�mero m�ximo de jugadores permitidos
    public int numPlayers = 4;

    // Instancia est�tica del GameManager
    public static GameManager Instance { get; private set; }

    #endregion

    #region Unity Callbacks

    void Start()
    {
        // Cacheo del NetworkManager
        _networkManager = NetworkManager.Singleton;

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
        print("El servidor est� listo");
        clientes.Value = 0;
    }

    // Evento cuando un cliente se ha conectado
    private void onClientConnected(ulong obj)
    {
        // Solo si eres el servidor decides instanciar a los clientes
        if (_networkManager.IsServer)
        {
            clientes.Value += 1;
            Debug.Log("Clientes conectados: " + clientes.Value);

            // Spawn del jugador
            var playerObject = Instantiate(_human);
            NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(obj);

            /*
            Player player = playerObject.GetComponent<Player>();
            player.ID = obj;
            */

            // Selecci�n de circuito basado en NetVariable

            // Posiciones de inicio/spawn de los jugadores en pista
            startPos1 = GameObject.FindGameObjectWithTag("Pos1");
            startPos2 = GameObject.FindGameObjectWithTag("Pos2");
            startPos3 = GameObject.FindGameObjectWithTag("Pos3");
            startPos4 = GameObject.FindGameObjectWithTag("Pos4");

            // Dependiendo de qu� cliente sea, se establece una posici�n distinta
            switch (clientes.Value)
            {
                case 1:
                    startPos = startPos1;
                    break;
                case 2:
                    startPos = startPos2;
                    break;
                case 3:
                    startPos = startPos3;
                    break;
                default:
                    startPos = startPos4;
                    break;
            };
            playerObject.transform.position = startPos.GetComponent<Transform>().transform.position;
        }
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

    #endregion

    #region M�todos P�blicos

    // Comprueba si el n�mero de jugadores listos es igual al n�mero total de clientes conectados
    public bool EqualsReadyConnected(int ready)
    {
        if (clientes.Value <= 1)
        {
            return false;
        }
        return ready == clientes.Value;
    }

    // Devuelve el n�mero de clientes conectados
    public int GetClients()
    {
        return clientes.Value;
    }

    #endregion

    #region M�todos Privados

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
