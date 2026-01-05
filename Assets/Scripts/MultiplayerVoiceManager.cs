using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using System.IO;
using TMPro;

public class MultiplayerVoiceManager : MonoBehaviour
{
    // --- DÜZELTİLEN KISIM ---
    // Artık kendi ismini taşıyor, hata vermeyecek.
    public static MultiplayerVoiceManager Instance;

    [Header("Gemini Ayarları")]
    public string geminiApiKey = "AIzaSyAj77w2jM3QqPaSrwObYiViwCoEfYhoioQ";
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("UI Bağlantıları")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI buttonLabel;

    // --- DEĞİŞKENLER ---
    public string aktifHedefKelime = ""; // GameManager burayı güncelleyecek
    private AudioClip recordingClip;
    private string deviceName;
    private bool isRecording = false;

    void Awake()
    {
        // Singleton Deseni (Artık türler uyuşuyor)
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            if (scoreText) scoreText.text = "Hazır";
        }
        else
        {
            if (scoreText) scoreText.text = "Mikrofon Yok!";
        }
    }

    // --- GAME MANAGER BURAYI ÇAĞIRACAK ---
    public void HedefKelimeyiGuncelle(string yeniKelime)
    {
        aktifHedefKelime = yeniKelime;
        Debug.Log("Yeni Hedef Kelime: " + aktifHedefKelime);
        // İstersen UI'da ipucu gösterebilirsin
        // if(scoreText) scoreText.text = "Sıradaki: " + yeniKelime;
    }

    // --- BUTON İŞLEMLERİ ---
    public void ButonMikrofonBas()
    {
        if (!isRecording)
            BaslatKayit();
        else
            BitirVeGonder();
    }

    void BaslatKayit()
    {
        if (string.IsNullOrEmpty(deviceName)) return;

        isRecording = true;
        recordingClip = Microphone.Start(deviceName, false, 3, 44100); // 3 Saniye

        if (scoreText) scoreText.text = "Dinliyorum...";
        if (scoreText) scoreText.color = Color.yellow;
        if (buttonLabel) buttonLabel.text = "BİTİR";
    }

    void BitirVeGonder()
    {
        if (!isRecording) return;

        int position = Microphone.GetPosition(deviceName);
        if (position <= 0)
        {
            Microphone.End(deviceName);
            isRecording = false;
            if (buttonLabel) buttonLabel.text = "KONUŞ";
            return;
        }

        Microphone.End(deviceName);
        isRecording = false;

        if (scoreText) scoreText.text = "Analiz ediliyor...";
        if (buttonLabel) buttonLabel.text = "KONUŞ";

        byte[] wavData = ConvertToWav(recordingClip, position);
        StartCoroutine(SendToGemini(wavData));
    }

    IEnumerator SendToGemini(byte[] audioData)
    {
        string url = $"{API_URL}?key={geminiApiKey.Trim()}";
        string base64Audio = Convert.ToBase64String(audioData);
        string promptText = "Bu ses dosyasını dinle. Söylenen TÜRKÇE kelimeyi tam olarak yazıya dök. Sadece kelimeyi yaz.";

        string jsonBody = $@"{{""contents"":[{{""parts"":[{{""text"":""{promptText}""}},{{""inline_data"":{{""mime_type"":""audio/wav"",""data"":""{base64Audio}""}}}}]}}]}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (scoreText) scoreText.text = "Hata!";
                Debug.LogError(request.error);
            }
            else
            {
                // 1. Cevabı İşle
                string jsonResponse = request.downloadHandler.text;
                string spokenText = ExtractTextFromJson(jsonResponse);

                // 2. Puanı Hesapla
                int score = CalculateScore(aktifHedefKelime, spokenText);

                // 3. UI Güncelle (Oyuncu ne dediğini görsün)
                UpdateUI(spokenText, score);

                // 4. KRİTİK NOKTA: Puanı GameManager'a gönder!
                if (MultiplayerAlphabetManager.Instance != null)
                {
                    MultiplayerAlphabetManager.Instance.GeminiSonucunuIsle(score);
                }
                else
                {
                    Debug.LogWarning("GameManager bulunamadı! Puan gönderilemedi.");
                }
            }
        }
    }

    // --- YARDIMCI METODLAR ---

    void UpdateUI(string spoken, int score)
    {
        if (scoreText)
        {
            string mesaj = $"Hedef: {aktifHedefKelime}\nAlgılanan: {spoken}\nPUAN: {score}";
            scoreText.text = mesaj;
            scoreText.color = score >= 50 ? Color.green : Color.red;
        }
    }

    int CalculateScore(string target, string received)
    {
        string s = target.ToLower().Trim();
        string t = received.ToLower().Trim();

        if (s == t) return 100;
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 0;

        // Levenshtein Mesafesi
        int n = s.Length; int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        float maxLen = Mathf.Max(n, m);
        float similarity = 1.0f - ((float)d[n, m] / maxLen);
        return Mathf.Clamp((int)(similarity * 100), 0, 100);
    }

    string ExtractTextFromJson(string json)
    {
        try
        {
            string marker = "\"text\": \""; int start = json.IndexOf(marker);
            if (start == -1) return "???";
            start += marker.Length; int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start).Replace("\\n", "").Trim();
        }
        catch { return "Hata"; }
    }
    public void ZorlaDurdur()
    {
        isRecording = false;
        Microphone.End(deviceName);
        if (scoreText) scoreText.text = "Süre Doldu!";
        if (buttonLabel) buttonLabel.text = "-";
    }

    public class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) { return true; } }

    byte[] ConvertToWav(AudioClip clip, int position)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            stream.Write(Encoding.UTF8.GetBytes("RIFF"), 0, 4); stream.Write(BitConverter.GetBytes(36 + position * 2), 0, 4); stream.Write(Encoding.UTF8.GetBytes("WAVE"), 0, 4); stream.Write(Encoding.UTF8.GetBytes("fmt "), 0, 4); stream.Write(BitConverter.GetBytes(16), 0, 4); stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); stream.Write(BitConverter.GetBytes(44100), 0, 4); stream.Write(BitConverter.GetBytes(44100 * 2), 0, 4); stream.Write(BitConverter.GetBytes((ushort)2), 0, 2); stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); stream.Write(Encoding.UTF8.GetBytes("data"), 0, 4); stream.Write(BitConverter.GetBytes(position * 2), 0, 4);
            float[] data = new float[position]; clip.GetData(data, 0);
            foreach (var sample in data) { short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f); stream.Write(BitConverter.GetBytes(intSample), 0, 2); }
            return stream.ToArray();
        }
    }
}