using UnityEngine;
using UnityEngine.AI;

namespace Harvest
{
    // Independent of input and animation: footsteps follow actual grounded travel.
    public sealed class FootstepAudio : MonoBehaviour
    {
        public AudioClip[] Gravel = new AudioClip[0];
        public AudioClip[] Wood = new AudioClip[0];
        public FootstepSurfaceKind DefaultSurface = FootstepSurfaceKind.Gravel;
        [Min(0.2f)] public float WalkStride = 1.8f;
        [Min(0.2f)] public float SprintStride = 2.35f;
        [Min(0.2f)] public float CrouchStride = 1.6f;
        [Range(0f, 1f)] public float Volume = 0.18f;
        [HideInInspector] public int AudioMixRevision;
        CharacterController controller;
        MarineController player;
        Vitality vitality;
        NavMeshAgent agent;
        CapsuleCollider capsule;
        AudioSource source;
        Vector3 previous;
        float travelled;
        int lastGravel = -1, lastWood = -1;
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            player = GetComponent<MarineController>();
            vitality = GetComponent<Vitality>();
            agent = GetComponent<NavMeshAgent>();
            capsule = GetComponent<CapsuleCollider>();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false;
            source.spatialBlend = player != null ? 0f : 1f;
            source.dopplerLevel = 0f; source.minDistance = 2f; source.maxDistance = 28f;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.priority = 160;
        }
        void OnEnable() => ResetStride();
        void OnDisable() { if (source != null) source.Stop(); }
        public void ResetStride() { previous = transform.position; travelled = 0f; }
        void LateUpdate()
        {
            Vector3 delta = transform.position - previous; previous = transform.position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if ((vitality != null && !vitality.IsAlive) || distance < 0.001f || distance > 3f ||
                (controller != null && (!controller.enabled || !controller.isGrounded)) ||
                (agent != null && (!agent.enabled || !agent.isOnNavMesh)))
            { travelled = 0f; return; }
            Vector3 foot = controller != null ? transform.TransformPoint(controller.center) - Vector3.up * controller.height * 0.5f :
                capsule != null ? new Vector3(transform.position.x, capsule.bounds.min.y, transform.position.z) : transform.position;
            RaycastHit[] hits = Physics.RaycastAll(foot + Vector3.up * 0.3f, Vector3.down, 0.65f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Collider floor = null;
            foreach (RaycastHit hit in hits)
                if (!hit.collider.transform.IsChildOf(transform) && hit.normal.y > 0.4f) { floor = hit.collider; break; }
            if (floor == null) { travelled = 0f; return; }
            bool crouched = player != null && player.IsCrouched;
            float speed = distance / Mathf.Max(0.001f, Time.deltaTime);
            bool sprinting = player != null && speed > player.WalkSpeed + 0.5f;
            float stride = Mathf.Max(0.2f, crouched ? CrouchStride : sprinting ? SprintStride : WalkStride);
            travelled += distance;
            if (travelled < stride) return;
            travelled %= stride; // At most one step per frame, never a catch-up audio burst.
            FootstepSurface surface = floor.GetComponentInParent<FootstepSurface>();
            FootstepSurfaceKind kind = surface != null ? surface.Kind : DefaultSurface;
            AudioClip[] clips = kind == FootstepSurfaceKind.Wood ? Wood : Gravel;
            if (clips == null || clips.Length == 0) return;
            int last = kind == FootstepSurfaceKind.Wood ? lastWood : lastGravel;
            int next = Random.Range(0, clips.Length);
            if (clips.Length > 1 && next == last) next = (next + Random.Range(1, clips.Length)) % clips.Length;
            if (kind == FootstepSurfaceKind.Wood) lastWood = next; else lastGravel = next;
            if (clips[next] == null) return;
            source.pitch = Random.Range(0.96f, 1.04f);
            source.clip = clips[next]; source.volume = Volume * (crouched ? 0.45f : sprinting ? 1.15f : 1f);
            source.Play();
        }
    }
}
