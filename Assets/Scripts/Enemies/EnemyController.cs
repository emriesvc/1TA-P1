using UnityEditor.Search;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Enumeración para definir los estados del enemigo
    public enum EnemyState { Patrolling, Chasing }
    [Header("Estado Actual")]
    public EnemyState currentState = EnemyState.Patrolling;

    // --- Variables de Ajuste ---
    [Header("Movimiento")]
    [Tooltip("La velocidad a la que el enemigo patrulla.")]
    public float patrolSpeed = 3f;
    [Tooltip("La velocidad extra cuando el enemigo persigue al jugador.")]
    public float chaseSpeedMultiplier = 1.5f;
    [Tooltip("La fuerza vertical para saltar al chocar.")]
    public float jumpForce = 5f;

    [Header("Detección y Persecución")]
    [Tooltip("Radio de detección. Si el jugador entra en este rango, el enemigo lo persigue.")]
    public float detectionRange = 5f;
    [Tooltip("Distancia mínima para dejar de perseguir si el jugador está demasiado cerca (para evitar temblores).")]
    public float minChaseDistance = 0.5f;

    [Header("Comportamiento de Patrulla")]
    [Tooltip("Posibilidad de que el enemigo salte al chocar contra algo (0.0 a 1.0).")]
    [Range(0f, 1f)] public float jumpChanceOnHit = 0.5f;

    // --- Componentes ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform playerTarget; // Referencia al Transform del jugador

    // --- Estado ---
    private float moveDirection = 1f; // 1f para derecha, -1f para izquierda

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            Debug.LogError("Se requiere un Rigidbody2D en este enemigo.");
            enabled = false;
            return;
        }

        // 1. Encontrar al Jugador por Etiqueta (Player)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }

        // Inicializar dirección aleatoria
        if (Random.value < 0.5f)
        {
            moveDirection = -1f;
        }
    }

    void FixedUpdate()
    {
        // 1. CHEQUEO DE DETECCIÓN (Solo si hay un jugador para perseguir)
        if (playerTarget != null)
        {
            CheckForPlayer();
        }

        // 2. LÓGICA BASADA EN ESTADO
        if (currentState == EnemyState.Patrolling)
        {
            PatrolMovement();
        }
        else if (currentState == EnemyState.Chasing)
        {
            ChaseMovement();
        }

        // 3. ACTUALIZAR VISUALES
        // Voltear el sprite según la dirección actual
        if (sr != null)
        {
            sr.flipX = moveDirection < 0;
        }
    }

    // -------------------------------------------------------------------
    // --- LÓGICA DE ESTADOS Y MOVIMIENTO ---
    // -------------------------------------------------------------------

    void CheckForPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= detectionRange)
        {
            // ¡Jugador detectado! Cambiar a modo persecución.
            currentState = EnemyState.Chasing;
        }
        else if (currentState == EnemyState.Chasing && distanceToPlayer > detectionRange)
        {
            // El jugador ha escapado del rango de detección. Volver a patrullar.
            currentState = EnemyState.Patrolling;
            // Al volver a Patrullar, elegimos una dirección aleatoria para que no quede estático
            if (Random.value < 0.5f)
            {
                moveDirection = 1f;
            }
            else
            {
                moveDirection = -1f;
            }
        }
    }

    void PatrolMovement()
    {
        // Usamos moveDirection (que se invierte en OnCollisionEnter2D) y patrolSpeed
        rb.linearVelocity = new Vector2(moveDirection * patrolSpeed, rb.linearVelocity.y);
    }

    void ChaseMovement()
    {
        // Determinar la dirección hacia el jugador
        float targetX = playerTarget.position.x;
        float currentX = transform.position.x;

        // Si el enemigo está demasiado cerca del jugador, no lo mueva horizontalmente
        if (Mathf.Abs(targetX - currentX) < minChaseDistance)
        {
            moveDirection = 0f; // Detener movimiento horizontal
        }
        else
        {
            // Establecer la dirección a 1 o -1 según la posición del jugador
            moveDirection = (targetX > currentX) ? 1f : -1f;
        }

        // Aplicar la velocidad de persecución
        float currentSpeed = patrolSpeed * chaseSpeedMultiplier;
        rb.linearVelocity = new Vector2(moveDirection * currentSpeed, rb.linearVelocity.y);
    }

    // -------------------------------------------------------------------
    // --- LÓGICA DE COLISIÓN (Solo Patrulla) ---
    // -------------------------------------------------------------------

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // La lógica de colisión (cambiar dirección y saltar) solo debe ocurrir
        // si el enemigo está PATRULLANDO.
        if (currentState == EnemyState.Patrolling)
        {
            // Si choca con el jugador, no queremos que cambie de dirección, así que ignoramos
            if (collision.gameObject.CompareTag("Player"))
            {
                return;
            }

            // Cambiar dirección y Saltar
            ChangeDirection();

            if (Random.value < jumpChanceOnHit)
            {
                Jump();
            }
        }
    }

    // -------------------------------------------------------------------
    // --- Funciones Auxiliares ---
    // -------------------------------------------------------------------

    void ChangeDirection()
    {
        moveDirection *= -1;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
    }

    // Para ver el radio de detección en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
