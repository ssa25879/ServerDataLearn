#if UNITY_EDITOR
using System.IO;
using Ranking;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class RankingSceneSetup
{
    [MenuItem("Tools/Ranking/Create Ranking Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("생성 중단", "Play 모드를 종료한 뒤 씬을 생성하세요.", "확인");
            return;
        }
        string path = EditorUtility.SaveFilePanelInProject("랭킹 씬 저장", "RankingScene", "unity", "새 랭킹 씬의 저장 위치를 선택하세요.", "Assets/Scenes");
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path))
        {
            EditorUtility.DisplayDialog("생성 중단", "기존 씬을 덮어쓰지 않습니다. 새 파일명을 선택하세요.", "확인");
            return;
        }
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansKR/NotoSansKR-Regular SDF.asset");
        if (font == null)
        {
            EditorUtility.DisplayDialog("폰트 확인 필요", "기존 NotoSansKR-Regular SDF 폰트를 찾지 못했습니다.", "확인");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.09f, 0.14f);
        cameraObject.transform.position = new Vector3(0, 0, -10);

        var events = new GameObject("RankingEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var canvasObject = new GameObject("RankingCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1000);
        // 세로형 UI가 가로 화면에서 압축되어 글자가 잘리지 않도록 높이에 맞춘다.
        scaler.matchWidthOrHeight = 1f;

        var panel = new GameObject("RankingPanel", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(36, 30);
        panelRect.offsetMax = new Vector2(-36, -30);
        var layout = panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Label(panel.transform, "Title", "랭킹", font, 40, 72);
        Label(panel.transform, "Rules", "이름별 최고 점수 · 동점은 최초 등록 순", font, 22, 38);
        var name = Input(panel.transform, "NameInput", "이름", font, false);
        var score = Input(panel.transform, "ScoreInput", "점수 (0 이상 정수)", font, true);
        var submit = Button(panel.transform, "SubmitButton", "점수 전송", font);
        var load = Button(panel.transform, "LoadButton", "상위 10명 조회", font);
        var status = Label(panel.transform, "StatusText", "이름과 점수를 입력하세요.", font, 22, 100);
        var ranking = Label(panel.transform, "RankingText", "조회 버튼을 누르면 랭킹을 표시합니다.", font, 25, 360);
        ranking.GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;

        var controller = canvasObject.AddComponent<RankingClient>();
        controller.nameInput = name;
        controller.scoreInput = score;
        controller.submitButton = submit;
        controller.loadButton = load;
        controller.statusText = status;
        controller.rankingText = ranking;
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new IOException("랭킹 씬을 저장하지 못했습니다: " + path);
        Selection.activeGameObject = canvasObject;
        EditorUtility.DisplayDialog("랭킹 씬 생성 완료", "기존 씬은 보존했습니다. 생성한 씬을 단독으로 열어 Play하세요. RankingCanvas의 Server Base Url을 실행 중인 서버 주소로 설정하세요.", "확인");
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string value, TMP_FontAsset font, int size, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = Color.white;
        text.richText = false;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Stretch(RectTransform rect, float margin)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, 0);
        rect.offsetMax = new Vector2(-margin, 0);
    }

    private static TMP_InputField Input(Transform parent, string name, string placeholder, TMP_FontAsset font, bool numeric)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(TMP_InputField), typeof(UnityEngine.UI.LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 60;
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.16f, 0.20f, 0.28f);
        var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
        viewport.transform.SetParent(obj.transform, false);
        Stretch((RectTransform)viewport.transform, 14);
        var text = Label(viewport.transform, "Text", "", font, 26, 60);
        Stretch(text.rectTransform, 0);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var hint = Label(viewport.transform, "Placeholder", placeholder, font, 26, 60);
        Stretch(hint.rectTransform, 0);
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.color = new Color(0.7f, 0.75f, 0.82f);
        var field = obj.GetComponent<TMP_InputField>();
        field.targetGraphic = image;
        field.textViewport = (RectTransform)viewport.transform;
        field.textComponent = text;
        field.placeholder = hint;
        field.lineType = TMP_InputField.LineType.SingleLine;
        if (numeric) field.contentType = TMP_InputField.ContentType.IntegerNumber;
        return field;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string caption, TMP_FontAsset font)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 60;
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.18f, 0.37f, 0.75f);
        var button = obj.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var text = Label(obj.transform, "Text", caption, font, 26, 60);
        Stretch(text.rectTransform, 8);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
#endif
