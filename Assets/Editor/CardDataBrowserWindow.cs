using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在一个表格中集中查看项目里的所有CardData。
/// 此窗口主要用于对比和检查数据；需要修改时，点击ID即可在Inspector中编辑原Asset。
/// </summary>
public class CardDataBrowserWindow : EditorWindow
{
    private readonly List<CardData> cardDataList = new List<CardData>();

    private Vector2 scrollPosition;
    private string searchText = string.Empty;
    private bool includeTestCards;

    // 固定列宽可以让不同卡牌的同类数据始终对齐，方便上下比较。
    private const float RowWidth = 1450f;
    private const float IDWidth = 40f;
    private const float ImageWidth = 65f;
    private const float NameWidth = 130f;
    private const float PowerWidth = 45f;
    private const float CopiesWidth = 45f;
    private const float KeywordsWidth = 150f;
    private const float EffectsWidth = 240f;
    private const float FieldEffectsWidth = 260f;
    private const float DescriptionWidth = 400f;

    [MenuItem("MindBug/CardData 浏览器")]
    private static void OpenWindow()
    {
        CardDataBrowserWindow window =
            GetWindow<CardDataBrowserWindow>("CardData 浏览器");

        window.minSize = new Vector2(900f, 500f);
        window.RefreshCardData();
    }

