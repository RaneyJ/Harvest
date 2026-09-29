using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MarineController : MonoBehaviour
    {
        public Camera View;
        public int MaxHealth = 100;
        public float WalkSpeed = 5.5f;
        public float SprintSpeed = 7.4f;
        public float MouseSensitivity = 2f;
        public float WeaponRange = 90f;
        public int MagazineSize = 32;
        public int ReserveAmmo = 128;
        public float SecondsBetweenShots = 0.12f;
        public float ReloadSeconds = 1.8f;

        public int Health { get; private set; }
        public int Magazine { get; private set; }
        public bool IsAlive => Health > 0;
        public string CallSign { get; private set; } = "Marine";
        public float HitMarkerUntil { get; private set; }

        CharacterController controller;
        float verticalVelocity;
        float pitch;
        float nextShot;
        float reloadUntil;
        HarvestEncounter encounter;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            Health = MaxHealth;
            Magazine = MagazineSize;
        }

        void OnEnable() => LockCursor(true);

        void OnDisable() => LockCursor(false);

        public void ResetMarine(string callSign, Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
            CallSign = callSign;
            Health = MaxHealth;
            Magazine = MagazineSize;
            ReserveAmmo = 128;
            verticalVelocity = 0f;
            reloadUntil = 0f;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
            if (!IsAlive || (encounter != null && encounter.IsFinished)) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float yaw = Input.GetAxisRaw("Mouse X") * MouseSensitivity;
                float look = Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
                transform.Rotate(Vector3.up, yaw);
                pitch = Mathf.Clamp(pitch - look, -85f, 85f);
                View.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Vector3 move = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f));
            float speed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;
            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (Input.GetKeyDown(KeyCode.Space)) verticalVelocity = 6f;
            }
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.R)) StartReload();
            if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0)) Fire();
        }

        void StartReload()
        {
            if (reloadUntil > Time.time || Magazine == MagazineSize || ReserveAmmo == 0) return;
            reloadUntil = Time.time + ReloadSeconds;
        }

        void Fire()
        {
            if (Time.time < nextShot || Time.time < reloadUntil) return;
            if (Magazine == 0) { StartReload(); return; }
            nextShot = Time.time + SecondsBetweenShots;
            Magazine--;
            Ray ray = new Ray(View.transform.position, View.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, WeaponRange, ~0, QueryTriggerInteraction.Ignore))
            {
                CovenantEnemy enemy = hit.collider.GetComponentInParent<CovenantEnemy>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(24);
                    HitMarkerUntil = Time.time + 0.12f;
                }
                Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.15f);
            }
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || (encounter != null && encounter.IsFinished)) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health == 0) encounter?.MarineDied();
        }

        public string AmmoText => Time.time < reloadUntil ? "RELOADING" : $"{Magazine} / {ReserveAmmo}";

        public void CompleteReload()
        {
            int amount = Mathf.Min(MagazineSize - Magazine, ReserveAmmo);
            Magazine += amount;
            ReserveAmmo -= amount;
        }

        void LateUpdate()
        {
            if (reloadUntil > 0f && Time.time >= reloadUntil)
            {
                reloadUntil = 0f;
                CompleteReload();
            }
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
