using System;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static event Action<PlayerData> OnPlayerSpawned;

    [SerializeField] private string playerName;

    private void Start()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    public string GetName() => playerName;
}
