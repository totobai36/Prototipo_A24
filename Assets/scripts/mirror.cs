using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorAnimacion : MonoBehaviour
{
    public string tagObjeto ; // El tag del objeto que quieres verificar
    public Animator animator; // El componente Animator del personaje
    public string nombreParametro; // El nombre del parámetro bool que controla el mirror en el Animator

    private GameObject objetoMasCercano; // El objeto más cercano con el tag especificado

    private void Update()
    {
        // Busca el objeto más cercano con el tag especificado
        objetoMasCercano = FindClosestObjectWithTag(tagObjeto);

        // Verifica si se encontró un objeto
        if (objetoMasCercano != null)
        {
            // Verifica si el objeto está a la izquierda o derecha del personaje
            bool estaALaIzquierda = objetoMasCercano.transform.position.x < transform.position.x;

            // Establece el valor del parámetro bool
            animator.SetBool(nombreParametro, estaALaIzquierda);
        }
        else
        {
            // Si no se encontró un objeto, puedes establecer un valor por defecto para el parámetro bool
            animator.SetBool(nombreParametro, false);
        }
    }

    // Función para encontrar el objeto más cercano con un tag específico
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
