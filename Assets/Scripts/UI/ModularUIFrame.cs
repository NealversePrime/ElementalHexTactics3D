using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalHexTactics3D.UI
{
    public enum UIModularTheme
    {
        HolySynod,
        DemonRebellion
    }

    public enum UIModularStyle
    {
        Sliced9,       // Native 9-Slice Grid (4 corners unscaled, 4 edges tile/stretch, center stretch)
        Modular3Piece  // 3-Piece Horizontal Assembly (Left Cap, Center Bar, Right Cap)
    }

    /// <summary>
    /// RPG-Maker-style modular UI frame controller.
    /// Allows dividing ornate fantasy UI frames into modular boxes (Left Cap, Center Bar, Right Cap, or 9-Slice)
    /// so the UI skin can be reused across any button, window, dialogue box, card, or modal without distortion.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModularUIFrame : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UIModularTheme theme = UIModularTheme.HolySynod;
        [SerializeField] private UIModularStyle style = UIModularStyle.Sliced9;
        [SerializeField] private float leftCapWidth = 64f;
        [SerializeField] private float rightCapWidth = 64f;

        private Image m_leftCapImage;
        private Image m_rightCapImage;
        private Image m_midBarImage;

        public UIModularTheme Theme => theme;
        public UIModularStyle Style => style;

        private void Awake()
        {
            ApplyStyle();
        }

        public void SetTheme(UIModularTheme newTheme)
        {
            theme = newTheme;
            ApplyStyle();
        }

        public void SetStyle(UIModularStyle newStyle)
        {
            style = newStyle;
            ApplyStyle();
        }

        public void ApplyStyle()
        {
            if (style == UIModularStyle.Sliced9)
            {
                ApplySliced9(gameObject, theme);
            }
            else
            {
                ApplyModular3Piece(gameObject, theme, leftCapWidth, rightCapWidth);
            }
        }

        // =========================================================================
        // STATIC UTILITIES: Can be called on ANY UI GameObject (Button, Panel, Window)
        // =========================================================================

        /// <summary>
        /// Applies a clean 9-slice fantasy frame to any UI element (Window, Panel, Button).
        /// </summary>
        public static void ApplySliced9(GameObject target, UIModularTheme themeMode)
        {
            if (target == null) return;
            Image img = target.GetComponent<Image>();
            if (img == null) img = target.AddComponent<Image>();

            string spriteName = (themeMode == UIModularTheme.HolySynod)
                ? "UI_Frame_Holy_9Slice"
                : "UI_Frame_Demon_9Slice";

            Sprite sp = LoadSprite(spriteName);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.preserveAspect = false;
                img.color = Color.white;
            }
        }

        /// <summary>
        /// Assembles a 3-piece modular frame (Left Cap, Center Bar, Right Cap) on any UI container.
        /// Guaranteed 100% distortion-free ornate caps with an infinitely stretchable center bar.
        /// </summary>
        public static void ApplyModular3Piece(GameObject target, UIModularTheme themeMode, float capWidth = 60f, float capOffset = 0f)
        {
            if (target == null) return;
            RectTransform rootRect = target.GetComponent<RectTransform>();
            if (rootRect == null) return;

            string prefix = (themeMode == UIModularTheme.HolySynod) ? "UI_Holy" : "UI_Demon";

            Sprite leftSprite = LoadModularSprite($"{prefix}_Cap_Left");
            Sprite midSprite  = LoadModularSprite($"{prefix}_Bar_Mid");
            Sprite rightSprite = LoadModularSprite($"{prefix}_Cap_Right");

            // 1. Center MidBar (stretched between caps)
            Transform midTrans = target.transform.Find("Frame_MidBar");
            if (midTrans == null)
            {
                GameObject midObj = new GameObject("Frame_MidBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                midObj.transform.SetParent(target.transform, false);
                midObj.transform.SetAsFirstSibling();
                midTrans = midObj.transform;
            }
            RectTransform midRect = midTrans.GetComponent<RectTransform>();
            midRect.anchorMin = Vector2.zero;
            midRect.anchorMax = Vector2.one;
            midRect.offsetMin = new Vector2(capWidth - 4f, 0f); // 4px overlap to eliminate subpixel gaps
            midRect.offsetMax = new Vector2(-(capWidth - 4f), 0f);

            Image midImg = midTrans.GetComponent<Image>();
            midImg.sprite = midSprite;
            midImg.type = Image.Type.Simple;
            midImg.raycastTarget = false;

            // 2. Left Cap (anchored strictly to left edge, unscaled aspect)
            Transform leftTrans = target.transform.Find("Frame_LeftCap");
            if (leftTrans == null)
            {
                GameObject leftObj = new GameObject("Frame_LeftCap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                leftObj.transform.SetParent(target.transform, false);
                leftObj.transform.SetSiblingIndex(1);
                leftTrans = leftObj.transform;
            }
            RectTransform leftRect = leftTrans.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.anchoredPosition = new Vector2(capOffset, 0f);
            leftRect.sizeDelta = new Vector2(capWidth, 0f);

            Image leftImg = leftTrans.GetComponent<Image>();
            leftImg.sprite = leftSprite;
            leftImg.type = Image.Type.Simple;
            leftImg.preserveAspect = false;
            leftImg.raycastTarget = false;

            // 3. Right Cap (anchored strictly to right edge, unscaled aspect)
            Transform rightTrans = target.transform.Find("Frame_RightCap");
            if (rightTrans == null)
            {
                GameObject rightObj = new GameObject("Frame_RightCap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rightObj.transform.SetParent(target.transform, false);
                rightObj.transform.SetSiblingIndex(2);
                rightTrans = rightObj.transform;
            }
            RectTransform rightRect = rightTrans.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.pivot = new Vector2(1f, 0.5f);
            rightRect.anchoredPosition = new Vector2(-capOffset, 0f);
            rightRect.sizeDelta = new Vector2(capWidth, 0f);

            Image rightImg = rightTrans.GetComponent<Image>();
            rightImg.sprite = rightSprite;
            rightImg.type = Image.Type.Simple;
            rightImg.preserveAspect = false;
            rightImg.raycastTarget = false;

            // Ensure root button image is transparent so it doesn't occlude the frame
            Image rootImg = target.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = new Color(1f, 1f, 1f, 0.001f); // nearly invisible but clickable
            }
        }

        // =========================================================================
        // ASSET LOADING & CACHING
        // =========================================================================

        private static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        public static Sprite LoadSprite(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return null;
            if (baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                baseName = Path.GetFileNameWithoutExtension(baseName);

            if (s_cache.TryGetValue(baseName, out Sprite cached) && cached != null)
                return cached;

            // 1. Resources.Load
            Sprite sp = Resources.Load<Sprite>($"UI/{baseName}");
            if (sp != null)
            {
                s_cache[baseName] = sp;
                return sp;
            }

            // 2. Editor AssetDatabase
#if UNITY_EDITOR
            string[] searchPaths = new string[]
            {
                $"Assets/Resources/UI/{baseName}.png",
                $"Assets/Sprites/UI/{baseName}.png",
                $"Assets/UI/{baseName}.png"
            };
            foreach (string p in searchPaths)
            {
                Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (edSprite != null)
                {
                    s_cache[baseName] = edSprite;
                    return edSprite;
                }
            }
#endif

            // 3. Fallback disk bytes
            string[] diskPaths = new string[]
            {
                Path.Combine(Application.dataPath, "Resources", "UI", $"{baseName}.png"),
                Path.Combine(Application.dataPath, "Sprites", "UI", $"{baseName}.png")
            };
            foreach (string dp in diskPaths)
            {
                if (File.Exists(dp))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(dp);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            Vector4 border = new Vector4(60f, 60f, 60f, 60f);
                            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                            s_cache[baseName] = sp;
                            return sp;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ModularUIFrame] Failed disk load for {dp}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        public static Sprite LoadModularSprite(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return null;
            if (baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                baseName = Path.GetFileNameWithoutExtension(baseName);

            string key = $"Modular/{baseName}";
            if (s_cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Sprite sp = Resources.Load<Sprite>($"UI/Modular/{baseName}");
#if UNITY_EDITOR
            if (sp == null)
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Modular/{baseName}.png")
                  ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Modular/{baseName}.png");
            }
#endif
            if (sp == null)
            {
                string[] diskPaths = new string[]
                {
                    Path.Combine(Application.dataPath, "Resources", "UI", "Modular", $"{baseName}.png"),
                    Path.Combine(Application.dataPath, "Sprites", "UI", "Modular", $"{baseName}.png")
                };
                foreach (string dp in diskPaths)
                {
                    if (File.Exists(dp))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(dp);
                            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(bytes))
                            {
                                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ModularUIFrame] Failed to load modular sprite {dp}: {ex.Message}");
                        }
                    }
                }
            }

            if (sp != null) s_cache[key] = sp;
            return sp;
        }
    }
}

