using UnityEngine;
using UnityEngine.UI;
using TMPro; // InputField için gerekli

public class TestUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private LobbyListUI lobbyListUI;

    // YENÝ EKLENEN: Input alanýný buraya sürükleyeceksin
    [SerializeField] private TMP_InputField lobbyNameInput;

    private void Awake()
    {
        hostBtn.onClick.AddListener(async () => {

            // Eðer input boþsa varsayýlan bir isim ata, deðilse girilen ismi kullan
            string odaIsmi = string.IsNullOrEmpty(lobbyNameInput.text) ? "Oda " + Random.Range(10, 99) : lobbyNameInput.text;

            await MultiplayerManager.Instance.CreateLobby(odaIsmi, false);
        });

        refreshBtn.onClick.AddListener(() => {
            lobbyListUI.RefreshList();
        });
    }
}