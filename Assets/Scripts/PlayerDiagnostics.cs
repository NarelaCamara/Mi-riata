using System.Linq;
using UnityEngine;

// Attach this to any active GameObject (e.g. Main Camera).
// Press 'D' in Play mode to print diagnostics about 'Rata-model'.
public class PlayerDiagnostics : MonoBehaviour
{
    public string playerName = "Rata-model";
    public KeyCode diagnosticKey = KeyCode.D;

    void Update()
    {
        if (Input.GetKeyDown(diagnosticKey))
        {
            RunDiagnostics();
        }
    }

    void RunDiagnostics()
    {
        var go = GameObject.Find(playerName);
        if (go == null)
        {
            Debug.LogWarning($"PlayerDiagnostics: No se encontró GameObject con nombre '{playerName}'.");
            return;
        }

        Debug.Log($"PlayerDiagnostics: Informacion para '{playerName}' (path: {GetFullPath(go.transform)})");

        // Transform
        Debug.Log($"  Position: {go.transform.position}, Rotation: {go.transform.eulerAngles}, Scale: {go.transform.localScale}");

        // Rigidbody
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"  Rigidbody: kinematic={rb.isKinematic}, useGravity={rb.useGravity}, constraints={rb.constraints}");
        }
        else
        {
            Debug.Log("  Rigidbody: none");
        }

        // CharacterController
        var cc = go.GetComponent<CharacterController>();
        Debug.Log(cc != null ? $"  CharacterController: enabled={cc.enabled}, height={cc.height}, center={cc.center}" : "  CharacterController: none");

        // NavMeshAgent
        var nav = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        Debug.Log(nav != null ? $"  NavMeshAgent: enabled={nav.enabled}, speed={nav.speed}, isStopped={nav.isStopped}" : "  NavMeshAgent: none");

        // Animator
        var anim = go.GetComponent<Animator>();
        Debug.Log(anim != null ? $"  Animator: enabled={anim.enabled}, applyRootMotion={anim.applyRootMotion}, hasController={(anim.runtimeAnimatorController!=null)}" : "  Animator: none");

        // List MonoBehaviours
        var monos = go.GetComponents<MonoBehaviour>();
        if (monos != null && monos.Length > 0)
        {
            Debug.Log($"  MonoBehaviours ({monos.Length}):");
            foreach (var m in monos)
            {
                if (m == null) { Debug.Log("    Missing (null) component"); continue; }
                Debug.Log($"    {m.GetType().Name} - enabled={m.enabled}");
            }

            // Try to detect movement controller
            var movement = monos.FirstOrDefault(m => m.GetType().Name.ToLower().Contains("move") || m.GetType().Name.ToLower().Contains("player") || m.GetType().Name.ToLower().Contains("controller"));
            if (movement != null)
                Debug.Log($"  Posible script de movimiento detectado: {movement.GetType().Name} (enabled={movement.enabled})");
            else
                Debug.Log("  No se detectó un script de movimiento obvio (nombres con 'Move','Player','Controller').");
        }
        else
        {
            Debug.Log("  MonoBehaviours: none");
        }

        // Input snapshot
        string inputAxes = $"  Input axes: Horizontal={Input.GetAxis("Horizontal"):F2}, Vertical={Input.GetAxis("Vertical"):F2}, MouseX={Input.GetAxis("Mouse X"):F2}, MouseY={Input.GetAxis("Mouse Y"):F2}";
        Debug.Log(inputAxes);

        // Constraints check: try to move a small amount (non-destructive test)
        if (rb != null && !rb.isKinematic)
        {
            Vector3 before = go.transform.position;
            // Do not modify scene state: use Rigidbody.SweepTest? only diagnostic. We'll log constraints.
            Debug.Log($"  Rigidbody constraints: {rb.constraints}");
        }

        // Check if object is rooted by parent with frozen transform
        if (go.transform.parent != null)
        {
            Debug.Log($"  Parent: {go.transform.parent.name} (path: {GetFullPath(go.transform.parent)})");
        }

        // Check if object is below ground or inside collider by raycast up/down
        RaycastHit hit;
        if (Physics.Raycast(go.transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 5f))
        {
            Debug.Log($"  Ground hit: {hit.collider.gameObject.name} at distance {hit.distance:F2}");
        }
        else
        {
            Debug.Log("  Ground check: no se detectó colisión por debajo en 5m");
        }

        Debug.Log("PlayerDiagnostics: fin del informe.");
    }

    string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}