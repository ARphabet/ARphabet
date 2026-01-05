using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;

public class MultiplayerAlphabetManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerAlphabetManager Instance;

    [Header("Veriler")]
    public List<AlphabetData> alphabetList;

    [Header("UI ve Ses")]
    public AudioSource audioSource;
    public Image ekrandakiResimKutusu;
    public TextMeshProUGUI benimPuanText;
    public TextMeshProUGUI rakipPuanText;

    [Header("Zaman Ayarları")]
    public TextMeshProUGUI zamanText;
    public GameObject oyunSonuPaneli;
    public TextMeshProUGUI oyunSonuMesajText;

    private const float OYUN_SURESI = 30f;
    private float kalanSure = 0;
    private bool oyunDevamEdiyor = false;

    // --- OYUN DEĞİŞKENLERİ ---
    private int[] oyunSirasi;
    private int suankiIndex = 0;

    public int benimToplamPuanim = 0;
    public int rakipToplamPuanim = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (oyunSonuPaneli) oyunSonuPaneli.SetActive(false);

        // SENARYO 1: İnternet Yok veya Lobi Kullanmadın (Simülatör Testi)
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("⚠️ TEST MODU: Offline Karıştırma Yapılıyor...");
            // Rastgele bir liste oluştur ve oyunu başlat
            int[] testSirasi = ListeyiRastgeleOlustur(alphabetList.Count);
            OyunuBaslatRPC(testSirasi);
        }
        // SENARYO 2: Gerçek Oyun (Lobi üzerinden geldin)
        else if (PhotonNetwork.IsMasterClient)
        {
            Invoke("OnlineKaristirVeBaslat", 2f);
        }
    }

    void Update()
    {
        if (oyunDevamEdiyor)
        {
            kalanSure -= Time.deltaTime;
            if (zamanText) zamanText.text = Mathf.CeilToInt(kalanSure).ToString();

            if (kalanSure <= 0)
            {
                oyunDevamEdiyor = false;
                kalanSure = 0;
                OyunBitti();
            }
        }
    }

    // --- YENİ KARIŞTIRMA FONKSİYONU ---
    // Bu fonksiyon 0'dan 29'a kadar sayıları alır ve çorba yapıp geri verir
    int[] ListeyiRastgeleOlustur(int uzunluk)
    {
        int[] sira = new int[uzunluk];
        // Önce sırayla doldur: 0, 1, 2, 3...
        for (int i = 0; i < uzunluk; i++) sira[i] = i;

        // Sonra Fisher-Yates algoritması ile karıştır
        for (int i = 0; i < sira.Length; i++)
        {
            int temp = sira[i];
            int randomIndex = Random.Range(i, sira.Length); // Rastgele bir yer seç
            sira[i] = sira[randomIndex];
            sira[randomIndex] = temp;
        }
        return sira;
    }

    // --- GERÇEK OYUN BAŞLATICISI ---
    void OnlineKaristirVeBaslat()
    {
        // Listeyi karıştır
        int[] sira = ListeyiRastgeleOlustur(alphabetList.Count);

        // Herkese gönder
        photonView.RPC("OyunuBaslatRPC", RpcTarget.All, sira);
    }

    [PunRPC]
    public void OyunuBaslatRPC(int[] gelenSira)
    {
        oyunSirasi = gelenSira;
        suankiIndex = 0;
        benimToplamPuanim = 0;
        rakipToplamPuanim = 0;

        kalanSure = OYUN_SURESI;
        oyunDevamEdiyor = true;

        if (benimPuanText) benimPuanText.text = "Ben: 0";
        if (rakipPuanText) rakipPuanText.text = "Rakip: 0";

        Debug.Log("Oyun Başladı! İlk Kart ID: " + oyunSirasi[0]);
        KartiAc(oyunSirasi[0]);
    }

    void KartiAc(int id)
    {
        if (!oyunDevamEdiyor) return;

        AlphabetData kartVerisi = alphabetList[id];

        if (ekrandakiResimKutusu != null)
        {
            ekrandakiResimKutusu.sprite = kartVerisi.cardImage;
            ekrandakiResimKutusu.gameObject.SetActive(true);
        }

        if (MultiplayerVoiceManager.Instance != null)
        {
            MultiplayerVoiceManager.Instance.HedefKelimeyiGuncelle(kartVerisi.targetWord);
        }

        if (audioSource)
        {
            audioSource.clip = kartVerisi.letterSound;
            audioSource.Play();
        }
    }

    public void GeminiSonucunuIsle(int kazanilanPuan)
    {
        if (!oyunDevamEdiyor) return;
        if (kazanilanPuan < 50) return;

        benimToplamPuanim += kazanilanPuan;
        if (benimPuanText) benimPuanText.text = "Ben: " + benimToplamPuanim;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RakipSkorunuGuncelleRPC", RpcTarget.Others, benimToplamPuanim);
        }

        SiradakiKartaGec();
    }

    void SiradakiKartaGec()
    {
        suankiIndex++;
        if (suankiIndex >= oyunSirasi.Length)
        {
            suankiIndex = 0; // Liste bitince başa sar
        }
        KartiAc(oyunSirasi[suankiIndex]);
    }

    [PunRPC]
    public void RakipSkorunuGuncelleRPC(int rakibinYeniPuani)
    {
        rakipToplamPuanim = rakibinYeniPuani;
        if (rakipPuanText) rakipPuanText.text = "Rakip: " + rakipToplamPuanim;
    }

    void OyunBitti()
    {
        if (MultiplayerVoiceManager.Instance != null)
            MultiplayerVoiceManager.Instance.ZorlaDurdur();

        if (oyunSonuPaneli) oyunSonuPaneli.SetActive(true);

        string sonucMesaji = "";
        if (benimToplamPuanim > rakipToplamPuanim) sonucMesaji = "KAZANDIN!\nSkor: " + benimToplamPuanim;
        else if (benimToplamPuanim < rakipToplamPuanim) sonucMesaji = "KAYBETTİN...\nRakip: " + rakipToplamPuanim;
        else sonucMesaji = "BERABERE!";

        if (oyunSonuMesajText) oyunSonuMesajText.text = sonucMesaji;
    }
}