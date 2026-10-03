using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RataController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;
    public float runMultiplier = 1.8f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Configuración")]
    public bool useCameraDirection = true; // mover relativo a la cámara

    [Header("Compatibilidad Rigidbody")]
    [Tooltip("Si está activo y existe un Rigidbody en el objeto, será eliminado en Start para usar CharacterController.")]
    public bool removeRigidbodyIfExists = true;

    CharacterController cc;
    Vector3 velocity;
    Transform cam;

    void Start()
    {
        // Si existe un Rigidbody en el objeto, manejar según la configuración
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (removeRigidbodyIfExists)
            {
                Debug.Log($"RataController: Se encontró Rigidbody en '{name}' y será eliminado para usar CharacterController.");
                Destroy(rb);
            }
            else
            {
                // Si no queremos eliminarlo, dejamos en modo cinemático para evitar conflictos con CharacterController
                Debug.Log($"RataController: Se encontró Rigidbody en '{name}'. Se marcará como kinematic para evitar conflictos.");
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.4f;
        }

        if (Camera.main != null)
            cam = Camera.main.transform;
        else if (FindObjectOfType<Camera>() != null)
            cam = FindObjectOfType<Camera>().transform;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0f, v);

        if (input.sqrMagnitude > 1f) input.Normalize();

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) speed *= runMultiplier;

        Vector3 move = Vector3.zero;
        if (useCameraDirection && cam != null)
        {
            Vector3 forward = cam.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cam.right;
            right.y = 0f;
            right.Normalize();
            move = forward * input.z + right * input.x;

            // Orientar al personaje hacia la dirección de movimiento
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }
        }
        else
        {
            move = transform.TransformDirection(input);
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }
        }

        cc.Move(move * speed * Time.deltaTime);

        // Gravity & Jump
        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small downward to keep grounded

        if (Input.GetButtonDown("Jump") && cc.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
