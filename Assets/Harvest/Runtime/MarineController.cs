using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController), typeof(Vitality), typeof(MarineLoadout))]
    public sealed class MarineController : MonoBehaviour
    {
        public Camera View;
        public float WalkSpeed = 5.5f;
        public float SprintSpeed = 7.4f;
        public float MouseSensitivity = 2f;
        public string CallSign { get; private set; } = "Marine";
        public Vitality Vitality { get; private set; }

        CharacterController controller;
        HarvestEncounter encounter;
        float verticalVelocity;
        float pitch;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Vitality = GetComponent<Vitality>();
            if (Vitality == null)
            {
                Debug.LogError("The Line scene needs updating. Stop Play mode and choose Harvest > Build The Line Prototype.", this);
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
            transform.position = position;
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
            pitch = 0f;
            View.transform.localRotation = Quaternion.identity;
            verticalVelocity = 0f;
            CallSign = callSign;
            Vitality.Restore();
            GetComponent<MarineLoadout>().ResetLoadout();
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
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
