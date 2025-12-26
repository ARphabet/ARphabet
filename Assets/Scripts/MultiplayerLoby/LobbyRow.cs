using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyRow : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public Button joinBtn;

    public void Setup(Lobby lobby)
    {
        roomNameText.text = lobby.Name; // Lobinin adýný ekrana yazar
        joinBtn.onClick.AddListener(() => {
            // Buraya týklanýnca MultiplayerManager'daki katýlma fonksiyonu çalýþacak
            Debug.Log(lobby.Name + " odasýna katýlýnýyor...");
        });
    }
}