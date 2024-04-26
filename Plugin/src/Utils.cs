using GiantSpecimens.Scrap;
using MoreShipUpgrades.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantSpecimens.src;
internal class Utils : NetworkBehaviour
{
    static int seed = StartOfRound.Instance.randomMapSeed;
    static System.Random random = new System.Random(seed + 85);
    internal static Utils Instance { get; set; }

    void Awake()
    {
        Instance = this;
    }

    public void SpawnScrap(Item item, Vector3 position) {        
        GameObject go = GameObject.Instantiate(item.spawnPrefab, position + Vector3.up, Quaternion.identity);
        int value = random.Next(minValue: item.minValue, maxValue: item.maxValue);
        var scanNode = go.gameObject.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        UpdateScanNodeClientRpc(go, value);
        SpawnNetworkObjectServerRpc(go);
    }
    [ServerRpc(RequireOwnership = true)]
    public static void SpawnNetworkObjectServerRpc(GameObject go) {
        go.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(GameObject go, int value) {
        var scanNode = go.gameObject.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
    }
}