using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Editor
{
    [ExecuteInEditMode]
    public class EditDiffer : MonoBehaviour
    {
        public uint fileId;
        public ulong pathId;
        public ulong origPathId;
        public bool newAsset;
        public static ulong lastId = 0;
        public static HashSet<ulong> usedIds = new HashSet<ulong>();
        [SerializeField]
        int instanceId = 0;
        public void Awake()
        {
            if (Application.isPlaying)
                return;
            if (instanceId == 0)
            {
                instanceId = GetInstanceID();
                newAsset = false;
                return;
            }
            if (instanceId != GetInstanceID() && GetInstanceID() < 0)
            {
                pathId = NextPathID();
                instanceId = GetInstanceID();
                newAsset = true;
            }
        }
        public ulong NextPathID()
        {
            ulong nextPathId = 1;
            while (usedIds.Contains(nextPathId))
            {
                nextPathId++;
            }
            usedIds.Add(nextPathId);
            lastId = nextPathId;
            return nextPathId;
        }
    }
}
