using UnityEngine;

public class Pies : MonoBehaviour
{
    public Player player;
    
    private void OnTriggerStay(Collider other)
    {
        if (!player.IsWallRunning())
        {
            player.Saltar = true;
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        player.Saltar = false;
    }
}
