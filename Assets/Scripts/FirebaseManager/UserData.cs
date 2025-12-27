using System;

[Serializable]
public class UserData
{
    public string nickname;
    public int oynanilan_oyun;
    public int kazanilan_oyun;
    public int gosterilen_kart_sayisi;
    public int bilinen_kart_sayisi;
    public int maxSkor;
    public int level;
    public int mevcut_xp;

    // Firestore boþ constructor ister
    public UserData() { }

    public UserData(string nickname)
    {
        this.nickname = nickname;
        oynanilan_oyun = 0;
        kazanilan_oyun = 0;
        gosterilen_kart_sayisi = 0;
        bilinen_kart_sayisi = 0;
        maxSkor = 0;
        level = 1;
        mevcut_xp = 0;
    }
}
