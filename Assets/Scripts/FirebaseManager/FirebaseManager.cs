using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    private FirebaseFirestore db;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("🔥 Firestore hazır");
    }

    // 🔹 KULLANICI OLUŞTUR
    public void CreateUser(string kullaniciId, string nickname)
    {
        UserData user = new UserData(nickname);

        db.Collection("users")
          .Document(kullaniciId)
          .SetAsync(user)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
                  Debug.Log("✅ Kullanıcı Firestore'a kaydedildi");
              else
                  Debug.LogError("❌ Kullanıcı kaydedilemedi");
          });
    }

    // 🔹 KULLANICI VERİSİNİ GÜNCELLE
    public void UpdateUserStats(string kullaniciId, Dictionary<string, object> updates)
    {
        db.Collection("users")
          .Document(kullaniciId)
          .UpdateAsync(updates)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
                  Debug.Log("📊 Kullanıcı verisi güncellendi");
              else
                  Debug.LogError("❌ Güncelleme başarısız");
          });
    }
}
