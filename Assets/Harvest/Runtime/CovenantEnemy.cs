using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CovenantEnemy : MonoBehaviour
    {
        public enum Kind { Grunt, Jackal, Brute }
        public Kind Type;
        public int Health;
        public int Shield;
        public float MoveSpeed;
        public float FireInterval;
        public Material BoltMaterial;

        MarineController target;
        HarvestEncounter encounter;
        CharacterController controller;
        float nextAttack;
        float verticalVelocity;
        float lastDamage;
        float shieldRechargeRemainder;
        int maxHealth;
        int maxShield;

        void Awake()
        {
            target = FindFirstObjectByType<MarineController>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            controller = GetComponent<CharacterController>();
            nextAttack = Time.time + Random.Range(0.4f, 1.4f);
        }

        void Start()
        {
            maxHealth = Health;
            maxShield = Shield;
        }

        void Update()
        {
            if (target == null || !target.IsAlive || (encounter != null && encounter.IsFinished)) return;
            if (maxShield > 0 && Time.time - lastDamage > 6f)
            {
                shieldRechargeRemainder += 18f * Time.deltaTime;
                int whole = Mathf.FloorToInt(shieldRechargeRemainder);
                Shield = Mathf.Min(maxShield, Shield + whole);
                shieldRechargeRemainder -= whole;
            }

            Vector3 toward = target.transform.position - transform.position;
            toward.y = 0f;
            float distance = toward.magnitude;
            if (distance < 0.01f) return;
            Vector3 direction = toward / distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);

            // Keep Grunts at range; Brutes close aggressively. Geometry blocks both attacks.
            float preferred = Type == Kind.Brute ? 1.9f : 12f;
            if (distance > preferred)
            {
                Vector3 horizontal = direction * MoveSpeed;
                verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
                controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
            }
            else if (!controller.isGrounded)
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
                controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            }

            if (Time.time < nextAttack) return;
            Vector3 origin = transform.position + Vector3.up * (Type == Kind.Brute ? 1.6f : 1.2f);
            Vector3 aim = target.View.transform.position - origin;
            if (Physics.Raycast(origin, aim.normalized, out RaycastHit hit, aim.magnitude + 0.2f) &&
                hit.collider.GetComponentInParent<MarineController>() == target)
            {
                if (Type == Kind.Brute)
                {
                    if (distance < 2.9f) { target.TakeDamage(42); nextAttack = Time.time + FireInterval; }
                }
                else
                {
                    FireBolt(origin, aim.normalized);
                    nextAttack = Time.time + FireInterval + Random.Range(-0.2f, 0.2f);
                }
            }
        }

        void FireBolt(Vector3 origin, Vector3 direction)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "Plasma Bolt";
            bolt.transform.position = origin + direction * 0.7f;
            bolt.transform.localScale = Vector3.one * 0.26f;
            Destroy(bolt.GetComponent<Collider>());
            bolt.GetComponent<Renderer>().sharedMaterial = BoltMaterial;
            PlasmaBolt projectile = bolt.AddComponent<PlasmaBolt>();
            projectile.Velocity = direction * 17f;
        }

        public void ApplyDamage(int amount)
        {
            lastDamage = Time.time;
            shieldRechargeRemainder = 0f;
            int shieldDamage = Mathf.Min(Shield, amount);
            Shield -= shieldDamage;
            Health -= amount - shieldDamage;
            if (Health > 0) return;
            encounter?.EnemyKilled(this);
            Destroy(gameObject);
        }

        void OnGUI()
        {
            if (target == null || Camera.main == null || Vector3.Distance(Camera.main.transform.position, transform.position) > 35f) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * (Type == Kind.Brute ? 3.3f : 2.1f));
            if (point.z <= 0f) return;
            Rect bar = new Rect(point.x - 25f, Screen.height - point.y, 50f, 4f);
            GUI.color = Color.black; GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Shield > 0 ? new Color(0.35f, 0.75f, 1f) : new Color(1f, 0.45f, 0.32f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, 50f * (Shield > 0 ? Shield / (float)maxShield : Mathf.Max(0f, Health / (float)maxHealth)), 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
