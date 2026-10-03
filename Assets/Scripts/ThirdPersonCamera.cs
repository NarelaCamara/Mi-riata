using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string targetName = "Rata-model";
    [Tooltip("Opcional: si está vacío no se usa. Usa FindWithTag si no se encuentra por nombre.")]
    public string targetTag = "";

    [Header("Rigidbody (Rata-model)")]
    [Tooltip("Si true, añadirá/configurará un Rigidbody en el target cuando se asigne.")]
    public bool ensureRigidbodyOnTarget = true;
    [Tooltip("Si true se sobrescriben las propiedades del Rigidbody existente.")]
    public bool overrideExistingRigidbody = false;
    public bool rbUseGravity = true;
    public bool rbIsKinematic = false;
    public float rbMass = 1f;
    public RigidbodyConstraints rbConstraints = RigidbodyConstraints.None;

    [Header("Posicionamiento")]
    public float distance = 4f;
    public float height = 1.5f;
    public float minDistance = 0.6f;
    public LayerMask occlusionLayers = ~0; // capas para detectar colisiones de cámara
    [Tooltip("Si es true, excluye automáticamente la capa del target de occlusionLayers.")]
    public bool excludeTargetLayerFromOcclusion = true;

    [Header("Rotación")]
    public float rotationSpeed = 200f; // sensibilidad del ratón
    public Vector2 pitchLimits = new Vector2(-30f, 60f);
    public bool invertY = false;

    [Header("Suavizado")]
    public float followSmoothTime = 0.08f;

    // Búsqueda periódica si el objetivo no existe en el Start
    [Header("Búsqueda")]
    [Tooltip("Intervalo en segundos para reintentar encontrar el objetivo por nombre o tag si no está asignado.")]
    public float searchInterval = 0.5f;

    Vector3 velocity = Vector3.zero;
    float yaw;
    float pitch;
    Vector3 desiredPosition;

    float searchTimer = 0f;
    bool excludedLayerApplied = false;

    Rigidbody targetRigidbody = null;

    void OnValidate()
    {
        // Asegurar valores válidos en el inspector
        if (distance < 0f) distance = 0f;
        if (minDistance < 0f) minDistance = 0f;
        if (minDistance > distance) minDistance = distance;
        if (followSmoothTime <= 0f) followSmoothTime = 0.01f;
        if (rotationSpeed <= 0f) rotationSpeed = 1f;
        if (searchInterval < 0.1f) searchInterval = 0.1f;
        if (rbMass <= 0f) rbMass = 1f;
    }

    void Start()
    {
        // Intento inicial de asignar target (Rata-model)
        TryAssignTargetAtStart();

        // Si se asignó target en Start, asegurar Rigidbody si está habilitado
        if (target != null)
            EnsureRigidbodyForTarget();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // Lógica de entrada mínima (la actualización principal de la cámara ocurre en LateUpdate)
    }

    void TryAssignTargetAtStart()
    {
        if (target != null) return;

        GameObject go = GameObject.Find(targetName);
        if (go == null && !string.IsNullOrEmpty(targetTag))
        {
            try
            {
                go = GameObject.FindWithTag(targetTag);
            }
            catch
            {
                // tag inexistente o error: ignorar
            }
        }

        if (go != null)
        {
            target = go.transform;
            ApplyIgnoreTargetLayer();
            SnapBehindImmediate();
            EnsureRigidbodyForTarget();
        }
    }

    void LateUpdate()
    {
        // Reintentar encontrar el objetivo periódicamente si no está asignado
        if (target == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                GameObject go = GameObject.Find(targetName);
                if (go == null && !string.IsNullOrEmpty(targetTag))
                {
                    try { go = GameObject.FindWithTag(targetTag); } catch { go = null; }
                }
                if (go != null)
                {
                    target = go.transform;
                    ApplyIgnoreTargetLayer();
                    // Al encontrarlo, reposicionar para evitar saltos
                    SnapBehindImmediate();
                    EnsureRigidbodyForTarget();
                }
            }
            return;
        }

        // Si el target existe pero fue destruido entre frames
        if (target == null) return;

        // Entrada del ratón (usa el sistema de Input clásico)
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * rotationSpeed * Time.deltaTime;
        pitch += (invertY ? 1f : -1f) * my * rotationSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // posición objetivo (altura sobre el jugador)
        Vector3 targetOffset = target.position + Vector3.up * height;
        desiredPosition = targetOffset - rot * Vector3.forward * Mathf.Max(distance, minDistance);

        // protección: evitar normalizar un vector cercano a cero
        Vector3 dirVec = desiredPosition - targetOffset;
        float dirMag = dirVec.magnitude;
        if (dirMag > 1e-4f)
        {
            Vector3 dir = dirVec / dirMag;

            // detección de colisión/oclusión para evitar que la cámara atraviese paredes
            RaycastHit hit;
            float rayDist = Mathf.Max(distance, minDistance);
            if (Physics.SphereCast(targetOffset, 0.2f, dir, out hit, rayDist, occlusionLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(hit.distance - 0.1f, minDistance);
                desiredPosition = targetOffset + dir * safeDistance;
            }
        }
        else
        {
            // Si dirMag es 0 forzamos una posición segura atrás
            desiredPosition = targetOffset - rot * Vector3.forward * Mathf.Max(minDistance, 0.1f);
        }

        // suavizado de movimiento y rotación
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10f * Time.deltaTime);
    }

    // Llamar si quieres reposicionar la cámara inmediatamente detrás del jugador (suavizado desactivado)
    public void SnapBehind()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
    }

    // Reposiciona inmediatamente la cámara para evitar saltos cuando el target se asigna en tiempo de ejecución
    void SnapBehindImmediate()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetOffset = target.position + Vector3.up * height;
        Vector3 immediatePos = targetOffset - rot * Vector3.forward * Mathf.Max(distance, minDistance);
        transform.position = immediatePos;
        transform.rotation = rot;
    }

    void ApplyIgnoreTargetLayer()
    {
        if (!excludeTargetLayerFromOcclusion || target == null || excludedLayerApplied) return;

        int layer = target.gameObject.layer;
        int mask = occlusionLayers;
        mask &= ~(1 << layer);
        occlusionLayers = mask;
        excludedLayerApplied = true;
        Debug.Log($"ThirdPersonCamera: excluida capa {layer} del target '{target.name}' de occlusionLayers.");
    }

    // Ensure Rigidbody presence/configuration on the target
    void EnsureRigidbodyForTarget()
    {
        if (target == null || !ensureRigidbodyOnTarget) return;

        GameObject go = target.gameObject;
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.mass = rbMass;
            rb.useGravity = rbUseGravity;
            rb.isKinematic = rbIsKinematic;
            rb.constraints = rbConstraints;
            targetRigidbody = rb;
            Debug.Log($"ThirdPersonCamera: añadido Rigidbody al target '{go.name}'.");
            return;
        }

        if (!overrideExistingRigidbody)
        {
            targetRigidbody = rb;
            Debug.Log($"ThirdPersonCamera: Rigidbody existente detectado en '{go.name}', no sobrescrito.");
            return;
        }

        rb.mass = rbMass;
        rb.useGravity = rbUseGravity;
        rb.isKinematic = rbIsKinematic;
        rb.constraints = rbConstraints;
        targetRigidbody = rb;
        Debug.Log($"ThirdPersonCamera: Rigidbody existente en '{go.name}' configurado/actualizado.");
    }
}