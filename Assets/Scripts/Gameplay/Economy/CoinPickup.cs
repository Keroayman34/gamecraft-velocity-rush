using UnityEngine;

namespace VelocityRush.Gameplay.Economy
{
    /// <summary>
    /// Pooled collectible coin visual instance positioned along the highway track (Phase 12 Stage 2; TDD §59).
    /// Purely visual: proximity collection is computed mathematically by <see cref="RoadCoinManager"/>
    /// without Physics Collider or Rigidbody broadphase overhead.
    /// </summary>
    public sealed class CoinPickup : MonoBehaviour
    {
        private static Mesh s_SharedCoinMesh;
        private static Material s_SharedCoinMaterial;

        [SerializeField] private bool _isActive;
        [SerializeField] private int _laneIndex;
        [SerializeField] private float _trackDistance;

        public bool IsActive => _isActive;
        public int LaneIndex => _laneIndex;
        public float TrackDistance => _trackDistance;
        public Vector3 WorldPosition => transform.position;

        public void Activate(int laneIndex, float trackDistance, Vector3 worldPos)
        {
            _laneIndex = laneIndex;
            _trackDistance = trackDistance;
            _isActive = true;

            transform.position = worldPos;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        public void TickSpin(float deltaSeconds, float spinSpeedDegreesPerSec)
        {
            if (!_isActive) return;
            transform.Rotate(Vector3.up, spinSpeedDegreesPerSec * deltaSeconds, Space.World);
        }

        /// <summary>
        /// Ensures a shared procedural golden disc mesh and material are applied if no custom prefab exists.
        /// Guaranteed 0 duplicate materials across all pooled coin instances.
        /// </summary>
        public void EnsureProceduralVisuals()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (s_SharedCoinMesh == null)
            {
                s_SharedCoinMesh = CreateProceduralDiscMesh(0.55f, 0.12f, 16);
            }

            if (s_SharedCoinMaterial == null)
            {
                var shader = Shader.Find("Mobile/Diffuse") ?? Shader.Find("Diffuse") ?? Shader.Find("Standard");
                s_SharedCoinMaterial = new Material(shader)
                {
                    color = new Color(1.0f, 0.85f, 0.15f, 1f)
                };
            }

            meshFilter.sharedMesh = s_SharedCoinMesh;
            meshRenderer.sharedMaterial = s_SharedCoinMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static Mesh CreateProceduralDiscMesh(float radius, float thickness, int segments)
        {
            var mesh = new Mesh { name = "ProceduralCoinDisc" };
            int vertCount = (segments + 1) * 2 + segments * 4;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            int triCount = segments * 12;
            var triangles = new int[triCount];

            float halfThick = thickness * 0.5f;
            int v = 0;
            int t = 0;

            // Front face center + perimeter
            int frontCenter = v;
            vertices[v] = new Vector3(0f, 0f, halfThick);
            normals[v++] = Vector3.forward;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[v] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, halfThick);
                normals[v++] = Vector3.forward;
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[t++] = frontCenter;
                triangles[t++] = frontCenter + 1 + i;
                triangles[t++] = frontCenter + 1 + next;
            }

            // Back face center + perimeter
            int backCenter = v;
            vertices[v] = new Vector3(0f, 0f, -halfThick);
            normals[v++] = Vector3.back;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[v] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -halfThick);
                normals[v++] = Vector3.back;
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[t++] = backCenter;
                triangles[t++] = backCenter + 1 + next;
                triangles[t++] = backCenter + 1 + i;
            }

            // Side cylinder quad per segment
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                float angleA = i * Mathf.PI * 2f / segments;
                float angleB = next * Mathf.PI * 2f / segments;

                Vector3 dirA = new Vector3(Mathf.Cos(angleA), Mathf.Sin(angleA), 0f);
                Vector3 dirB = new Vector3(Mathf.Cos(angleB), Mathf.Sin(angleB), 0f);

                int v0 = v++;
                int v1 = v++;
                int v2 = v++;
                int v3 = v++;

                vertices[v0] = dirA * radius + Vector3.forward * halfThick;
                vertices[v1] = dirB * radius + Vector3.forward * halfThick;
                vertices[v2] = dirA * radius - Vector3.forward * halfThick;
                vertices[v3] = dirB * radius - Vector3.forward * halfThick;

                normals[v0] = dirA;
                normals[v1] = dirB;
                normals[v2] = dirA;
                normals[v3] = dirB;

                triangles[t++] = v0;
                triangles[t++] = v1;
                triangles[t++] = v2;

                triangles[t++] = v1;
                triangles[t++] = v3;
                triangles[t++] = v2;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            return mesh;
        }
    }
}
