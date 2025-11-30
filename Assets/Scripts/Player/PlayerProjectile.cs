using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public float tiempoDeVida = 2f;

    void Start()
    {
        // Destruir después de X segundos (si no choca antes)
        Destroy(gameObject, tiempoDeVida);

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Aquí podrías añadir lógica para dañar al enemigo
        if (collision.gameObject.CompareTag("Enemigo"))
        {
            // collision.gameObject.GetComponent<Enemigo>().RecibirDaño();
        }

        // Destruir al tocar cualquier cosa (suelo o enemigo)
        Destroy(gameObject);
    }
}