using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Config", menuName = "Game/Enemy Configuration")]
public class EnemyConfig : ScriptableObject
{
    [Header("Configuración de Idle")]
    [Tooltip("Si está activo, el enemigo se moverá lentamente en su zona")]
    public bool wanderInZone = false;
    [Range(0.5f, 3f)]
    public float wanderSpeed = 1f;
    [Range(1f, 10f)]
    public float wanderRadius = 3f;
    [Range(2f, 10f)]
    public float wanderChangeInterval = 5f;

    [Header("Configuración de Persecución")]
    [Range(3f, 12f)]
    [Tooltip("Velocidad al perseguir al jugador")]
    public float chaseSpeed = 5f;
    [Range(5f, 30f)]
    [Tooltip("Distancia a la que detecta al jugador")]
    public float detectionRange = 15f;
    [Range(15f, 50f)]
    [Tooltip("Distancia a la que deja de perseguir")]
    public float losePlayerRange = 25f;
    [Range(1f, 10f)]
    [Tooltip("Segundos antes de volver a patrullar")]
    public float returnToZoneDelay = 3f;

    [Header("Configuración de Ataque")]
    [Range(1f, 30f)]
    [Tooltip("Segundos que resta del tiempo del jugador")]
    public float timeDamage = 10f;
    [Range(0.5f, 5f)]
    [Tooltip("Segundos entre ataques")]
    public float attackCooldown = 2f;
    [Range(1f, 3f)]
    [Tooltip("Distancia para atacar")]
    public float attackRange = 1.5f;

    [Header("Configuración Visual")]
    public Color idleColor = Color.green;
    public Color chaseColor = Color.red;
    public Color attackColor = Color.yellow;

    [Header("Configuración de Física")]
    [Range(5f, 20f)]
    public float movementForceMultiplier = 10f;
    [Range(1f, 10f)]
    public float rotationSpeed = 5f;
}
