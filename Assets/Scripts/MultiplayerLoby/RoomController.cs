using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class RoomController : NetworkBehaviour
{
    [Header("Paneller")]
    [SerializeField] private GameObject lobbyPanel; // Menüdeki her þey (Scroll, Input vs.)
    [SerializeField] private GameObject roomPanel;  // Odadaki her þey (Butonlar vs.)

    [Header("Oda Ýçi UI")]
    [SerializeField] private Button mainButton;     // Baþlat / Hazýr Ol
    [SerializeField] private TextMeshProUGUI mainButtonText;
    [SerializeField] private Button leaveButton;    // Ayrýl

    private void Awake()
    {
        // OYUN AÇILDIÐINDA:
        // Lobi paneli açýk olsun, Oda paneli gizli olsun.
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
    }

    private void Start()
    {
        // Buton Dinleyicileri
        mainButton.onClick.AddListener(OnMainButtonClick);

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(() => {
                MultiplayerManager.Instance.LeaveLobby();
            });
        }

        // Event Abonelikleri
        LobbyPlayer.OnPlayerChangedReadyState += UpdateButtonUI;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => UpdateButtonUI();
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
            {
                if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    MultiplayerManager.Instance.LeaveLobby();
                }
                UpdateButtonUI();
            };
        }
    }

    // --- KRÝTÝK KISIM BURASI ---
    public override void OnNetworkSpawn()
    {
        // Aða baðlandýk (Odaya girdik)!
        // Artýk Lobi menüsüne (Liste, Input vs.) ihtiyacýmýz yok.
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        // Oda butonlarýný göster
        if (roomPanel != null) roomPanel.SetActive(true);

        UpdateButtonUI();
    }



    public void OnMainButtonClick()
    {
        if (IsServer) 
        {
            
            if (CanStartGame())
            {
                Debug.Log("Oyun Baþlatýlýyor! Sahne: MultiplayerGame");

               
                NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGame", LoadSceneMode.Single);
            }
        }
        else
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LobbyPlayer>();
            if (localPlayer != null)
            {
                localPlayer.ToggleReadyServerRpc();
            }
        }
    }

    private void UpdateButtonUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.LocalClient?.PlayerObject == null)
        {
            return;
        }

        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                mainButtonText.text = "Oyuncu Bekleniyor";
                mainButton.interactable = false;
            }
            else if (!CheckIfAllReady())
            {
                mainButtonText.text = "Oyunu Baþlat";
                mainButton.interactable = false;
            }
            else
            {
                mainButtonText.text = "Oyunu Baþlat";
                mainButton.interactable = true;
            }
        }
        else
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LobbyPlayer>();
            bool amIReady = localPlayer.IsReady.Value;

            mainButtonText.text = amIReady ? "ÝPTAL" : "HAZIR OL";
            mainButton.interactable = true;
        }
    }

    private bool CanStartGame()
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1) return false;
        return CheckIfAllReady();
    }

    private bool CheckIfAllReady()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;
            if (client.PlayerObject != null)
            {
                var playerScript = client.PlayerObject.GetComponent<LobbyPlayer>();
                if (playerScript == null || !playerScript.IsReady.Value) return false;
            }
        }
        return true;
    }

    private void OnDestroy()
    {
        LobbyPlayer.OnPlayerChangedReadyState -= UpdateButtonUI;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= (id) => UpdateButtonUI();
            NetworkManager.Singleton.OnClientDisconnectCallback -= (id) => UpdateButtonUI();
        }
    }
}