using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GameObjectHasher
{
    public int HashGameObject(GameObject gameObject)
    {
        int hash = 17;
        int transformHash = HashTransform(gameObject.transform);

        hash = hash * 23 + transformHash;
        //put more here if we need more
        //make sure that those values aren't changable
        return hash;
    }
    private int HashTransform(Transform transform)
    {
        unchecked
        {
            int hashPos = 17;
            int hashRot = 17;
            int hashScl = 17;
            int hash = 17;

            hashPos = hashPos * 23 + transform.localPosition.x.GetHashCode();
            hashPos = hashPos * 23 + transform.localPosition.y.GetHashCode();
            hashPos = hashPos * 23 + transform.localPosition.z.GetHashCode();

            hashRot = hashRot * 23 + transform.localRotation.w.GetHashCode();
            hashRot = hashRot * 23 + transform.localRotation.x.GetHashCode();
            hashRot = hashRot * 23 + transform.localRotation.y.GetHashCode();
            hashRot = hashRot * 23 + transform.localRotation.z.GetHashCode();

            hashScl = hashScl * 23 + transform.localScale.x.GetHashCode();
            hashScl = hashScl * 23 + transform.localScale.y.GetHashCode();
            hashScl = hashScl * 23 + transform.localScale.z.GetHashCode();

            hash = hash * 23 + hashPos;
            hash = hash * 23 + hashRot;
            hash = hash * 23 + hashScl;

            hash = hash * 23 + transform.childCount;
            return hash;
        }
    }
}