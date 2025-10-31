using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Pausa : MonoBehaviour
{
    [SerializeField] private GameObject Panel;
    protected bool playPause;
    private bool activo;

    void Update()
    {
        if (activo == false && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Pausar();
            activo = true;
        }
        else if (activo == true && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Reanudar(); 
            activo = false;
        }
    }
    public void Reanudar()
    {
        Panel.SetActive(false);
        Time.timeScale = 1;
        playPause = false;
    }
    public void Pausar()
    {
        Panel.SetActive(true);
        Time.timeScale = 0;
        playPause = true;
    }
}
