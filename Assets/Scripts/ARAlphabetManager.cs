using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AlphabetData
{
    public string letterName; // Hatýrlatýcý olmasý için (A, B, C...)
    public AudioClip letterSound;
    public AudioClip wordSound;
}

public class ARAlphabetManager : MonoBehaviour
{
    public AudioSource audioSource;
    public GameObject uiPanel;
    public List<AlphabetData> alphabetList; // Tüm harfler burada toplanacak

    private int currentLetterIndex = -1;
    private bool isTargetLocked = false;

    public void OnTargetFound(int index)
    {
        if (isTargetLocked) return;

        currentLetterIndex = index;
        uiPanel.SetActive(true);
        Debug.Log("Harf kilitlendi: " + alphabetList[index].letterName);
    }

    public void OnTargetLost()
    {
        if (currentLetterIndex < 0 || currentLetterIndex >= alphabetList.Count)
        {
            uiPanel.SetActive(false);
            audioSource.Stop();
            currentLetterIndex = -1;
            return;
        }

        Debug.Log(alphabetList[currentLetterIndex].letterName + " harfi kayboldu.");

        uiPanel.SetActive(false);
        audioSource.Stop();
        currentLetterIndex = -1;
    }

    public void PlayLetter()
    {
        if (currentLetterIndex != -1)
        {
            Debug.Log("Harf sesi çalýnýyor: " + alphabetList[currentLetterIndex].letterName);
            audioSource.clip = alphabetList[currentLetterIndex].letterSound;
            audioSource.Play();
        }
    }

    public void PlayWord()
    {
        if (currentLetterIndex != -1)
        {
            Debug.Log("Kelime sesi çalýnýyor: " + alphabetList[currentLetterIndex].letterName);
            audioSource.clip = alphabetList[currentLetterIndex].wordSound;
            audioSource.Play();
        }
    }

    public void BtnBasildi()
    {
        Debug.Log("Butona basýldý");
    }
}