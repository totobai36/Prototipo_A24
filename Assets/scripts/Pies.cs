using UnityEngine;

public class Pies : MonoBehaviour
{
    public Player player;
    
    private void OnTriggerStay(Collider other)
    {
        player.Saltar = true;
    }

    private void OnTriggerExit(Collider other)
    {
        player.Saltar = false;
    }
}
