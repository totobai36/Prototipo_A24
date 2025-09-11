using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{


    [SerializeField] float m_speed = 8f;
    [SerializeField] float m_sprintSpeed = 10f;
    float m_currentSpeed;

    [SerializeField] float jumpForce;
    bool isGrounded;

    [SerializeField] float wallRunSpeed = 6f;
    [SerializeField] float wallRunDuration = 1.5f;
    [SerializeField] float wallCheckDistance = 0.8f;
    [SerializeField] float wallJumpForce = 8f;
    string wallRunTag = "WallRun";

    bool isWallRunning = false;
    bool isTouchingWall = false;
    float wallRunTimer = 0f;
    Vector3 wallNormal;


    public Camera playerCamera;
    Vector3 m_movement;
    Rigidbody m_playerRigidbody;

    float m_horz = 0f;
    float m_vert = 0f;

    public bool canMove = true;

    void Start()
    {
        m_playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)&& canMove)
        {
            if(isWallRunning)
            {
                WallJump();
            }
            else if (isGrounded)
            {
                Jump();
            }
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        CheckForWalls();

        if (isTouchingWall && !isGrounded && m_vert > 0)
        {
            StartWallRun();
        }
        else
        {
            StopWallRun();
        }
        if (!isWallRunning)
        {
            m_horz = Input.GetAxis("Horizontal");
            m_vert = Input.GetAxis("Vertical");

            m_currentSpeed = Input.GetKey(KeyCode.LeftShift) ? m_sprintSpeed : m_speed;
            Move();
        }
    }
    void Move()
    {
         m_movement.Set(m_horz, 0f, m_vert);
         m_movement = m_movement.normalized * m_currentSpeed * Time.deltaTime;
         m_playerRigidbody.MovePosition(transform.position + m_movement);
    }

    void Jump()
    {
        Vector3 velocity = m_playerRigidbody.velocity;
        velocity.y = 0;
        m_playerRigidbody.velocity = velocity;

        m_playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }


    void CheckForWalls()
    {
        RaycastHit hit;
        isTouchingWall = false;

        if (Physics.Raycast(transform.position, transform.right, out hit, wallCheckDistance))
        {
            if (hit.collider.CompareTag(wallRunTag))
            {
                isTouchingWall = true;
                wallNormal = hit.normal;
            }
        }
        // Raycast izquierda
        else if (Physics.Raycast(transform.position, -transform.right, out hit, wallCheckDistance))
        {
            if (hit.collider.CompareTag(wallRunTag))
            {
                isTouchingWall = true;
                wallNormal = hit.normal;
            }
        }
    }
    void StartWallRun()
    {
        if (!isWallRunning)
        {
            isWallRunning = true;
            wallRunTimer = wallRunDuration;
            m_playerRigidbody.useGravity = false;
        }

        wallRunTimer -= Time.fixedDeltaTime;
        if (wallRunTimer <= 0f) StopWallRun();

        // Movimiento a lo largo de la pared
        Vector3 alongWall = Vector3.Cross(wallNormal, Vector3.up);
        m_playerRigidbody.velocity = alongWall.normalized * wallRunSpeed;
    }

    void StopWallRun()
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            m_playerRigidbody.useGravity = true;
        }
    }
    void WallJump()
    {
        StopWallRun();
        Vector3 jumpDirection = wallNormal + Vector3.up;
        m_playerRigidbody.velocity = Vector3.zero;
        m_playerRigidbody.AddForce(jumpDirection.normalized * wallJumpForce, ForceMode.Impulse);
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
            isGrounded = true;
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
            isGrounded = false;
    }
}
