using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyRoomItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    private Lobby lobby;

    public void Initialize(Lobby _lobby)
    {
        lobby = _lobby;
        roomNameText.text = _lobby.Name;
    }

    // BUTONA BASINCA BURASI ÇALIÞACAK
    public void JoinLobby()
    {
        // KONTROLÜ KALDIRIYORUZ!
        // Çünkü biz daha üye olmadýðýmýz için 'JoinCode' verisini þu an göremeyiz.
        // Odaya girme ve kodu okuma iþini MultiplayerManager halledecek.

        if (lobby != null)
        {
            Debug.Log("Butona basýldý, Manager'a istek gidiyor: " + lobby.Id);
            MultiplayerManager.Instance.JoinLobby(lobby.Id);
        }
    }
}