    private void OnEnable()
    {
        RefreshCardData();

        // 确保窗口中同时显示较多卡图时，Unity能够缓存足够的预览图。
        AssetPreview.SetPreviewTextureCacheSize(100);
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawHeader();
        DrawCardTable();

        // AssetPreview会异步生成资源缩略图。在加载完成前重绘窗口，
        // 否则部分卡图可能要等到下一次鼠标操作后才会出现。
        if (AssetPreview.IsLoadingAssetPreviews())
        {
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("搜索", GUILayout.Width(35f));

            searchText = GUILayout.TextField(
                searchText,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(220f));

            includeTestCards = GUILayout.Toggle(
                includeTestCards,
                "包含测试卡",
                EditorStyles.toolbarButton,
                GUILayout.Width(90f));

            if (GUILayout.Button(
                "刷新",
                EditorStyles.toolbarButton,
                GUILayout.Width(60f)))
            {
                RefreshCardData();
            }

            GUILayout.FlexibleSpace();

            // 筛选掉测试卡时，计数也只反映当前表格中能看到的卡牌。
            int errorCount = GetVisibleCards().Count(HasValidationError);

            GUI.color = errorCount > 0
                ? new Color(1f, 0.5f, 0.5f)
                : Color.white;

            GUILayout.Label($"存在问题：{errorCount}");
            GUI.color = Color.white;
        }
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(
            EditorStyles.helpBox,
            GUILayout.Width(RowWidth)))
        {
            DrawLabel("ID", IDWidth, EditorStyles.boldLabel);
            DrawLabel("图片", ImageWidth, EditorStyles.boldLabel);
            DrawLabel("名称", NameWidth, EditorStyles.boldLabel);
            DrawLabel("力量", PowerWidth, EditorStyles.boldLabel);
            DrawLabel("张数", CopiesWidth, EditorStyles.boldLabel);
            DrawLabel("关键词", KeywordsWidth, EditorStyles.boldLabel);
            DrawLabel("普通效果", EffectsWidth, EditorStyles.boldLabel);
            DrawLabel("场上效果", FieldEffectsWidth, EditorStyles.boldLabel);
            DrawLabel("描述", DescriptionWidth, EditorStyles.boldLabel);
        }
    }

    private void DrawCardTable()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            true,
            true);

        foreach (CardData cardData in GetVisibleCards())
        {
            DrawCardRow(cardData);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCardRow(CardData cardData)
    {
        bool hasError = HasValidationError(cardData);
        Color originalColor = GUI.backgroundColor;

        if (hasError)
        {
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        }

        using (new EditorGUILayout.HorizontalScope(
            EditorStyles.helpBox,
            GUILayout.Width(RowWidth),
            GUILayout.Height(70f)))
        {
            // ID同时作为选择按钮。点击后在Project和Inspector中定位原CardData。
            if (GUILayout.Button(
                cardData.CardDataID.ToString(),
                GUILayout.Width(IDWidth),
                GUILayout.Height(55f)))
            {
                SelectCardData(cardData);
            }

            DrawCardPreview(cardData);
            DrawLabel(cardData.CardName, NameWidth);
            DrawLabel(cardData.Power.ToString(), PowerWidth);
            DrawLabel(cardData.Copies.ToString(), CopiesWidth);
            DrawLabel(cardData.CardKeywords.ToString(), KeywordsWidth);

            DrawLabel(
                GetCardEffectNames(cardData),
                EffectsWidth,
                EditorStyles.wordWrappedMiniLabel);

            DrawLabel(
                GetFieldEffectNames(cardData),
                FieldEffectsWidth,
                EditorStyles.wordWrappedMiniLabel);

            DrawLabel(
                cardData.Description,
                DescriptionWidth,
                EditorStyles.wordWrappedMiniLabel);
        }

        GUI.backgroundColor = originalColor;
    }

    private static void DrawCardPreview(CardData cardData)
    {
        Texture preview = null;

        if (cardData.CardImage != null)
        {
            preview = AssetPreview.GetAssetPreview(cardData.CardImage);

            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(cardData.CardImage);
            }
        }

        GUILayout.Box(
            preview,
            GUILayout.Width(ImageWidth),
            GUILayout.Height(60f));
    }

    private IEnumerable<CardData> GetVisibleCards()
    {
        IEnumerable<CardData> result = cardDataList;

        if (!includeTestCards)
        {
            result = result.Where(cardData =>
                !AssetDatabase.GetAssetPath(cardData)
                    .Contains("/TestCards/"));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string keyword = searchText.Trim().ToLowerInvariant();

            result = result.Where(cardData =>
                (cardData.CardName ?? string.Empty)
                    .ToLowerInvariant().Contains(keyword) ||
                cardData.CardDataID.ToString().Contains(keyword) ||
                (cardData.Description ?? string.Empty)
                    .ToLowerInvariant().Contains(keyword));
        }

        return result;
    }

    private void RefreshCardData()
    {
        cardDataList.Clear();

        // 只搜索CardData目录及其子目录，避免扫描整个项目。
        string[] assetGUIDs = AssetDatabase.FindAssets(
            "t:CardData",
            new[] { "Assets/CardData" });

        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData =
                AssetDatabase.LoadAssetAtPath<CardData>(assetPath);

            if (cardData != null)
            {
                cardDataList.Add(cardData);
            }
        }

        cardDataList.Sort(
            (left, right) =>
                left.CardDataID.CompareTo(right.CardDataID));

        Repaint();
    }

    /// <summary>
    /// 第一版只检查容易导致实际运行问题或资源遗漏的基础错误。
    /// </summary>
    private bool HasValidationError(CardData cardData)
    {
        // 正式卡和TestCards允许分别使用自己的ID范围，互相不判定为重复。
        // 这样测试用的1号卡不会让正式的1号卡也被错误标红。
        bool isTestCard = IsTestCard(cardData);
        bool duplicateID = cardDataList.Count(other =>
            IsTestCard(other) == isTestCard &&
            other.CardDataID == cardData.CardDataID) > 1;

        bool containsNullEffect =
            cardData.CardEffects.Any(effect => effect == null) ||
            cardData.CardFieldEffects.Any(effect => effect == null);

        return duplicateID ||
               string.IsNullOrWhiteSpace(cardData.CardName) ||
               cardData.CardImage == null ||
               cardData.Copies <= 0 ||
               containsNullEffect;
    }

    private static bool IsTestCard(CardData cardData)
    {
        return AssetDatabase.GetAssetPath(cardData)
            .Contains("/TestCards/");
    }

    private static string GetCardEffectNames(CardData cardData)
    {
        if (cardData.CardEffects.Count == 0)
        {
            return "-";
        }

        return string.Join(
            "\n",
            cardData.CardEffects.Select(effect =>
                effect == null
                    ? "Missing Effect"
                    : $"{effect.Trigger}: {effect.name}"));
    }

    private static string GetFieldEffectNames(CardData cardData)
    {
        if (cardData.CardFieldEffects.Count == 0)
        {
            return "-";
        }

        return string.Join(
            "\n",
            cardData.CardFieldEffects.Select(effect =>
                effect == null
                    ? "Missing Effect"
                    : effect.name));
    }

    private static void SelectCardData(CardData cardData)
    {
        Selection.activeObject = cardData;
        EditorGUIUtility.PingObject(cardData);
    }

    private static void DrawLabel(
        string text,
        float width,
        GUIStyle style = null)
    {
        GUILayout.Label(
            text ?? string.Empty,
            style ?? EditorStyles.label,
            GUILayout.Width(width),
            GUILayout.Height(60f));
    }
}
