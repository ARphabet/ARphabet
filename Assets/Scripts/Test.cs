using UnityEngine;
using Vuforia;

public class Test : MonoBehaviour
{
    void Start()
    {
        // Vuforia'nın gözlemcisine (Observer) abone oluyoruz
        var observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += DurumDegisti;
        }
    }

    // Hedefin durumu değişince (Görünce veya Kaybedince) burası çalışır
    private void DurumDegisti(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (targetStatus.Status == Status.TRACKED ||
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            // Kamera hedefi gördü!
            Debug.Log("🔥🔥🔥 GORDUM KRAL! -> " + behaviour.TargetName);
        }
        else
        {
            // Kamera hedefi kaybetti
            Debug.Log("❌ KAYBOLDU...");
        }
    }
}