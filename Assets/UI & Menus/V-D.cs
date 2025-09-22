using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VD : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Accept();
        }
        else if (SceneManager.GetActiveScene().name == "Derrota" && Input.GetKeyDown(KeyCode.Space))
        {
            Reboot();
        }

    }
    public void Reboot()
    {
        SceneManager.LoadScene("Juego");
    }

    public void Accept()
    {
        SceneManager.LoadScene("Inicio");
    }

    public void Play()
    {
        SceneManager.LoadScene("Juego");
    }

    public void Salir()
    {
        Application.Quit();
    }
}