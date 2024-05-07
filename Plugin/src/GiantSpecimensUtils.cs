using Unity.Netcode;
using UnityEngine;

namespace GiantSpecimens.src;
internal class GiantSpecimensUtils : NetworkBehaviour
{
    static int seed = 0;
    static System.Random random;
    internal static GiantSpecimensUtils Instance { get; set; }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position) {
        if (StartOfRound.Instance == null)
        {
            Plugin.Logger.LogInfo("StartOfRound null");
            return;
        }
        if (random == null)
        {
            Plugin.Logger.LogInfo("Initializing random");
            seed = StartOfRound.Instance.randomMapSeed;
            random = new System.Random(seed + 85);
        }

        if (itemName.Length == 0)
        {
            Plugin.Logger.LogInfo("itemName is empty");
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null)
        {
            Plugin.Logger.LogInfo($"Could not get Item {itemName}");
            return;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up, Quaternion.identity);
        int value = random.Next(minValue: item.minValue, maxValue: item.maxValue);
        var grabNode = go.gameObject.GetComponentInChildren<GrabbableObject>();
        grabNode.SetScrapValue(value);
        go.GetComponent<NetworkObject>().Spawn(false);
        UpdateScanNodeClientRpc(go.GetComponent<NetworkObject>(), value);
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value) {
        go.TryGet(out NetworkObject netObj);
        if (netObj != null)
        {
            var grabNode = netObj.gameObject.GetComponentInChildren<GrabbableObject>();
            grabNode.SetScrapValue(value);
        }
    }
}