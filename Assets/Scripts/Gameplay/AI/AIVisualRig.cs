using UnityEngine;
using UnityEngine.Rendering;

namespace VelocityRush.Gameplay.AI
{
    /// <summary>
    /// Kinematic visual shell for an AI opponent rig (T-10.2.1/T-10.2.3, D-059).
    /// </summary>
    /// <remarks>
    /// <para>
    /// An AI opponent is a path-following actor, not a physics body. This component removes the
    /// physics affordances inherited from <c>VehicleRigBase</c> — it disables every
    /// <see cref="WheelCollider"/>, kinematizes the <see cref="Rigidbody"/>, and turns colliders
    /// into query-only triggers so the player is never solved against an AI body — then drives the
    /// visual wheel spin, steering, body roll, and body pitch from the path speed and curvature.
    /// </para>
    /// <para>
    /// The approved draw-call budget requires AI cars to use a blob/contact shadow rather than
    /// realtime shadow casting (D-060/T-10.0.3): six realtime casters would add a second pass and
    /// breach the 500-draw-call ceiling. <see cref="Bind"/> therefore forces
    /// <see cref="ShadowCastingMode.Off"/> on every child renderer and creates exactly one
    /// unlit, ground-aligned blob mesh.
    /// </para>
    /// <para>
    /// All runtime work is struct math over cached references. No LINQ, no string formatting, and
    /// no managed allocation occurs in <see cref="Apply"/>, so the AI step keeps the 0 B/frame
    /// contract (D-060).
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AIVisualRig : MonoBehaviour
    {
        private const string FrontLeftName = "Wheel FL Visual";
        private const string FrontRightName = "Wheel FR Visual";
        private const string RearLeftName = "Wheel RL Visual";
        private const string RearRightName = "Wheel RR Visual";
        private const string ChassisBodyName = "ChassisBody";

        [Header("Visual bindings (resolved by name when empty)")]
        [SerializeField] private Transform frontLeftVisual;
        [SerializeField] private Transform frontRightVisual;
        [SerializeField] private Transform rearLeftVisual;
        [SerializeField] private Transform rearRightVisual;
        [SerializeField] private Transform chassisBody;

        [Header("Visual response")]
        [SerializeField, Min(0.01f)] private float wheelRadius = 0.35f;
        [SerializeField, Range(0f, 90f)] private float maxSteeringAngleDegrees = 30f;
        [SerializeField, Min(0f)] private float rollDegreesPerG = 6f;
        [SerializeField, Range(0f, 15f)] private float maxRollDegrees = 5f;
        [SerializeField, Min(0f)] private float pitchDegreesPerG = 4f;
        [SerializeField, Range(0f, 15f)] private float maxPitchDegrees = 4f;
        [SerializeField, Min(0.1f)] private float attitudeSmoothing = 8f;

        [Header("Blob shadow (realtime casters are disabled)")]
        [SerializeField] private Vector2 blobSize = new Vector2(2.2f, 3.6f);
        [SerializeField, Range(0f, 1f)] private float blobOpacity = 0.42f;
        [SerializeField, Min(0f)] private float blobGroundOffset = 0.02f;

        private readonly Transform[] _wheelVisuals = new Transform[4];
        private readonly Quaternion[] _wheelBaseRotations = new Quaternion[4];

        private bool _bound;
        private float _wheelSpinDegrees;
        private float _rollDegrees;
        private float _pitchDegrees;
        private Transform _blobShadow;
        private Mesh _blobMesh;
        private Texture2D _blobTexture;
        private Material _blobMaterial;

        /// <summary>Wheel radius used to convert linear speed into spin.</summary>
        public float WheelRadius => wheelRadius;

        /// <summary>Whether the rig has been bound and its physics affordances removed.</summary>
        public bool IsBound => _bound;

        /// <summary>The procedural blob shadow transform, or null when it could not be created.</summary>
        public Transform BlobShadow => _blobShadow;

        /// <summary>Current visual wheel spin in degrees (test/telemetry helper).</summary>
        public float WheelSpinDegrees => _wheelSpinDegrees;

        private void Awake()
        {
            Bind();
        }

        /// <summary>
        /// Resolves the visual transforms, removes physics affordances, and creates the blob shadow.
        /// Idempotent; safe to call from EditMode tests before <c>Awake</c> runs.
        /// </summary>
        public void Bind()
        {
            if (_bound)
            {
                return;
            }

            _wheelVisuals[0] = frontLeftVisual != null ? frontLeftVisual : FindChild(FrontLeftName);
            _wheelVisuals[1] = frontRightVisual != null ? frontRightVisual : FindChild(FrontRightName);
            _wheelVisuals[2] = rearLeftVisual != null ? rearLeftVisual : FindChild(RearLeftName);
            _wheelVisuals[3] = rearRightVisual != null ? rearRightVisual : FindChild(RearRightName);
            if (chassisBody == null)
            {
                chassisBody = FindChild(ChassisBodyName);
            }

            for (var i = 0; i < _wheelVisuals.Length; i++)
            {
                var visual = _wheelVisuals[i];
                _wheelBaseRotations[i] = visual != null ? visual.localRotation : Quaternion.identity;
            }

            DisablePhysicsAffordances();
            DisableRealtimeShadowCasting();
            CreateBlobShadow();
            _bound = true;
        }

        /// <summary>Overrides the wheel radius used for spin and blob scaling.</summary>
        public void ConfigureWheelRadius(float radius)
        {
            if (radius > 0.01f)
            {
                wheelRadius = radius;
            }
        }

        /// <summary>
        /// Applies one visual step. Never allocates.
        /// </summary>
        /// <param name="deltaTime">Step in seconds.</param>
        /// <param name="speed">Forward speed in m/s.</param>
        /// <param name="steering">Normalized steering command in -1..1 (positive = right).</param>
        /// <param name="lateralAcceleration">Signed lateral acceleration in m/s² (right turn positive).</param>
        /// <param name="longitudinalAcceleration">Signed longitudinal acceleration in m/s².</param>
        /// <param name="groundY">Ground height under the car in world units.</param>
        public void Apply(
            float deltaTime,
            float speed,
            float steering,
            float lateralAcceleration,
            float longitudinalAcceleration,
            float groundY)
        {
            if (!_bound)
            {
                return;
            }

            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            var steerDegrees = Mathf.Clamp(steering, -1f, 1f) * maxSteeringAngleDegrees;
            var spinDelta = wheelRadius > 0.01f
                ? (speed / wheelRadius) * Mathf.Rad2Deg * deltaTime
                : 0f;
            _wheelSpinDegrees = Mathf.Repeat(_wheelSpinDegrees + spinDelta, 360f);

            var steerRotation = Quaternion.Euler(0f, steerDegrees, 0f);
            var spinRotation = Quaternion.Euler(_wheelSpinDegrees, 0f, 0f);

            SetWheelRotation(0, steerRotation, spinRotation);
            SetWheelRotation(1, steerRotation, spinRotation);
            SetWheelRotation(2, Quaternion.identity, spinRotation);
            SetWheelRotation(3, Quaternion.identity, spinRotation);

            var targetRoll = Mathf.Clamp(
                lateralAcceleration * rollDegreesPerG / 9.81f,
                -maxRollDegrees,
                maxRollDegrees);
            var targetPitch = Mathf.Clamp(
                -longitudinalAcceleration * pitchDegreesPerG / 9.81f,
                -maxPitchDegrees,
                maxPitchDegrees);

            var smoothing = attitudeSmoothing * deltaTime;
            _rollDegrees = Mathf.MoveTowards(_rollDegrees, targetRoll, smoothing);
            _pitchDegrees = Mathf.MoveTowards(_pitchDegrees, targetPitch, smoothing);

            if (chassisBody != null)
            {
                chassisBody.localRotation = Quaternion.Euler(_pitchDegrees, 0f, _rollDegrees);
            }

            if (_blobShadow != null)
            {
                _blobShadow.SetPositionAndRotation(
                    new Vector3(transform.position.x, groundY + blobGroundOffset, transform.position.z),
                    Quaternion.identity);
            }
        }

        /// <summary>Resets accumulated visual state (used on grid reset in later stages).</summary>
        public void ResetVisualState()
        {
            _wheelSpinDegrees = 0f;
            _rollDegrees = 0f;
            _pitchDegrees = 0f;
            if (chassisBody != null)
            {
                chassisBody.localRotation = Quaternion.identity;
            }

            for (var i = 0; i < _wheelVisuals.Length; i++)
            {
                if (_wheelVisuals[i] != null)
                {
                    _wheelVisuals[i].localRotation = _wheelBaseRotations[i];
                }
            }
        }

        private void SetWheelRotation(int index, Quaternion steer, Quaternion spin)
        {
            var visual = _wheelVisuals[index];
            if (visual == null)
            {
                return;
            }

            visual.localRotation = _wheelBaseRotations[index] * steer * spin;
        }

        private Transform FindChild(string childName)
        {
            var direct = transform.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            var all = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName)
                {
                    return all[i];
                }
            }

