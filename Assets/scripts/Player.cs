using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento = 5.0f;
    [SerializeField] private float velocidadRotacion = 200.0f;
    private Animator anim;
    [SerializeField] private float x, y;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        transform.Rotate(0, x * Time.deltaTime * velocidadRotacion, 0);
        transform.Translate(0, 0, y * Time.deltaTime * velocidadMovimiento);

        anim.SetFloat("VelX", x);
        anim.SetFloat("VelY", y);
    }
}
