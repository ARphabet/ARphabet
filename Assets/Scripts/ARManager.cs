using UnityEngine;

public class ARAlphabetController : MonoBehaviour
{
    public GameObject uiPanel; // Ekranda çýkacak buton paneli
    public AudioSource audioSource;

    public AudioClip letterSound; // Harf sesi (m4a)
    public AudioClip wordSound;   // Kelime sesi (m4a)

    // Image Target bulunduðunda çalýþýr
    public void OnTargetFound()
    {
        uiPanel.SetActive(true);
    }

    // Image Target kaybolduðunda çalýþýr
    public void OnTargetLost()
    {
        uiPanel.SetActive(false);
        audioSource.Stop();
    }

    // 1. Butona baðlanacak fonksiyon
    public void PlayLetter()
    {
        audioSource.clip = letterSound;
        audioSource.Play();
    }

    // 2. Butona baðlanacak fonksiyon
    public void PlayWord()
    {
        audioSource.clip = wordSound;
        audioSource.Play();
    }
}