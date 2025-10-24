using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorAnimacion : MonoBehaviour
{
    public string tagObjeto;
    public Animator animator; 
    public string nombreParametro; 
    private GameObject objetoMasCercano; 

    private void Update()
    {
        animator.SetBool(nombreParametro, true);
        objetoMasCercano = FindClosestObjectWithTag(tagObjeto);
        if (objetoMasCercano != null)
        {
            bool estaALaIzquierda = objetoMasCercano.transform.position.x < transform.position.x;
            animator.SetBool(nombreParametro, estaALaIzquierda);
        }
        else
        {
            animator.SetBool(nombreParametro, false);
        }
    }
    private GameObject FindClosestObjectWithTag(string tag)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tag);
        GameObject objetoMasCercano = null;
        float distanciaMinima = Mathf.Infinity;

        foreach (GameObject objeto in objetos)
        {
            float distancia = Vector3.Distance(transform.position, objeto.transform.position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                objetoMasCercano = objeto;
            }
        }

        return objetoMasCercano;
    }
}

