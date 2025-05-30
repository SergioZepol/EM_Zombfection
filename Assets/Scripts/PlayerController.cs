using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    private TextMeshProUGUI coinText;
    /*
    public NetworkVariable<int> CoinsCollected = new NetworkVariable<int>();
    public NetworkVariable<bool> isZombie = new NetworkVariable<bool>();
    public NetworkVariable<FixedString64Bytes> uniqueID = new NetworkVariable<FixedString64Bytes>();
    */
    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // Añadir una propiedad para el estado del jugador
    public string uniqueID; // Añadir una propiedad para el identificador único

    [Header("Movement Settings")]
    public float moveSpeed = 5f;           // Velocidad de movimiento
    public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
    public Animator animator;              // Referencia al Animator
    public Transform cameraTransform;      // Referencia a la cámara

    private float horizontalInput;         // Entrada horizontal (A/D o flechas)
    private float verticalInput;           // Entrada vertical (W/S o flechas)

    public NetworkVariable<FixedString64Bytes> playerNameNT = new NetworkVariable<FixedString64Bytes> ("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> moveSpeedSync = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        playerNameNT.OnValueChanged += OnNameChanged;
    }
    void Start()
    {
    if (IsOwner)
    {
        Camera.main.GetComponent<CameraController>().player = this.transform;
    }
    // Buscar el objeto "CanvasPlayer" en la escena
    GameObject canvas = GameObject.Find("CanvasPlayer");

        if (!IsOwner && canvas != null)
        {
            canvas.SetActive(false); // Oculta el HUD a los demás jugadores
        }

        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                {
                    coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        UpdateCoinUI();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // El Owner (host o cliente) establece su nombre
            playerNameNT.Value = UIManager.Instance.GetComponent<UIManager>().joinName;
            Debug.Log("Jugador instanciado con nombre (owner): " + playerNameNT.Value);

            // Asegurar que la cámara esté asignada correctamente
            AssignCamera();
        }

        // Todos (owner o no) actualizan visualmente el nombre en pantalla
        OnNameChanged(playerNameNT.Value, playerNameNT.Value);
    }

    private void AssignCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraController camController = mainCam.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.player = this.transform;
                camController.enabled = true;
                Debug.Log("Cámara vinculada al jugador");
                cameraTransform = mainCam.transform; // Asignar la cámara al transform del jugador
            }
            else
            {
                Debug.LogError("CameraController no encontrado en la cámara principal");
            }
        }
        else
        {
            Debug.LogError("Camera.main es null al instanciar jugador");
        }
    }

    void Update()
    {

        if (!IsOwner) return;

        // Leer entrada del teclado
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // Mover el jugador
        //MovePlayer();
        if (!IsHost)
        {
            // Calcula la dirección en base a la cámara del cliente
            if (cameraTransform != null)
            {
                Vector3 moveDir = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
                moveDir.y = 0f;

                if (moveDir != Vector3.zero)
                {
                    SendDirectionToServerRpc(moveDir);
                }
            }
        }
        else
        {
            MovePlayer();
        }
        HandleAnimations();
    }

    [ServerRpc]
    void SendDirectionToServerRpc(Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.fixedDeltaTime);

        float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;
        transform.Translate(moveDirection * adjustedSpeed * Time.fixedDeltaTime, Space.World);

        BroadcastTransformClientRpc(transform.position, transform.rotation);
    }

    [ClientRpc]
    void BroadcastTransformClientRpc(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    void MovePlayer()
    {
        if (cameraTransform == null) { return; }

        // Calcular la dirección de movimiento en relación a la cámara
        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

        // Mover el jugador usando el Transform
        if (moveDirection != Vector3.zero)
        {
            // Calcular la rotación en Y basada en la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.fixedDeltaTime);

            // Ajustar la velocidad si es zombie
            float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;

            // Mover al jugador en la dirección deseada
            transform.Translate(moveDirection * adjustedSpeed * Time.fixedDeltaTime, Space.World);

            MoveRequestRpc(transform.position, transform.rotation);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void MoveRequestRpc(Vector3 pos, Quaternion rot)
    {
        this.transform.position = pos;
        this.transform.rotation = rot;
    }

    void HandleAnimations()
    {
        float speed = Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput);
        animator.SetFloat("Speed", speed);

        if (IsOwner)
        {
            moveSpeedSync.Value = speed;
        }
    }

    void LateUpdate()
    {
        if (!IsOwner)
        {
            animator.SetFloat("Speed", moveSpeedSync.Value);
        }
    }

    public void CoinCollected()
    {
        if (!isZombie) // Solo los humanos pueden recoger monedas
        {
            this.CoinsCollected++;
            //CoinsCollected.Value++;
            UpdateCoinUI();
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected}";
        }
    }

    private void OnNameChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        Debug.Log("Nombre previo:"+this.GetComponentInChildren<TextMeshPro>().text);
        this.GetComponentInChildren<TextMeshPro>().text = current.ToString();
        Debug.Log("Nombre después:" + this.GetComponentInChildren<TextMeshPro>().text);
    }
}

