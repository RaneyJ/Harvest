using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController), typeof(Vitality), typeof(MarineLoadout))]
    [RequireComponent(typeof(MarineArmor), typeof(Suppression))]
    public sealed class MarineController : MonoBehaviour
    {
        public Camera View;
        public float WalkSpeed = 5.5f;
        public float SprintSpeed = 7.4f;
        public float MouseSensitivity = 2f;
        [Min(0.8f)] public float CrouchHeight = 1.15f;
        [Min(0.1f)] public float CrouchSpeed = 2.7f;
        [Min(0.1f)] public float CrouchCameraSpeed = 12f;
        public bool IsCrouched { get; private set; }
        public string CallSign { get; private set; } = "Marine";
        public Vitality Vitality { get; private set; }

        CharacterController controller;
        WeaponRecoil recoil;
        HarvestEncounter encounter;
        float verticalVelocity;
        float pitch;
        float standingHeight;
        float standingStepOffset;
        Vector3 standingCenter;
        Vector3 standingViewPosition;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            recoil = GetComponent<WeaponRecoil>();
            standingHeight = controller.height;
            standingStepOffset = controller.stepOffset;
            standingCenter = controller.center;
            standingViewPosition = View != null ? View.transform.localPosition : new Vector3(0f, 0.65f, 0f);
            Vitality = GetComponent<Vitality>();
            if (Vitality == null || GetComponent<MarineArmor>() == null)
            {
                Debug.LogError("The Line marine needs the armor update. Stop Play mode and choose Harvest > Build The Line Prototype.", this);
                enabled = false;
                return;
            }
            encounter = FindFirstObjectByType<HarvestEncounter>();
        }

        void OnEnable() => LockCursor(true);
        void OnDisable() => LockCursor(false);

        public void TransferTo(string callSign, Vector3 position)
        {
            controller.enabled = false;
            SetCrouched(false);
            View.transform.localPosition = standingViewPosition;
            GetComponent<Suppression>().ResetPressure();
            transform.position = position;
            GetComponent<FootstepAudio>()?.ResetStride();
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
            pitch = 0f;
            View.transform.localRotation = Quaternion.identity;
            verticalVelocity = 0f;
            CallSign = callSign;
            Vitality.Restore();
            GetComponent<MarineArmor>().ResetArmor();
            GetComponent<MarineLoadout>().ResetLoadout();
            GetComponent<GrenadeInventory>()?.ResetInventory();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
            if (!Vitality.IsAlive || (encounter != null && encounter.IsFinished)) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(Vector3.up, Input.GetAxisRaw("Mouse X") * MouseSensitivity);
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * MouseSensitivity, -85f, 85f);
                float kick = recoil != null ? recoil.Pitch : 0f;
                float yaw = recoil != null ? recoil.Yaw : 0f;
                View.transform.localRotation = Quaternion.Euler(Mathf.Clamp(pitch - kick, -85f, 85f), yaw, 0f);
            }
            bool wantsCrouch = Cursor.lockState == CursorLockMode.Locked && Input.GetKey(KeyCode.LeftControl);
            if (wantsCrouch) SetCrouched(true);
            else if (IsCrouched && CanStand()) SetCrouched(false);
            Vector3 desiredEye = standingViewPosition - Vector3.up * (standingHeight - controller.height);
            View.transform.localPosition = Vector3.Lerp(View.transform.localPosition, desiredEye,
                1f - Mathf.Exp(-CrouchCameraSpeed * Time.deltaTime));
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Vector3 move = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f));
            float speed = IsCrouched ? CrouchSpeed : Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;
            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!IsCrouched && Input.GetKeyDown(KeyCode.Space)) verticalVelocity = 6f;
            }
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        void SetCrouched(bool value)
        {
            if (IsCrouched == value) return;
            IsCrouched = value;
            float height = value ? Mathf.Clamp(CrouchHeight, controller.radius * 2f, standingHeight) : standingHeight;
            controller.stepOffset = Mathf.Min(standingStepOffset, height * 0.4f);
            controller.height = height;
            // Shift the capsule center down by half the height change to keep its feet fixed.
            controller.center = standingCenter + Vector3.up * ((height - standingHeight) * 0.5f);
        }

        bool CanStand()
        {
            Vector3 foot = transform.TransformPoint(standingCenter) - Vector3.up * (standingHeight * 0.5f);
            float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth - 0.01f);
            // Check the upper volume added by standing, avoiding existing floor contacts.
            Vector3 lower = foot + Vector3.up * Mathf.Max(radius, controller.height - radius);
            Vector3 upper = foot + Vector3.up * (standingHeight - radius - 0.01f);
            Collider[] overlaps = Physics.OverlapCapsule(lower, upper, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider other in overlaps)
                if (!other.transform.IsChildOf(transform)) return false;
            return true;
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
