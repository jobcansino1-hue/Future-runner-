using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class RobotRunner2300Pro : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 10f;
    public float velocidadLateral = 5f;
    public float velocidadGiro = 10f;
    public float salto = 8f;
    public float alturaMaxSalto = 4f;
    
    [Header("Deslizamiento (estilo Spiderman Unlimited)")]
    public float tiempoDeslizamiento = 0.8f;
    public float alturaNormal = 2f;
    public float alturaDeslizamiento = 1f;
    public Transform modelo3D; // 🆕 Asigna aquí tu personaje‑hijo 3D

    [Header("Carcel")]
    public float tiempoCarcel = 10f;
    public TextMeshProUGUI textoContador;
    public string escenaCarcel = "Carcel";

    private Rigidbody rb;
    private CapsuleCollider col;
    private bool enSuelo = true;
    private bool caido = false;
    private bool deslizando = false;
    private float tiempoRestante;
    private float alturaInicioSalto;
    private float timerDeslizamiento;
    private Vector3 escalaNormalModelo;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        if (rb == null || col == null)
        {
            Debug.LogError("Falta Rigidbody o CapsuleCollider en: " + gameObject.name);
            enabled = false;
            return;
        }

        if (!gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("RECUERDA: Ponle la etiqueta Player a este objeto");
        }
        if (col.isTrigger)
        {
            Debug.LogWarning("RECUERDA: El CapsuleCollider NO debe tener Is Trigger activado");
            col.isTrigger = false;
        }

        if (modelo3D != null)
            escalaNormalModelo = modelo3D.localScale;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true;
        tiempoRestante = tiempoCarcel;
        
        if (textoContador != null)
            textoContador.gameObject.SetActive(false);

        if (!Application.CanStreamedLevelBeLoaded(escenaCarcel))
            Debug.LogError("¡Aviso! La escena '" + escenaCarcel + "' no está en Build Settings");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo && !deslizando)
        {
            rb.AddForce(Vector3.up * salto, ForceMode.Impulse);
            alturaInicioSalto = transform.position.y;
            enSuelo = false;
        }

        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.DownArrow)) && enSuelo && !deslizando)
        {
            IniciarDeslizamiento();
        }

        if (!enSuelo && rb != null)
        {
            float alturaActual = transform.position.y - alturaInicioSalto;
            if (alturaActual > alturaMaxSalto && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }

        if (deslizando)
        {
            timerDeslizamiento -= Time.deltaTime;
            if (timerDeslizamiento <= 0f) TerminarDeslizamiento();
        }

        if (caido)
        {
            tiempoRestante -= Time.deltaTime;
            tiempoRestante = Mathf.Max(tiempoRestante, 0f);
            if (textoContador != null)
            {
                textoContador.gameObject.SetActive(true);
                textoContador.text = $"¡POLIS! {Mathf.CeilToInt(tiempoRestante)}s ¡SUBE!";
            }
            if (tiempoRestante <= 0f) SceneManager.LoadScene(escenaCarcel);
        }

        if (transform.position.y < -5f && !caido) { caido = true; tiempoRestante = tiempoCarcel; }
        if (transform.position.y > 1f && caido) { caido = false; if(textoContador) textoContador.gameObject.SetActive(false); }
    }

    void FixedUpdate()
    {
        float mult = caido ? 1.5f : 1f;
        if (deslizando) mult *= 1.2f;

        float horizontal = Input.GetAxis("Horizontal");
        Vector3 movimiento = (Vector3.forward * velocidad * mult) + (Vector3.right * horizontal * velocidadLateral * mult);
        rb.MovePosition(rb.position + movimiento * Time.fixedDeltaTime);

        if (enSuelo && movimiento.sqrMagnitude > 0.1f)
        {
            Quaternion objetivoGiro = Quaternion.LookRotation(movimiento.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, objetivoGiro, velocidadGiro * Time.fixedDeltaTime));
        }
    }

    void IniciarDeslizamiento()
    {
        deslizando = true;
        timerDeslizamiento = tiempoDeslizamiento;
        col.height = alturaDeslizamiento;
        col.center = new Vector3(0, alturaDeslizamiento / 2f, 0);
        if (modelo3D != null)
            modelo3D.localScale = new Vector3(escalaNormalModelo.x, escalaNormalModelo.y * 0.5f, escalaNormalModelo.z);
    }

    void TerminarDeslizamiento()
    {
        deslizando = false;
        col.height = alturaNormal;
        col.center = new Vector3(0, alturaNormal / 2f, 0);
        if (modelo3D != null)
            modelo3D.localScale = escalaNormalModelo;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ObstaculoBajo"))
        {
            if (enSuelo && !deslizando)
            {
                rb.AddForce(-Vector3.forward * 12f, ForceMode.Impulse);
            }
        }
        if (other.CompareTag("ObstaculoAlto"))
        {
            if (!deslizando)
            {
                rb.AddForce(-Vector3.forward * 12f, ForceMode.Impulse);
            }
        }
    }

    void OnCollisionEnter(Collision colision)
    {
        if (colision.gameObject.CompareTag("Suelo") || colision.gameObject.CompareTag("Edificio"))
        {
            enSuelo = true;
            alturaInicioSalto = transform.position.y;
        }
    }

    void OnCollisionExit(Collision colision)
    {
        if (colision.gameObject.CompareTag("Suelo") || colision.gameObject.CompareTag("Edificio"))
        {
            enSuelo = false;
        }
    }
}
