using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public bool hasKey = false;

    public void GetKey()
    {
        hasKey = true;
        Debug.Log("열쇠를 획득했다.");
    }
}