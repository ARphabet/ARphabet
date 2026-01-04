using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("--- UI Elemanlarý ---")]
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Text statusText;

    [Header("--- Oda Listesi Ayarlarý ---")]
    public Transform contentObject; // Inspector'da buranýn dolu olduðundan emin ol
    public GameObject roomItemPrefab; // Inspector'da buranýn dolu olduðundan emin ol
    public GameObject lobbyPanel;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();

        if (statusText != null) statusText.text = "Sunucuya baðlanýlýyor...";
        if (createButton != null) createButton.interactable = false;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Server'a gelindi, Lobiye giriliyor...");
        PhotonNetwork.JoinLobby();
        if (statusText != null) statusText.text = "Lobiye giriliyor...";
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Lobiye Girildi.");
        if (statusText != null) statusText.text = "Lobiye Hoþgeldin!";
        if (createButton != null) createButton.interactable = true;
    }

    public void CreateRoom()
    {
        string odaAdi = roomNameInput.text;

        if (string.IsNullOrEmpty(odaAdi))
        {
            odaAdi = "Oda " + UnityEngine.Random.Range(1000, 9999);
        }

        RoomOptions options = new RoomOptions { MaxPlayers = 2, IsOpen = true, IsVisible = true };
        PhotonNetwork.CreateRoom(odaAdi, options);

        if (statusText != null) statusText.text = "Oda kuruluyor: " + odaAdi;
        Debug.Log(odaAdi + " kuruluyor...");
    }

    public override void OnJoinedRoom()
    {
        if (statusText != null) statusText.text = "Odaya Girildi: " + PhotonNetwork.CurrentRoom.Name;
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    // --- DÜZENLENEN KISIM (HATA BURADAYDI) ---
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // 1. KONTROL: Liste içeriði veya parent obje boþ mu?
        if (contentObject == null)
        {
            Debug.LogError("HATA: Inspector'da 'Content Object' kýsmýný boþ býrakmýþsýn!");
            return;
        }

        // Önce eski listeyi temizle
        foreach (Transform child in contentObject)
        {
            Destroy(child.gameObject);
        }

        // 2. KONTROL: Prefab atanmýþ mý?
        if (roomItemPrefab == null)
        {
            Debug.LogError("HATA: Inspector'da 'Room Item Prefab' kýsmýný boþ býrakmýþsýn!");
            return;
        }

        foreach (RoomInfo room in roomList)
        {
            // Kapalý, görünmez veya silinmiþ odalarý atla
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible) continue;

            // Prefab'ý oluþtur
            GameObject newRow = Instantiate(roomItemPrefab, contentObject);

            // 3. KONTROL: Prefab üzerinde 'RoomItem' scripti var mý?
            RoomItem itemScript = newRow.GetComponent<RoomItem>();

            if (itemScript != null)
            {
                itemScript.SetRoomName(room.Name);
            }
            else
            {
                // Eðer buraya düþerse; Unity'de Prefab'ý aç, "Add Component" diyip RoomItem scriptini ekle.
                Debug.LogError("HATA: Oluþturulan Prefab üzerinde 'RoomItem' scripti bulunamadý! Lütfen prefab'ý kontrol et.");
            }
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (statusText != null) statusText.text = "Hata: " + message;
        Debug.LogError("Oda kurulamadý: " + message);
    }
}