using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }
    
    private Lobby hostLobby; // Kurduðumuz lobi (Sadece Host için)
    private float heartbeatTimer; // Kalp atýþý sayacý

    private void Awake() 
    { 
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this; 
            DontDestroyOnLoad(this.gameObject); // Sahne deðiþince yok olmasýn
        }
    }

    async void Start()
    {
        // Unity Servislerini baþlat
        await UnityServices.InitializeAsync();

        // Giriþ yapmamýþsa anonim giriþ yaptýr
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sisteme giriþ yapýldý. ID: " + AuthenticationService.Instance.PlayerId);
        }
    }

    private void Update()
    {
        // LOBÝ KALP ATIÞI (HEARTBEAT)
        // Eðer bir lobi kurduysak, her 15 saniyede bir "Buradayým" demeliyiz.
        // Yoksa Unity 30 saniye sonra lobiyi "aktif deðil" sanýp siler.
        HandleLobbyHeartbeat();
    }

    // --- 1. LOBÝ OLUÞTURMA (HOST) ---
    // LOBÝ OLUÞTURMA (HOST)
    public async Task CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            // 1. Relay Kodunu Al
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[HOST] Relay Kodu Üretildi: {relayJoinCode}");

            // 2. Seçenekleri Hazýrla (DATA BURADA!)
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject> {
                { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            }
            };

            // 3. Odayý Kur (options parametresine DÝKKAT!)
            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, options);

            // --- ÝÞTE AJAN BURASI (Bunu eklemezsen kör kalýrýz) ---
            Debug.Log("---------------- HOST KONTROLÜ ----------------");
            if (hostLobby.Data != null && hostLobby.Data.ContainsKey("JoinCode"))
            {
                Debug.Log($"[HOST] Odayý kurdum ve þifreyi içine gömdüm! Kod: {hostLobby.Data["JoinCode"].Value}");
            }
            else
            {
                Debug.LogError(" [HOST] HASSÝKTÝR! Odayý kurdum ama Data BOÞ geldi! Options iþe yaramadý.");
            }
            Debug.Log("-----------------------------------------------");

            // 4. Host Baðlantýsýný Baþlat
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HOST] HATA: {e.Message}");
        }
    }
    // --- 2. LOBÝLERÝ LÝSTELEME ---
    public async Task<List<Lobby>> GetAllLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25; // En fazla 25 lobi getir

            // Sadece boþ yeri olan odalarý getir (Ýsteðe baðlý)
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results;
        }
        catch (LobbyServiceException e) 
        { 
            Debug.LogError("Lobi listeleme hatasý: " + e.Message); 
            return new List<Lobby>(); 
        }
    }

    // --- 3. LOBÝYE KATILMA (CLIENT) ---
    // Not: Burayý 'Lobby' objesi yerine 'lobbyId' alacak þekilde güncelledim.
    // Çünkü UI scriptinden ID göndermek daha garantidir.
    // LOBÝYE KATILMA (CLIENT)
    public async void JoinLobby(string lobbyId)
    {
        Debug.Log("ADIM 1: JoinLobby fonksiyonu çalýþtý! ID: " + lobbyId);

        // KORUMA: Zaten baðlýysak tekrar deneme
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Zaten bir aða baðlýyýz, iþlem iptal.");
            return;
        }

        try
        {
            Debug.Log("ADIM 2: Lobi servisine istek atýlýyor...");

            // A. LOBÝYE GÝR
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("ADIM 3: Lobiye giriþ baþarýlý! Lobi Adý: " + joinedLobby.Name);

            // B. RELAY KODUNU ÇEK (En kritik yer burasý!)
            string relayJoinCode = null;

            if (joinedLobby.Data != null && joinedLobby.Data.ContainsKey("JoinCode"))
            {
                relayJoinCode = joinedLobby.Data["JoinCode"].Value;
                Debug.Log("ADIM 4: Relay Kodu Bulundu! Kod: " + relayJoinCode);
            }
            else
            {
                Debug.LogError("HATA: Bu lobide 'JoinCode' verisi YOK! Host kodu kaydetmemiþ.");
                return; // Kod yoksa devam etme
            }

            // C. RELAY BAÐLANTISI
            Debug.Log("ADIM 5: Relay servisine baðlanýlýyor...");
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            Debug.Log("ADIM 6: Network verileri ayarlanýyor...");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            Debug.Log("ADIM 7: Client Baþlatýlýyor!");
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError("BÜYÜK HATA: Lobiye katýlýrken bir þeyler patladý: " + e.Message);
        }
    }

    // --- YARDIMCI FONKSÝYONLAR ---

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                // Unity'ye "Lobi hala aktif, silme" diyoruz
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    // MultiplayerManager.cs dosyanýn en altýna git ve eski LeaveLobby yerine bunu yapýþtýr
    public async void LeaveLobby()
    {
        try
        {
            // 1. Eðer biz HOST isek, odayý tamamen silmemiz lazým
            if (hostLobby != null)
            {
                string lobbyId = hostLobby.Id;
                hostLobby = null; // Önce yerel deðiþkeni boþalt

                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log("[HOST] Lobi baþarýyla silindi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lobi silinirken hata oldu: " + e.Message);
        }
        finally
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }
}