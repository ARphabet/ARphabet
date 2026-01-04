using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // Photon ekledik
using TMPro;      // UI için
using System.Collections.Generic;

// MonoBehaviour yerine MonoBehaviourPunCallbacks yaptýk ki internetle konuþabilsin
public class MultiplayerAlphabetManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerAlphabetManager Instance;

    [Header("Veriler")]
    public List<AlphabetData> alphabetList; // Senin harf listen

    [Header("UI ve Ses")]
    public AudioSource audioSource;
    public Image ekrandakiResimKutusu;      // Ortada çýkan soru resmi
    public TextMeshProUGUI benimPuanText;   // Sol köþe puan
    public TextMeshProUGUI rakipPuanText;   // Sað köþe puan
    public GameObject uiPanel;              // (Eski kodundan kalan panel)

    // --- OYUN DEÐÝÞKENLERÝ ---
    private int[] oyunSirasi;
    private int suankiIndex = 0;
    private int benimToplamPuanim = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Odayý kuran kiþi oyunu baþlatýr
        if (PhotonNetwork.IsMasterClient)
        {
            KartlariKaristirVeBaslat();
        }
    }

    // --- 1. OYUN KURULUMU ---
    void KartlariKaristirVeBaslat()
    {
        int listeUzunlugu = alphabetList.Count;
        int[] sira = new int[listeUzunlugu];
        for (int i = 0; i < listeUzunlugu; i++) sira[i] = i;

        // Karýþtýr
        for (int i = 0; i < sira.Length; i++)
        {
            int rnd = Random.Range(0, sira.Length);
            int temp = sira[i];
            sira[i] = sira[rnd];
            sira[rnd] = temp;
        }

        // Herkese gönder
        photonView.RPC("OyunuBaslatRPC", RpcTarget.All, sira);
    }

    [PunRPC]
    public void OyunuBaslatRPC(int[] gelenSira)
    {
        oyunSirasi = gelenSira;
        suankiIndex = 0;
        benimToplamPuanim = 0;

        if (benimPuanText) benimPuanText.text = "Ben: 0";
        if (rakipPuanText) rakipPuanText.text = "Rakip: 0";

        Debug.Log("Oyun Baþladý!");
        KartiAc(oyunSirasi[0]);
    }

    // --- 2. KART AÇMA ---
    void KartiAc(int id)
    {
        // Listenden veriyi çek
        AlphabetData kartVerisi = alphabetList[id];

        // Resmi ekrana bas (Eðer UI Image atadýysan)
        if (ekrandakiResimKutusu != null)
        {
            ekrandakiResimKutusu.sprite = kartVerisi.cardImage; // Data içinde sprite yoksa hata verir, ekle!
            ekrandakiResimKutusu.gameObject.SetActive(true);
        }

        // VoiceManager'a "Hedef bu kelime" de
        if (MultiplayerVoiceManager.Instance != null)
        {
            MultiplayerVoiceManager.Instance.HedefKelimeyiGuncelle(kartVerisi.targetWord); // Data içinde string yoksa hata verir, ekle!
        }

        // Ýstersen harf sesini de çalabilirsin
        if (audioSource)
        {
            audioSource.clip = kartVerisi.letterSound;
            audioSource.Play();
        }
    }

    // --- 3. SES SONUCUNU ÝÞLEME ---
    // Bunu MultiplayerVoiceManager çaðýracak
    public void GeminiSonucunuIsle(int kazanilanPuan)
    {
        if (kazanilanPuan < 50) return; // Düþük puaný sayma

        benimToplamPuanim += kazanilanPuan;
        if (benimPuanText) benimPuanText.text = "Ben: " + benimToplamPuanim;

        // Rakibe hava at
        photonView.RPC("RakipSkorunuGuncelleRPC", RpcTarget.Others, benimToplamPuanim);

        // Sonraki
        SiradakiKartaGec();
    }

    void SiradakiKartaGec()
    {
        suankiIndex++;
        if (suankiIndex < oyunSirasi.Length)
        {
            KartiAc(oyunSirasi[suankiIndex]);
        }
        else
        {
            Debug.Log("OYUN BÝTTÝ");
        }
    }

    [PunRPC]
    public void RakipSkorunuGuncelleRPC(int rakibinYeniPuani)
    {
        if (rakipPuanText) rakipPuanText.text = "Rakip: " + rakibinYeniPuani;
    }
}