using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    [SerializeField] private EnemyConfig config;

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private TimeLifeManager timeManager;

    [Header("Configuración de Zona")]
    [Tooltip("Define los límites de la zona donde el enemigo está activo. Usa al menos 3 puntos para crear un área.")]
    [SerializeField] private Transform[] zonePoints;

    // Estado del enemigo
    private enum EnemyState { Idle, Chase, Attack, Returning }
    private EnemyState currentState = EnemyState.Idle;

    // Variables internas
    private Rigidbody rb;
    private Renderer enemyRenderer;
    private float lastAttackTime = -999f;
    private float returnTimer = 0f;
    private Vector3 spawnPosition;
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    private bool canAttack = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemyRenderer = GetComponent<Renderer>();

        // Configurar Rigidbody
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;

        // Guardar posición inicial
        spawnPosition = transform.position;
        wanderTarget = spawnPosition;

        // Buscar referencias automáticamente si no están asignadas
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("EnemyAI: No se encontró el jugador. Asegúrate de que tenga el tag 'Player'");
        }

        if (timeManager == null)
        {
            timeManager = TimeLifeManager.Instance;
            if (timeManager == null)
                Debug.LogError("EnemyAI: No se encontró TimeLifeManager en la escena");
        }

        // Validar configuración
        if (config == null)
        {
            Debug.LogError("EnemyAI: No se asignó un EnemyConfig. Asigna uno en el Inspector.");
            enabled = false;
            return;
        }

        // Validar zona
        if (zonePoints == null || zonePoints.Length < 3)
        {
            Debug.LogWarning("EnemyAI: Se necesitan al menos 3 puntos para definir una zona. El enemigo usará detección de rango simple.");
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (player == null || config == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerInZone = IsPlayerInZone();

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState(distanceToPlayer, playerInZone);
                break;

            case EnemyState.Chase:
                HandleChaseState(distanceToPlayer, playerInZone);
                break;

            case EnemyState.Attack:
                HandleAttackState(distanceToPlayer);
                break;

            case EnemyState.Returning:
                HandleReturningState(distanceToPlayer, playerInZone);
                break;
        }

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (config == null) return;

        // Limitar velocidad horizontal
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float maxSpeed = currentState == EnemyState.Chase ? config.chaseSpeed : config.wanderSpeed;

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    // ===================================
    // ESTADOS DEL ENEMIGO
    // ===================================

    void HandleIdleState(float distanceToPlayer, bool playerInZone)
    {
        // Solo perseguir si el jugador está en la zona Y dentro del rango de detección
        if (playerInZone && distanceToPlayer <= config.detectionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // Movimiento de deambular opcional
        if (config.wanderInZone)
        {
            WanderInZone();
        }
        else
        {
            // Quedarse quieto
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleChaseState(float distanceToPlayer, bool playerInZone)
    {
        // Cambiar a ataque si está lo suficientemente cerca
        if (distanceToPlayer <= config.attackRange)
        {
            currentState = EnemyState.Attack;
            return;
        }

        // Dejar de perseguir si el jugador sale de la zona o está muy lejos
        if (!playerInZone || distanceToPlayer > config.losePlayerRange)
        {
            currentState = EnemyState.Returning;
            returnTimer = config.returnToZoneDelay;
            return;
        }

        ChasePlayer();
    }

    void HandleAttackState(float distanceToPlayer)
    {
        // Volver a perseguir si el jugador se aleja
        if (distanceToPlayer > config.attackRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // Atacar si el cooldown terminó
        if (canAttack && Time.time >= lastAttackTime + config.attackCooldown)
        {
            AttackPlayer();
        }

        // Mirar al jugador mientras ataca
        LookAtTarget(player.position);
    }

    void HandleReturningState(float distanceToPlayer, bool playerInZone)
    {
        // Volver a perseguir si el jugador regresa a la zona
        if (playerInZone && distanceToPlayer <= config.detectionRange)
        {
            currentState = EnemyState.Chase;
            returnTimer = 0f;
            return;
        }

        returnTimer -= Time.deltaTime;

        if (returnTimer <= 0f)
        {
            currentState = EnemyState.Idle;
            return;
        }

        // Volver al centro de la zona
        Vector3 zoneCenter = GetZoneCenter();
        MoveTowards(zoneCenter, config.wanderSpeed);

        // Si llegó cerca del centro, parar
        if (Vector3.Distance(transform.position, zoneCenter) < 2f)
        {
            currentState = EnemyState.Idle;
        }
    }

    // ===================================
    // COMPORTAMIENTOS
    // ===================================

    void WanderInZone()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            // Elegir nuevo punto aleatorio cerca de la posición actual
            Vector2 randomCircle = Random.insideUnitCircle * config.wanderRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Asegurarse de que esté dentro de la zona
            if (IsPointInZone(randomPoint))
            {
                wanderTarget = randomPoint;
            }
            else
            {
                // Si no está en la zona, moverse hacia el centro
                wanderTarget = GetZoneCenter();
            }

            wanderTimer = config.wanderChangeInterval;
        }

        // Moverse hacia el objetivo de deambular
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        if (distanceToTarget > 1f)
        {
            MoveTowards(wanderTarget, config.wanderSpeed);
        }
    }

    void ChasePlayer()
    {
        MoveTowards(player.position, config.chaseSpeed);
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;

        if (timeManager != null)
        {
            timeManager.LoseTime(config.timeDamage, transform.position);
            StartCoroutine(AttackCooldownVisual());
        }
    }

    IEnumerator AttackCooldownVisual()
    {
        canAttack = false;
        yield return new WaitForSeconds(config.attackCooldown);
        canAttack = true;
    }

    // ===================================
    // ZONA DE PATRULLAJE
    // ===================================

    bool IsPlayerInZone()
    {
        if (player == null) return false;

        // Si no hay puntos de zona definidos, usar detección por rango simple
        if (zonePoints == null || zonePoints.Length < 3)
        {
            return Vector3.Distance(transform.position, player.position) <= config.losePlayerRange;
        }

        return IsPointInZone(player.position);
    }

    bool IsPointInZone(Vector3 point)
    {
        if (zonePoints == null || zonePoints.Length < 3) return true;

        // Algoritmo de Ray Casting para verificar si un punto está dentro de un polígono
        int intersections = 0;

        for (int i = 0; i < zonePoints.Length; i++)
        {
            if (zonePoints[i] == null) continue;

            Vector3 p1 = zonePoints[i].position;
            Vector3 p2 = zonePoints[(i + 1) % zonePoints.Length].position;

            // Verificar si el rayo horizontal desde el punto cruza esta arista
            if ((p1.z > point.z) != (p2.z > point.z))
            {
                float xIntersection = (p2.x - p1.x) * (point.z - p1.z) / (p2.z - p1.z) + p1.x;
                if (point.x < xIntersection)
                {
                    intersections++;
                }
            }
        }

        // Si hay un número impar de intersecciones, el punto está dentro
        return (intersections % 2) == 1;
    }

    Vector3 GetZoneCenter()
    {
        if (zonePoints == null || zonePoints.Length == 0)
            return spawnPosition;

        Vector3 sum = Vector3.zero;
        int validPoints = 0;

        foreach (Transform point in zonePoints)
        {
            if (point != null)
            {
                sum += point.position;
                validPoints++;
            }
        }

        return validPoints > 0 ? sum / validPoints : spawnPosition;
    }

    // ===================================
    // MOVIMIENTO Y UTILIDADES
    // ===================================

    void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Solo movimiento horizontal

        if (direction.magnitude > 0.1f)
        {
            // Aplicar fuerza de movimiento
            Vector3 targetVelocity = direction * speed;
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentHorizontalVel;

            rb.AddForce(velocityChange * config.movementForceMultiplier, ForceMode.Force);

            // Rotar hacia el objetivo
            LookAtTarget(targetPosition);
        }
    }

    void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * config.rotationSpeed);
        }
    }

    void UpdateVisuals()
    {
        if (enemyRenderer == null || enemyRenderer.material == null || config == null) return;

        Color targetColor = config.idleColor;

        switch (currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Returning:
                targetColor = config.idleColor;
                break;
            case EnemyState.Chase:
                targetColor = config.chaseColor;
                break;
            case EnemyState.Attack:
                targetColor = canAttack ? config.attackColor : config.chaseColor;
                break;
        }

        enemyRenderer.material.color = targetColor;
    }

    // ===================================
    // COLISIONES
    // ===================================

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && currentState == EnemyState.Attack)
        {
            if (canAttack && Time.time >= lastAttackTime + config.attackCooldown)
            {
                AttackPlayer();
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && currentState == EnemyState.Attack)
        {
            if (canAttack && Time.time >= lastAttackTime + config.attackCooldown)
            {
                AttackPlayer();
            }
        }
    }
}