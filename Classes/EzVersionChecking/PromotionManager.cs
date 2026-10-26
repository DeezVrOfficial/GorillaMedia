using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GorillaMedia.Classes.EzVersionChecking;

internal class PromotionManager : MonoBehaviour
{
    public TextMeshProUGUI TextObj;

    private GameObject imageObj;
    private GameObject stumpObj;
    private Image uiImage;
    public static PromotionManager Instance { get; private set; }

    private void Awake() => Instance = this;

    public void Start() => GorillaTagger.OnPlayerSpawned(SpawnStumpObj);

    private void Update()
    {
        if (stumpObj != null && Camera.main != null)
        {
            stumpObj.transform.LookAt(Camera.main.transform.position);
            stumpObj.transform.Rotate(0f, 180f, 0f);
        }

        if (VRRig.LocalRig != null)
            uiImage.color = VRRig.LocalRig.playerColor;
    }

    public void SpawnStumpObj()
    {
        stumpObj = new GameObject($"DGMPromoStump");
        Canvas canvas = stumpObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = stumpObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;
        stumpObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = stumpObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 2f);
        stumpObj.transform.position = new Vector3(-66.9419f, 13f, -82.6273f);
        stumpObj.transform.localScale = Vector3.one * 0.002f;
        stumpObj.transform.Rotate(0f, 180f, 0f);

        GameObject bgObj = new("Background");
        bgObj.transform.SetParent(stumpObj.transform, false);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
        bgImage.raycastTarget = false;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(400f, 300f);
        bgRect.localScale = Vector3.one;
        bgRect.anchoredPosition = Vector2.zero;

        TextObj = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        TextObj.transform.SetParent(stumpObj.transform, false);
        TextObj.font = LoadFont("comicbd");
        TextObj.text =
                $"<color=blue>{Constants.Name}</color>\n<color=white>Made By</color> <color=black>Deez</color>";

        TextObj.fontSize = 50f;
        TextObj.color = Color.paleVioletRed;
        TextObj.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = TextObj.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0f, -50f);
        textRect.sizeDelta = new Vector2(400f, 200f);

        Texture2D tex = LoadEmbeddedImage("GorillaMedia.png");

        if (tex == null)
            return;

        imageObj = new GameObject("MediaIcon");
        imageObj.transform.SetParent(stumpObj.transform, false);
        uiImage = imageObj.AddComponent<Image>();

        RectTransform imgRect = imageObj.GetComponent<RectTransform>();

        float targetHeight = 100f;
        float aspect = (float)tex.width / tex.height;
        float targetWidth = targetHeight * aspect * 1f;

        imgRect.sizeDelta = new Vector2(targetWidth, targetHeight);
        imgRect.anchoredPosition = new Vector2(0f, 80f);

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        uiImage.sprite = sprite;
    }

    private Texture2D LoadEmbeddedImage(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly()
                                      .GetManifestResourceStream("GorillaMedia.Resources." + name);

        if (stream == null) return null;
        byte[] imageData = new byte[stream.Length];
        stream.Read(imageData, 0, imageData.Length);
        Texture2D texture = new(2, 2);
        texture.LoadImage(imageData);

        return texture;
    }

    public static TMP_FontAsset LoadFont(string name)
    {
        Stream manifestResourceStream = Assembly.GetExecutingAssembly()
                                                .GetManifestResourceStream("GorillaMedia.Resources." + name + ".ttf");

        byte[] array = new byte[manifestResourceStream.Length];
        manifestResourceStream.Read(array, 0, array.Length);
        string text = Path.Combine(Application.temporaryCachePath, "TempFont.ttf");
        File.WriteAllBytes(text, array);
        TMP_FontAsset result = TMP_FontAsset.CreateFontAsset(new Font(text));
        manifestResourceStream?.Dispose();

        return result;
    }
}