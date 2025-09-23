using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    //Movimiento
    [SerializeField] private float velocidadMovimiento = 5.0f;
    [SerializeField] private float velocidadRotacion = 200.0f;
    private Animator anim;
    [SerializeField] private float x, y;

    //Salto
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float fuerzaDeSalto = 4f;
    public bool Saltar;
    [SerializeField] private float timer = 4f;

    void Start()
    {
        Saltar = false;
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        transform.Rotate(0, x * Time.deltaTime * velocidadRotacion, 0);
        transform.Translate(0, 0, y * Time.deltaTime * velocidadMovimiento);
    }
    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        anim.SetFloat("VelX", x);
        anim.SetFloat("VelY", y);

        if (Saltar)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                anim.SetBool("Salto", true);
                timer = Time.deltaTime;
                rb.AddForce(new Vector3(0, fuerzaDeSalto, 0), ForceMode.Impulse);
            }
            anim.SetBool("Suelo", true);
        }
        else
        {
            EstoyCayendo();
        }
    }

    void EstoyCayendo()
    {
        anim.SetBool("Suelo", false);
        anim.SetBool("Salto", false);
    }
}
