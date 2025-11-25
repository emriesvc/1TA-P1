using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración Movimiento")]
    public float velocidadCaminar = 2f;
    public float velocidadCorrer = 4f;
    public float velocidadAgachado = 1f;

    [Header("Configuración Salto")]
    public float fuerzaSaltoBase = 20f;
    public float multiplicadorSaltoCarrera = 0.5f;
    public float multiplicadorCorteSalto = 0.5f;
    public Transform checkSuelo;
    public float radioCheckSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Configuración Combate")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float fuerzaDisparoBase = 5f;
    public float cadenciaDisparo = 0.2f;
    public int municionMaxima = 15;
    public float tiempoRecarga = 2f;

    // Variables Privadas de Estado
    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaEnSuelo;
    private bool agachado;
    private bool corriendo;
    private bool saltando;

    // Variables de Combate
    private float siguienteDisparoTime = 0f;
    private int municionActual;
    private bool recargando = false;

    // Variable para el "Agachado" (Minecraft)
    public float distanciaBorde = 0.6f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        municionActual = municionMaxima;
    }

    void Update()
    {
        ProcesarInputs();
        ProcesarDisparo();
    }

    void FixedUpdate()
    {
        MoverJugador();
        ChequearSuelo();
    }

    void ProcesarInputs()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal"); // GetAxisRaw para respuesta más inmediata

        // Detectar teclas de estado
        corriendo = Input.GetKey(KeyCode.LeftShift);
        agachado = Input.GetKey(KeyCode.LeftControl);

        // Salto (Input Down) - Iniciar salto
        if (Input.GetButtonDown("Jump") && estaEnSuelo)
        {
            Saltar();
        }

        // Salto Variable (Input Up) - "Corte de salto" (Hollow Knight style)
        // Si soltamos el botón y estamos subiendo, cortamos la velocidad vertical
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * multiplicadorCorteSalto);
        }
    }

    void MoverJugador()
    {
        float velocidadActual = velocidadCaminar;

        if (agachado) velocidadActual = velocidadAgachado;
        else if (corriendo) velocidadActual = velocidadCorrer;

        //// Lógica "Minecraft Sneak" (No caerse de bordes si agachado y en suelo)
        //if (agachado && estaEnSuelo)
        //{
        //    if (inputHorizontal != 0)
        //    {
        //        // Predecir si habrá suelo en la dirección del movimiento
        //        // Movemos el checkBorde a la izquierda o derecha según input
        //        float direccion = Mathf.Sign(inputHorizontal);
        //        Vector2 posicionCheck = (Vector2)transform.position + (Vector2.right * direccion * 0.1f); // Ajusta el 0.5f al ancho de tu pj

        //        // Lanzamos un Raycast hacia abajo desde la posición futura
        //        bool haySueloDelante = Physics2D.Raycast(posicionCheck, Vector2.down, 0.2f, capaSuelo);

        //        if (!haySueloDelante)
        //        {
        //            // Si no hay suelo, paramos en seco (evita caer)
        //            inputHorizontal = 0;
        //            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        //            return;
        //        }
        //    }
        //}
        // Lógica "Minecraft Sneak" Mejorada
        if (agachado && estaEnSuelo && inputHorizontal != 0)
        {
            float direccionIntento = Mathf.Sign(inputHorizontal);

            // Usamos distanciaBorde para mirar hacia dónde vamos
            Vector2 origenRayo = (Vector2)transform.position + (Vector2.right * direccionIntento * distanciaBorde);

            bool haySueloFuturo = Physics2D.Raycast(origenRayo + Vector2.up * 0.2f, Vector2.down, 1.5f, capaSuelo);

            // Debug visual para ver el rayo rojo en la escena
            Debug.DrawRay(origenRayo + Vector2.up * 0.2f, Vector2.down * 1.5f, Color.red);

            if (!haySueloFuturo)
            {
                inputHorizontal = 0;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }

        // Aplicar movimiento estándar
        rb.linearVelocity = new Vector2(inputHorizontal * velocidadActual, rb.linearVelocity.y);
    }

    void Saltar()
    {
        float fuerzaTotal = fuerzaSaltoBase;

        // Transformación de energía cinética a potencial (Salto con inercia)
        if (Mathf.Abs(rb.linearVelocity.x) > velocidadCaminar)
        {
            // A mayor velocidad X, mayor salto. Usamos una fórmula simple pero efectiva.
            float bonusInercia = (Mathf.Abs(rb.linearVelocity.x) - velocidadCaminar) * multiplicadorSaltoCarrera;
            fuerzaTotal += bonusInercia;
        }

        // Si estamos agachados, quizás el salto es menor (opcional)
        if (agachado) fuerzaTotal *= 0.8f;

        // Aplicar fuerza (Impulso instantáneo)
        rb.AddForce(Vector2.up * fuerzaTotal, ForceMode2D.Impulse);
    }

    void ProcesarDisparo()
    {
        // Lógica de disparo
        if (Input.GetButton("Fire1") && Time.time >= siguienteDisparoTime && !recargando)
        {
            if (municionActual > 0)
            {
                Disparar();
                siguienteDisparoTime = Time.time + cadenciaDisparo;
            }
            else
            {
                StartCoroutine(Recargar());
            }
        }
    }

    void Disparar()
    {
        municionActual--;

        // Instanciar bala
        GameObject bala = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.identity);
        Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();

        // Calcular dirección (Derecha o Izquierda según input o velocidad actual)
        float direccion = transform.localScale.x;
        if (inputHorizontal != 0) direccion = Mathf.Sign(inputHorizontal);

        // Física del disparo:
        // 1. Velocidad base del disparo en X e Y (para la parábola)
        Vector2 velocidadDisparo = new Vector2(direccion * fuerzaDisparoBase, fuerzaDisparoBase * 0.5f); // Un poco hacia arriba para arco

        // 2. HERENCIA DE INERCIA: Sumamos la velocidad actual del jugador a la bala
        // Esto hace que si corres, la bala llegue más lejos.
        rbBala.linearVelocity = velocidadDisparo + (Vector2)rb.linearVelocity;
    }

    IEnumerator Recargar()
    {
        recargando = true;
        Debug.Log("Recargando...");
        // Aquí podrías poner una animación o sonido

        yield return new WaitForSeconds(tiempoRecarga);

        municionActual = municionMaxima;
        recargando = false;
        Debug.Log("¡Recarga completa!");
    }

    void ChequearSuelo()
    {
        estaEnSuelo = Physics2D.OverlapCircle(checkSuelo.position, radioCheckSuelo, capaSuelo);
    }

    // Dibujar Gizmos para ver los checks en el editor (Incluso sin dar al Play)
    void OnDrawGizmos()
    {
        // 1. DIBUJAR CHECK SUELO (Círculo Rojo)
        if (checkSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(checkSuelo.position, radioCheckSuelo);
        }

        // 2. DIBUJAR LÍMITES DE BORDE (Líneas Amarillas)
        // Esto te mostrará dónde comprobará el juego si hay suelo
        Gizmos.color = Color.yellow;

        // Calculamos dónde caerían los rayos a derecha e izquierda
        Vector2 centro = transform.position;
        Vector2 origenDerecha = centro + (Vector2.right * distanciaBorde) + (Vector2.up * 0.2f);
        Vector2 origenIzquierda = centro + (Vector2.left * distanciaBorde) + (Vector2.up * 0.2f);

        // Dibujamos las líneas hacia abajo (longitud 0.5f + 0.2f de offset = 0.7f aprox visual)
        Gizmos.DrawLine(origenDerecha, origenDerecha + Vector2.down * 0.7f);
        Gizmos.DrawLine(origenIzquierda, origenIzquierda + Vector2.down * 0.7f);
    }
}