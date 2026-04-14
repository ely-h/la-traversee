using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight ambient floating dust effect using pooled UI Images.
/// Attach to a GameObject inside a Canvas panel. Fully UI-native, no ParticleSystem needed.
/// </summary>
public class AmbientDustUI : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float minSize = 2f;
    [SerializeField] private float maxSize = 6f;
    [SerializeField] private float minSpeed = 8f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float minLifetime = 4f;
    [SerializeField] private float maxLifetime = 9f;
    [SerializeField] private float maxAlpha = 0.35f;
    [SerializeField] private float horizontalDrift = 15f;

    private RectTransform panelRect;
    private Sprite dotSprite;
    private readonly List<DustMote> motes = new List<DustMote>();

    private class DustMote
    {
        public RectTransform rt;
        public Image img;
        public float lifetime;
        public float age;
        public float speed;
        public float xDrift;
        public float peakAlpha;
    }

    private void OnEnable()
    {
        panelRect = GetComponent<RectTransform>();
        dotSprite = FindBuiltInSprite();

        // Spawn pool
        for (int i = motes.Count; i < particleCount; i++)
        {
            var go = new GameObject("Dust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            var mote = new DustMote
            {
                rt = go.GetComponent<RectTransform>(),
                img = go.GetComponent<Image>()
            };

            mote.img.sprite = dotSprite;
            mote.img.raycastTarget = false;

            ResetMote(mote, true);
            motes.Add(mote);
        }
    }

    private void OnDisable()
    {
        // Clean up spawned objects
        foreach (var mote in motes)
        {
            if (mote.rt != null)
                Destroy(mote.rt.gameObject);
        }
        motes.Clear();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime; // works even if Time.timeScale == 0

        foreach (var m in motes)
        {
            m.age += dt;

            if (m.age >= m.lifetime)
            {
                ResetMote(m, false);
                continue;
            }

            // Fade in first 25%, fade out last 25%
            float t = m.age / m.lifetime;
            float alpha;
            if (t < 0.25f)
                alpha = Mathf.Lerp(0, m.peakAlpha, t / 0.25f);
            else if (t > 0.75f)
                alpha = Mathf.Lerp(m.peakAlpha, 0, (t - 0.75f) / 0.25f);
            else
                alpha = m.peakAlpha;

            var c = m.img.color;
            c.a = alpha;
            m.img.color = c;

            // Drift upward + gentle horizontal sine wave
            var pos = m.rt.anchoredPosition;
            pos.y += m.speed * dt;
            pos.x += Mathf.Sin(m.age * 0.7f + m.xDrift) * horizontalDrift * dt;
            m.rt.anchoredPosition = pos;
        }
    }

    private void ResetMote(DustMote mote, bool randomAge)
    {
        float w = panelRect.rect.width;
        float h = panelRect.rect.height;

        float size = Random.Range(minSize, maxSize);
        mote.rt.sizeDelta = new Vector2(size, size);

        // Spawn randomly across the panel width, at the bottom
        float startY = randomAge ? Random.Range(-h / 2f, h / 2f) : -h / 2f - 10f;
        mote.rt.anchoredPosition = new Vector2(Random.Range(-w / 2f, w / 2f), startY);

        mote.lifetime = Random.Range(minLifetime, maxLifetime);
        mote.age = randomAge ? Random.Range(0, mote.lifetime) : 0;
        mote.speed = Random.Range(minSpeed, maxSpeed);
        mote.xDrift = Random.Range(0f, Mathf.PI * 2f);
        mote.peakAlpha = Random.Range(maxAlpha * 0.3f, maxAlpha);

        // Subtle warm white/gold tint variety
        float tint = Random.Range(0.85f, 1f);
        mote.img.color = new Color(1f, tint, tint * 0.9f, 0f);
    }

    private Sprite FindBuiltInSprite()
    {
        // Use Unity's built-in "Knob" sprite (soft circle)
        var knob = Resources.Load<Sprite>("UI/Skin/Knob");
        if (knob != null) return knob;

        // Procedural fallback: tiny soft circle
        var tex = new Texture2D(8, 8);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                float dx = (x - 3.5f) / 3.5f;
                float dy = (y - 3.5f) / 3.5f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, d));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 8, 8), Vector2.one * 0.5f);
    }
}
