using UnityEngine;

namespace ARSurvivalGame.Procedural
{
    /// <summary>
    /// Genera modelos 3D estilizados, materiales URP con emisión y efectos proceduralmente por código.
    /// Permite ser reemplazado fácilmente mediante prefabs personalizados en el Inspector.
    /// </summary>
    public static class ProceduralModelBuilder
    {
        private static Material playerBodyMat;
        private static Material playerGlowMat;
        private static Material enemyBodyMat;
        private static Material enemyGlowMat;
        private static Material bulletMat;
        private static Material reticleMat;
        private static Material arenaRingMat;

        public static Shader GetSafeShader(bool lit = true, bool transparent = false)
        {
            Shader s = null;
            if (lit)
            {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
            }
            else
            {
                s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Unlit/Color");
            }

            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        private static Material CreateMaterial(Color baseColor, Color emissiveColor, float metallic = 0.5f, float smoothness = 0.7f, bool transparent = false)
        {
            Shader shader = GetSafeShader(lit: true);
            Material mat = new Material(shader);
            mat.color = baseColor;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            if (emissiveColor != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissiveColor);
            }

            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent in URP
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return mat;
        }

        public static Material GetPlayerBodyMaterial()
        {
            if (playerBodyMat == null)
                playerBodyMat = CreateMaterial(new Color(0.12f, 0.15f, 0.22f), Color.black, 0.7f, 0.8f);
            return playerBodyMat;
        }

        public static Material GetPlayerGlowMaterial()
        {
            if (playerGlowMat == null)
                playerGlowMat = CreateMaterial(new Color(0f, 0.8f, 1f), new Color(0f, 0.9f, 1f) * 2.5f, 0.1f, 0.9f);
            return playerGlowMat;
        }

        public static Material GetEnemyBodyMaterial()
        {
            if (enemyBodyMat == null)
                enemyBodyMat = CreateMaterial(new Color(0.18f, 0.08f, 0.08f), Color.black, 0.6f, 0.6f);
            return enemyBodyMat;
        }

        public static Material GetEnemyGlowMaterial()
        {
            if (enemyGlowMat == null)
                enemyGlowMat = CreateMaterial(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.1f, 0.05f) * 2.8f, 0.1f, 0.9f);
            return enemyGlowMat;
        }

        public static Material GetBulletMaterial()
        {
            if (bulletMat == null)
                bulletMat = CreateMaterial(new Color(0.2f, 0.9f, 1f), new Color(0.3f, 1f, 1f) * 3f, 0.1f, 0.95f);
            return bulletMat;
        }

        public static Material GetReticleMaterial()
        {
            if (reticleMat == null)
                reticleMat = CreateMaterial(new Color(0f, 0.85f, 1f, 0.6f), new Color(0f, 0.7f, 1f) * 1.5f, 0f, 0f, transparent: true);
            return reticleMat;
        }

        public static Material GetArenaRingMaterial()
        {
            if (arenaRingMat == null)
                arenaRingMat = CreateMaterial(new Color(0f, 0.6f, 1f, 0.35f), new Color(0f, 0.4f, 0.9f) * 0.8f, 0f, 0f, transparent: true);
            return arenaRingMat;
        }

