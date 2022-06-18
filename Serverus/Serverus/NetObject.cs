using UnityEngine;

public class NetObject : MonoBehaviour
{
    public int id { get; set; }
    void Start()
    {
        this.id = this.gameObject.GetInstanceID();
    }
}
