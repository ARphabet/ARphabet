using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class LobbyPlayer : NetworkBehaviour
{
    // Herkesin görebileceði "Hazýr mý?" deðiþkeni
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Sadece sunucu yazabilir
    );

    // Olay tetikleyici (UI'ý güncellemek için)
    public static event UnityAction OnPlayerChangedReadyState;

    public override void OnNetworkSpawn()
    {
        // Deðiþken deðiþtiðinde (biri hazýr olunca) haber ver
        IsReady.OnValueChanged += (oldValue, newValue) =>
        {
            OnPlayerChangedReadyState?.Invoke();
        };

        // Ýlk doðduðunda da listeyi güncelle
        OnPlayerChangedReadyState?.Invoke();
    }

    // Client butona basýnca bu ServerRPC'yi çaðýrýr
    [ServerRpc]
    public void ToggleReadyServerRpc()
    {
        IsReady.Value = !IsReady.Value; // Server deðeri deðiþtirir
    }
}