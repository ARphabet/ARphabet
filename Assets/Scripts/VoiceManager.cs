using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using System.IO;
using TMPro;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;

    [Header("Gemini Ayarları")]
    public string geminiApiKey = "AIzaSyB9kX2AvhuHdap23qIBHBHK9qaNoF432Tc"; // Yeni aldığın Key'i buraya yaz

    // SENİN İSTEDİĞİN ÖZEL URL:
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent"; 
    
    [Header("UI Bağlantıları")]
    public TextMeshProUGUI scoreText;      // Sonucu yazacağımız yer
    public TextMeshProUGUI buttonLabel;    // Buton üzerindeki yazı

    // --- HAFIZA ---
    private string aktifHedefKelime = "Havuç"; // Test için varsayılan kelime
    private AudioClip recordingClip;
    private string deviceName;
    private bool isRecording = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            Debug.Log("🎤 Seçilen Mikrofon: " + deviceName);

            if (scoreText) scoreText.text = "Hazır: " + deviceName;
        }
        else
        {
            if (scoreText) scoreText.text = "Mikrofon Yok!";
        }
    }

    // --- BUTON FONKSİYONU ---
    // Bu fonksiyonu Unity'de Butona bağlayacaksın
    public void ButonMikrofonBas()
    {
        if (!isRecording)
        {
            BaslatKayit();
        }
        else
        {
            BitirVeGonder();
        }
    }

    void BaslatKayit()
    {
        if (string.IsNullOrEmpty(deviceName)) return;

        isRecording = true;
        // 3 Saniye kayıt yeterli (Daha hızlı sonuç için 5'ten 3'e düşürdüm)
        recordingClip = Microphone.Start(deviceName, false, 3, 44100);

        if (scoreText) scoreText.text = "Dinliyorum...";
        if (scoreText) scoreText.color = Color.yellow;
        if (buttonLabel) buttonLabel.text = "BİTİR";
    }

    void BitirVeGonder()
    {
        if (!isRecording) return;

        // Mikrofonun nerede olduğunu al
        int position = Microphone.GetPosition(deviceName);

        if (position <= 0)
        {
            Debug.LogWarning("Kayıt çok kısa, işlem iptal edildi.");
            Microphone.End(deviceName); // Mikrofonu yine de kapat
            isRecording = false;

            if (buttonLabel) buttonLabel.text = "KONUŞ";
            if (scoreText) scoreText.text = "Ses Yok!";
            return; // Fonksiyondan çık, aşağıya inme!
        }
        // ---------------------------

        Microphone.End(deviceName);
        isRecording = false;

        if (scoreText) scoreText.text = "Analiz ediliyor...";
        if (buttonLabel) buttonLabel.text = "KONUŞ";

        // Artık position güvenli, wav dönüşümü yapabiliriz
        byte[] wavData = ConvertToWav(recordingClip, position);
        StartCoroutine(SendToGemini(wavData));
    }

    IEnumerator SendToGemini(byte[] audioData)
    {
        // URL + API Key birleşimi (Trim ile boşlukları temizledik)
        string url = $"{API_URL}?key={geminiApiKey.Trim()}";
        string base64Audio = Convert.ToBase64String(audioData);

        // Prompt: Sadece duyduğunu yazmasını istiyoruz.
        string promptText = "Bu ses dosyasını dinle. Söylenen TÜRKÇE kelimeyi tam olarak yazıya dök. Çeviri yapma, sadece duyduğun kelimeyi yaz.";

        // JSON Paketi
        string jsonBody = $@"{{""contents"":[{{""parts"":[{{""text"":""{promptText}""}},{{""inline_data"":{{""mime_type"":""audio/wav"",""data"":""{base64Audio}""}}}}]}}]}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // SSL Sertifika hatalarını atlamak için (Güvenlik duvarı olan okullarda vs işe yarar)
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string hata = $"HATA: {request.responseCode}\n{request.error}";
                if (scoreText) scoreText.text = "Bağlantı Hatası!";
                Debug.LogError(hata + "\n" + request.downloadHandler.text);
            }
            else
            {
                // 1. Cevabı al
                string responseText = request.downloadHandler.text;
                // 2. JSON içinden metni ayıkla
                string spokenText = ExtractTextFromJson(responseText);

                // 3. Puanı Hesapla (Matematiksel Karşılaştırma)
                int score = CalculateScore(aktifHedefKelime, spokenText);

                // 4. Ekrana Yaz
                UpdateUI(spokenText, score);

                // --- DATABASE & ANALYTICS KISMI (Scriptlerin varsa açabilirsin) ---
                /*
                if (DatabaseManager.Instance != null)
                {
                    DatabaseManager.Instance.SkoruKaydet(aktifHedefKelime, score, "Kelime");
                }
                */
            }
        }
    }

    void UpdateUI(string spoken, int score)
    {
        if (scoreText)
        {
            string mesaj = $"Hedef: {aktifHedefKelime}\nAlgılanan: {spoken}\nPUAN: {score}";

            if (score >= 80)
            {
                scoreText.text = "MÜKEMMEL! \n" + mesaj;
                scoreText.color = Color.green;
            }
            else if (score >= 50)
            {
                scoreText.text = "İYİ \n" + mesaj;
                scoreText.color = new Color(1f, 0.64f, 0f); // Turuncu
            }
            else
            {
                scoreText.text = "TEKRAR DENE \n" + mesaj;
                scoreText.color = Color.red;
            }
        }
    }

    // --- YARDIMCI SINIFLAR ---

    // SSL Hatası Engelleyici
    public class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) { return true; } }

    // WAV Dönüştürücü
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

    // Basit JSON okuyucu
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

    // Levenshtein Mesafesi (Kelime Benzerlik Algoritması)
    int CalculateScore(string target, string received)
    {
        string s = target.ToLower().Trim();
        string t = received.ToLower().Trim();

        // Gereksiz noktalama işaretlerini temizle
        char[] charsToTrim = { '*', '.', ',', '!', '?' };
        s = s.Trim(charsToTrim);
        t = t.Trim(charsToTrim);

        if (s == t) return 100;
        if (string.IsNullOrEmpty(s)) return 0;
        if (string.IsNullOrEmpty(t)) return 0;

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
}