        /// <summary>
        /// Crea el modelo 3D del jugador (Droide de combate sci-fi).
        /// </summary>
        public static GameObject CreatePlayerModel(Transform parent)
        {
            GameObject root = new GameObject("ProceduralPlayerModel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            Material bodyMat = GetPlayerBodyMaterial();
            Material glowMat = GetPlayerGlowMaterial();

            // Cuerpo central (Esfera)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            body.transform.localScale = new Vector3(0.25f, 0.22f, 0.25f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Visor emisivo cian
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visor.name = "Visor";
            visor.transform.SetParent(root.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.16f, 0.09f);
            visor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visor.transform.localScale = new Vector3(0.16f, 0.05f, 0.08f);
            visor.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            // Cañón izquierdo
            GameObject cannonL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cannonL.name = "Cannon_L";
            cannonL.transform.SetParent(root.transform, false);
            cannonL.transform.localPosition = new Vector3(-0.16f, 0.13f, 0.06f);
            cannonL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cannonL.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            cannonL.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(cannonL.GetComponent<Collider>());

            // Punta del cañón izquierdo emisiva
            GameObject tipL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipL.transform.SetParent(cannonL.transform, false);
            tipL.transform.localPosition = new Vector3(0f, 1f, 0f);
            tipL.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
            tipL.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(tipL.GetComponent<Collider>());

            // Cañón derecho
            GameObject cannonR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cannonR.name = "Cannon_R";
            cannonR.transform.SetParent(root.transform, false);
            cannonR.transform.localPosition = new Vector3(0.16f, 0.13f, 0.06f);
            cannonR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cannonR.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            cannonR.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(cannonR.GetComponent<Collider>());

            // Punta del cañón derecho emisiva
            GameObject tipR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipR.transform.SetParent(cannonR.transform, false);
            tipR.transform.localPosition = new Vector3(0f, 1f, 0f);
            tipR.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
            tipR.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(tipR.GetComponent<Collider>());

            // Propulsor / Anillo inferior
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ThrusterRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localScale = new Vector3(0.18f, 0.03f, 0.18f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            return root;
        }

        /// <summary>
        /// Crea el modelo 3D del enemigo (Dron acechador agresivo con núcleo rojo).
        /// </summary>
        public static GameObject CreateEnemyModel(Transform parent)
        {
            GameObject root = new GameObject("ProceduralEnemyModel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            Material bodyMat = GetEnemyBodyMaterial();
            Material glowMat = GetEnemyGlowMaterial();

            // Núcleo central amenazante (Cubo rotado a diamante)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            core.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            core.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(core.GetComponent<Collider>());

            // Blindaje exterior envolvente
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "ArmorShell";
            shell.transform.SetParent(root.transform, false);
            shell.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            shell.transform.localScale = new Vector3(0.24f, 0.18f, 0.24f);
            shell.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(shell.GetComponent<Collider>());

            // Púa frontal / aguijón
            GameObject spikeF = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spikeF.name = "Spike_F";
            spikeF.transform.SetParent(root.transform, false);
            spikeF.transform.localPosition = new Vector3(0f, 0.16f, 0.16f);
            spikeF.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spikeF.transform.localScale = new Vector3(0.04f, 0.1f, 0.04f);
            spikeF.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(spikeF.GetComponent<Collider>());

            // Aletas laterales
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = $"Fin_{i}";
                fin.transform.SetParent(root.transform, false);
                fin.transform.localPosition = Quaternion.Euler(0, angle, 0) * new Vector3(0.15f, 0.16f, 0f);
                fin.transform.localRotation = Quaternion.Euler(0, angle, 25f);
                fin.transform.localScale = new Vector3(0.03f, 0.12f, 0.08f);
                fin.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
                Object.DestroyImmediate(fin.GetComponent<Collider>());
            }

            return root;
        }

        /// <summary>
        /// Crea el proyectil de plasma con estela (TrailRenderer).
        /// </summary>
        public static GameObject CreateBulletObject()
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "PlasmaBullet";
            bullet.transform.localScale = new Vector3(0.08f, 0.08f, 0.16f);
            bullet.GetComponent<MeshRenderer>().sharedMaterial = GetBulletMaterial();

            // Collider trigger para detectar impacto
            SphereCollider col = bullet.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // Rigidbody kinematic para física y eventos de colisión
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Trail Renderer para estela luminosa
            TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.06f;
            trail.endWidth = 0.0f;
            trail.material = GetBulletMaterial();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return bullet;
        }

        /// <summary>
        /// Crea el retículo holográfico para posicionamiento AR.
        /// </summary>
        public static GameObject CreateReticleObject()
        {
            GameObject reticle = new GameObject("ARPlacementReticle");

            // Anillo exterior plano
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ReticleRing";
            ring.transform.SetParent(reticle.transform, false);
            ring.transform.localScale = new Vector3(0.4f, 0.005f, 0.4f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = GetReticleMaterial();
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Punto central
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "ReticleCenter";
            center.transform.SetParent(reticle.transform, false);
            center.transform.localScale = new Vector3(0.08f, 0.008f, 0.08f);
            center.GetComponent<MeshRenderer>().sharedMaterial = GetPlayerGlowMaterial();
            Object.DestroyImmediate(center.GetComponent<Collider>());

            return reticle;
        }

        /// <summary>
        /// Crea un anillo delimitador de la arena de juego sobre el plano AR.
        /// </summary>
        public static GameObject CreateArenaBoundaryObject(float radius)
        {
            GameObject arena = new GameObject("ArenaBoundary");
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "BoundaryRing";
            ring.transform.SetParent(arena.transform, false);
            ring.transform.localScale = new Vector3(radius * 2f, 0.003f, radius * 2f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = GetArenaRingMaterial();
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            return arena;
        }
    }
}
