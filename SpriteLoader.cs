using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(SpriteLoader.Main), "SpriteLoader", "1.0", "InhumanDegenerate")]
[assembly: MelonGame("OttersideGames", "SpiritValley")]

namespace SpriteLoader
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            HarmonyInstance.PatchAll();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "TitleScreen") return;

            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.name == "Pusseen")
                {
                    SpriteOverride spriteOverride = obj.AddComponent<SpriteOverride>();
                    Image image = obj.GetComponent<Image>();
                    spriteOverride.image = image;
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MonsterMenuImageComponent), "UpdateUI", new[] { typeof(MonsterBaseStats), typeof(bool) })]
    static class Patch_UpdateUI
    {
        static void Postfix(MonsterMenuImageComponent __instance)
        {
            SpriteOverride spriteOverride = __instance.GetComponent<SpriteOverride>();
            if (spriteOverride == null)
            {
                Image image = __instance.monsterImage;
                spriteOverride = __instance.AddComponent<SpriteOverride>();
                spriteOverride.image = image;
            }

            spriteOverride.frames = null;
        }
    }

    [HarmonyPatch(typeof(MonsterPortrait), "UpdateUI")]
    class Patch_MonsterPortrait_UpdateUI
    {
        static void Postfix(MonsterPortrait __instance)
        {
            Texture2D customTex = Util.LoadCustomTexture($"{__instance.portrait.mainTexture.name}");
            if (customTex == null) return;

            Sprite newSprite = Sprite.Create(customTex, new Rect(0, 0, customTex.width, customTex.height), new Vector2(0.5f, 0.5f));

            __instance.portrait.sprite = newSprite;
        }
    }

    [HarmonyPatch(typeof(MonsterUIInstance), "Start")]
    class Patch_MonsterUIInstance_Start
    {
        static void Postfix(MonsterUIInstance __instance)
        {
            SpriteOverride spriteOverride = __instance.AddComponent<SpriteOverride>();
            SpriteRenderer spriteRenderer = __instance.spriteRenderer;
            spriteOverride.spriteRenderer = spriteRenderer;
        }
    }
    public class SpriteOverride : MonoBehaviour
    {
        public Image image;
        public SpriteRenderer spriteRenderer;
        public Sprite[] frames;
        private string prevName;

        void LateUpdate()
        {
            if (frames == null)
            {
                Sprite sprite = (image == null) ? spriteRenderer.sprite : image.sprite;

                string texName = Util.ExtractFrameName(image.sprite.name);
                if (prevName == texName) return;
                prevName = texName;

                Texture2D customTex = Util.LoadCustomTexture(texName);
                if (customTex == null) return;

                int h = customTex.width / (int)sprite.rect.width;
                int v = customTex.height / (int)sprite.rect.height;

                frames = Util.SliceSpriteSheet(customTex, h, v, sprite.pixelsPerUnit);
            }

            if (image == null)
            {
                int frameNum = Util.ExtractFrameNumber(spriteRenderer.sprite.name);
                spriteRenderer.sprite = frames[frameNum];
                return;
            }
            if (spriteRenderer == null)
            {
                int frameNum = Util.ExtractFrameNumber(image.sprite.name);
                image.sprite = frames[frameNum];
            }
        }
    }

    public class Util
    {
        public static Texture2D LoadCustomTexture(string name)
        {
            var path = $"Mods/Textures/{name}.png";
            if (!System.IO.File.Exists(path)) return null;

            byte[] data = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (tex.LoadImage(data)) return tex;
            return null;
        }

        public static Sprite[] SliceSpriteSheet(Texture2D texture, int h_columns, int v_rows, float ppu)
        {
            List<Sprite> sprites = new List<Sprite>();

            int frameWidth = texture.width / h_columns;
            int frameHeight = texture.height / v_rows;

            for (int y = v_rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < h_columns; x++)
                {
                    Rect rect = new Rect(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    Sprite sprite = Sprite.Create(texture, rect, pivot, ppu);
                    sprites.Add(sprite);
                }
            }

            return sprites.ToArray();
        }

        public static string ExtractFrameName(string input)
        {
            int lastUnderscore = input.LastIndexOf('_');
            return input.Substring(0, lastUnderscore);
        }

        public static int ExtractFrameNumber(string input)
        {
            int lastUnderscore = input.LastIndexOf('_');
            return int.Parse(input.Substring(lastUnderscore + 1));
        }
    }
}
