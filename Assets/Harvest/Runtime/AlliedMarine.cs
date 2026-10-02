using UnityEngine;
using UnityEngine.AI;

namespace Harvest
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Vitality), typeof(MarineArmor))]
    [RequireComponent(typeof(ActorWeapon), typeof(CombatTarget), typeof(CapsuleCollider))]
    [RequireComponent(typeof(Suppression))]
    public sealed class AlliedMarine : MonoBehaviour
    {
        public string CallSign = "Marine";
        [Min(1f)] public float DetectionRange = 75f;
        [Min(1f)] public float CoverSearchRadius = 30f;
        [Range(0f, 15f)] public float AimErrorDegrees = 3f;
        [Min(0.1f)] public float BurstRestSeconds = 1.5f;
        [Min(1)] public int ShotsPerBurst = 4;
        public Transform BodyVisual;
        public Transform GunVisual;

        enum Phase { SeekingCover, Hidden, MovingToPeek, Firing, Returning }
        Phase phase;
        CombatTarget identity;
        CombatTarget target;
        CoverPoint cover;
        NavMeshAgent agent;
        CapsuleCollider body;
        Vitality vitality;
        MarineArmor armor;
        ActorWeapon weapon;
        MeleeAttack melee;
        HarvestEncounter encounter;
        float nextThink;
        float phaseUntil;
        float nextBurst;
        float suppressedUntil;
        float nextShot;
        int shots;
        bool crouched;

        void Awake()
        {
            identity = GetComponent<CombatTarget>();
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<CapsuleCollider>();
            vitality = GetComponent<Vitality>();
            armor = GetComponent<MarineArmor>();
            weapon = GetComponent<ActorWeapon>();
            melee = GetComponent<MeleeAttack>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            agent.updateRotation = false;
        }

        void OnEnable()
        {
            GetComponent<Vitality>().Died += Die;
            GetComponent<MarineArmor>().Hit += TakeCover;
        }
        void OnDisable()
        {
            if (vitality != null) vitality.Died -= Die;
            if (armor != null) armor.Hit -= TakeCover;
            ReleaseCover();
        }

        void Start()
        {
            if (NavMesh.GetSettingsCount() == 0 ||
                !NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                Debug.LogError("Marine needs a navigation surface. Rebuild The Line scene.", this);
                enabled = false;
                return;
            }
            transform.position = hit.position;
            agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            agent.enabled = true;
            if (!agent.isOnNavMesh)
            {
                Debug.LogError("Marine could not reach navigation surface. Rebuild The Line scene.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!vitality.IsAlive || !agent.isOnNavMesh) return;
            if (cover != null && !agent.pathPending && !agent.isStopped &&
                agent.pathStatus != NavMeshPathStatus.PathComplete) ReleaseCover();
            if (encounter != null && encounter.IsFinished) { agent.isStopped = true; SetCrouched(true); return; }
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.6f;
                target = CombatTarget.FindOpponent(CombatTeam.Marine, identity.AimPosition, DetectionRange);
                if (cover != null && !cover.ProtectsFrom(target)) ReleaseCover();
                if (cover == null) FindCover();
            }
            if (target != null && !target.IsAlive) target = null;
            if (target != null)
            {
                Vector3 direction = target.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 7f);
            }
            WeaponInstance gun = weapon.Equipped;
            if (gun == null) { SetCrouched(true); return; }
            if (!gun.Definition.UsesEnergy && gun.Magazine == 0) gun.BeginReload();
            bool empty = gun.Definition.UsesEnergy ? gun.Energy < gun.Definition.EnergyPerShot :
                gun.Magazine == 0 && gun.Reserve == 0;
            bool mustHide = empty || target == null || (identity.Pressure != null && identity.Pressure.EffectStrength >= 0.55f) || Time.time < suppressedUntil || gun.ReloadUntil > Time.time;
            if (cover == null)
            {
                agent.isStopped = true;
                SetCrouched(mustHide || Time.time < nextBurst);
                if (!mustHide && Time.time >= nextBurst) FireBurst();
                return;
            }
            if (mustHide && phase != Phase.SeekingCover && phase != Phase.Returning && phase != Phase.Hidden)
                ReturnToCover();
            switch (phase)
            {
                case Phase.SeekingCover:
                case Phase.Returning:
                    SetCrouched(false);
                    if (Arrived())
                    {
                        agent.isStopped = true;
                        phase = Phase.Hidden;
                        phaseUntil = Time.time + Random.Range(0.8f, 1.6f);
                        SetCrouched(true);
                    }
                    break;
                case Phase.Hidden:
                    SetCrouched(true);
                    if (!mustHide && Time.time >= Mathf.Max(phaseUntil, nextBurst))
                    {
                        SetCrouched(false);
                        if (MoveTo(cover.PeekPosition)) phase = Phase.MovingToPeek;
                        else ReleaseCover();
                    }
                    break;
                case Phase.MovingToPeek:
                    if (Arrived())
                    {
                        agent.isStopped = true;
                        phase = Phase.Firing;
                        phaseUntil = Time.time + 1.2f;
                        shots = 0;
                    }
                    break;
                case Phase.Firing:
                    if (Time.time >= phaseUntil || Time.time < nextBurst) ReturnToCover();
                    else FireBurst();
                    break;
            }
        }

        void FindCover()
        {
            CoverPoint best = null;
            float score = float.MaxValue;
            foreach (CoverPoint point in CoverPoint.Active)
            {
                if (point == null || !point.IsAvailableFor(this) || !point.ProtectsFrom(target)) continue;
                float distance = (point.transform.position - transform.position).sqrMagnitude;
                if (distance > CoverSearchRadius * CoverSearchRadius || distance >= score || !point.HasCompletePath(transform.position)) continue;
                best = point;
                score = distance;
            }
            if (best != null && best.TryClaim(this))
            {
                cover = best;
                phase = Phase.SeekingCover;
                if (!MoveTo(cover.transform.position)) ReleaseCover();
            }
        }

        bool MoveTo(Vector3 destination)
        {
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 1f, NavMesh.AllAreas)) return false;
            agent.isStopped = false;
            return agent.SetDestination(hit.position);
        }
        bool Arrived() => !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete &&
            Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + 0.15f;

        void FireBurst()
        {
            if (target == null || Time.time < nextShot || !target.CanSeeFrom(identity.AimPosition)) return;
            if (melee != null && Vector3.Distance(identity.AimPosition,
                CombatGeometry.ClosestBodyPoint(target.GetComponent<Collider>(), identity.AimPosition, target.AimPosition)) <= melee.Reach &&
                melee.TrySwing(identity.AimPosition, (target.AimPosition - identity.AimPosition).normalized, out bool meleeHit))
            {
                nextShot = Time.time + melee.Cooldown;
                return;
            }
            nextShot = Time.time + 0.18f;
            int before = weapon.Equipped.Magazine;
            int energyBefore = weapon.Equipped.Energy;
            weapon.TryFire(identity.AimPosition, (target.AimPosition - identity.AimPosition).normalized, AimErrorDegrees);
            if (weapon.Equipped.Magazine == before && weapon.Equipped.Energy == energyBefore) return;
            shots++;
            if (shots >= ShotsPerBurst)
            {
                shots = 0;
                nextBurst = Time.time + BurstRestSeconds + Random.Range(0f, 0.6f);
            }
        }

        void TakeCover(bool healthExposed)
        {
            if (!vitality.IsAlive) return;
            suppressedUntil = Time.time + (healthExposed ? 2f : 0.9f);
            if (cover != null && agent.isOnNavMesh) ReturnToCover();
        }
        void ReturnToCover()
        {
            phase = Phase.Returning;
            if (cover != null && !MoveTo(cover.transform.position)) ReleaseCover();
        }
        void ReleaseCover()
        {
            if (cover != null) cover.Release(this);
            cover = null;
            shots = 0;
        }
        void SetCrouched(bool value)
        {
            if (crouched == value) return;
            crouched = value;
            body.height = value ? 1.05f : 1.9f;
            body.center = Vector3.up * (body.height * 0.5f);
            identity.AimOffset = Vector3.up * (value ? 0.8f : 1.45f);
            if (BodyVisual != null)
            {
                BodyVisual.localPosition = Vector3.up * (body.height * 0.5f);
                BodyVisual.localScale = new Vector3(0.7f, body.height * 0.5f, 0.7f);
            }
            if (GunVisual != null) GunVisual.localPosition = new Vector3(0.38f, value ? 0.7f : 1.25f, 0.45f);
        }
        void Die()
        {
            ReleaseCover();
            weapon.Drop();
            Destroy(gameObject);
        }

        void OnGUI()
        {
            if (!vitality.IsAlive || Camera.main == null || Vector3.Distance(Camera.main.transform.position, transform.position) > 40f) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (point.z <= 0f) return;
            GUI.color = new Color(0.45f, 0.9f, 0.65f);
            GUI.Label(new Rect(point.x - 65f, Screen.height - point.y, 180f, 24f), CallSign);
            GUI.color = Color.white;
        }
    }
}
