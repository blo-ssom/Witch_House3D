using UnityEngine;
using System.Collections.Generic;
public class PlayerInventory : MonoBehaviour
{
    private HashSet<KeyType> ownedKeys = new HashSet<KeyType>();

    public void AddKey(KeyType keyType)
    {
        if (keyType == KeyType.None) return;

        ownedKeys.Add(keyType);
        Debug.Log($"{keyType} 열쇠 획득");
    }

    public bool HasKey(KeyType keyType)
    {
        if (keyType == KeyType.None)
            return true;

        return ownedKeys.Contains(keyType);
    } 
}