using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

public class LobbyListUI : MonoBehaviour
{
    [SerializeField] private Transform content; // Scroll View -> Viewport -> Content
    [SerializeField] private GameObject lobbyRowPrefab; // Hazýrladýðýn Lobi Satýrý Prefabý

    public async void RefreshList()
    {
        // 1. Önce listedeki eski satýrlarý temizle
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // 2. Güncel lobileri çek
        List<Lobby> lobbies = await MultiplayerManager.Instance.GetAllLobbies();

        if (lobbies == null) return;

        // 3. Her lobi için bir prefab oluþtur
        foreach (Lobby lobby in lobbies)
        {
            GameObject row = Instantiate(lobbyRowPrefab, content);

            // Ölçeði düzelt (Bazen UI bozulabiliyor)
            row.transform.localScale = Vector3.one;

            // --- DÜZELTÝLEN KISIM BURASI ---
            // Senin scriptinin adý "LobbyRow" deðil "LobbyRoomItem"
            LobbyRoomItem itemScript = row.GetComponent<LobbyRoomItem>();

            if (itemScript != null)
            {
                // Fonksiyonun adý "Setup" deðil "Initialize"
                itemScript.Initialize(lobby);
            }
            else
            {
                Debug.LogError("HATA: Prefabýn üzerine 'LobbyRoomItem' scriptini eklememiþsin!");
            }
        }

        Debug.Log("Lobi listesi yenilendi.");
    }
}