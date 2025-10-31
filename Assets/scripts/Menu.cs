using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class VD : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Accept();
        }
        else if (SceneManager.GetActiveScene().name == "Derrota" && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Reboot();
        }
    }

    public void Reboot()
    {
        SceneManager.LoadScene("Level1");
    }

    public void Accept()
    {
        SceneManager.LoadScene("Inicio");
    }

    public void Play()
    {
        SceneManager.LoadScene("Level1");
    }

    public void Salir()
    {
        Application.Quit();
    }
}