            return null;
        }

        private void DisablePhysicsAffordances()
        {
            // The rig is instantiated from VehicleRigBase, which carries the player controller.
            // An AI is not a WheelCollider vehicle (D-059), so the inherited controller is
            // disabled rather than allowed to run its fixed step.
            var controllers = GetComponentsInChildren<VehicleController>(true);
            for (var i = 0; i < controllers.Length; i++)
            {
                controllers[i].enabled = false;
            }

            var wheels = GetComponentsInChildren<WheelCollider>(true);
            for (var i = 0; i < wheels.Length; i++)
            {
                wheels[i].enabled = false;
            }

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            // Query-only triggers: the soft-push check finds them, but the physics solver never
            // resolves a contact against an AI car (D-061).
            var colliders = GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider is WheelCollider)
                {
                    continue;
                }

                collider.isTrigger = true;
            }
        }

        private void DisableRealtimeShadowCasting()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private void CreateBlobShadow()
        {
            if (_blobShadow != null)
            {
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader == null)
            {
                // Shadow casting is already off; a missing shader must not break the AI.
                return;
            }

            _blobTexture = CreateRadialTexture();
            _blobMaterial = new Material(shader)
            {
                name = "AI Blob Shadow (runtime)",
                mainTexture = _blobTexture,
                color = new Color(0f, 0f, 0f, blobOpacity)
            };
            _blobMesh = CreateQuadMesh();

            var blob = new GameObject("AI Blob Shadow");
            _blobShadow = blob.transform;
            _blobShadow.SetParent(transform, false);
            _blobShadow.localScale = new Vector3(blobSize.x, 1f, blobSize.y);

            var filter = blob.AddComponent<MeshFilter>();
            filter.sharedMesh = _blobMesh;

            var renderer = blob.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _blobMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Texture2D CreateRadialTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AI Blob Shadow (runtime)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01(1f - (distance * distance));
                    pixels[(y * size) + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "AI Blob Shadow Quad (runtime)" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            DestroyRuntimeAsset(_blobMaterial);
            DestroyRuntimeAsset(_blobTexture);
            DestroyRuntimeAsset(_blobMesh);
            _blobMaterial = null;
            _blobTexture = null;
            _blobMesh = null;
        }

        private static void DestroyRuntimeAsset(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(asset);
            }
            else
            {
                DestroyImmediate(asset);
            }
        }
    }
}
