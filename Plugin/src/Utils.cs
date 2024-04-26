﻿using GiantSpecimens.Scrap;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantSpecimens.src;
internal static class Utils
{
    static int seed = StartOfRound.Instance.randomMapSeed;
    static System.Random random = new System.Random(seed + 85);
    public static void SpawnScrap(Item item, Vector3 position) {        
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
    public static void UpdateScanNodeClientRpc(GameObject go, int value) {
        var scanNode = go.gameObject.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
    }
}