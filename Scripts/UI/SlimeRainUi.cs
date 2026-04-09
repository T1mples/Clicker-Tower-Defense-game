using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRain : MonoBehaviour
{
    [Header("Parent")]
    public RectTransform parent;

    [Header("Drop Prefab")]
    public Image dropPrefab;

    [Header("Rain Settings")]
    public int spawnPerSecond = 120;
    public Vector2 speedMinMax = new Vector2(400f, 900f);

    [Header("Pool Settings")]
    public int poolSize = 400;
    public float spawnPadding = 0f;
    public bool ensureRectMaskOnParent = true;

    [Header("Safety")]
    public bool cleanupNonPoolDrops = true;
    public float cleanupInterval = 1f;

    private Image[] pool;
    private RectTransform[] poolRT;
    private float[] speed;
    private readonly HashSet<int> poolIds = new HashSet<int>();
    private readonly Vector3[] parentWorldCorners = new Vector3[4];

    private int index;
    private float spawnAcc;
    private float cleanupTimer;

    private void Awake()
    {
        if (parent == null)
        {
            parent = (RectTransform)transform;
        }

        if (dropPrefab == null)
        {
            enabled = false;
            return;
        }

        if (ensureRectMaskOnParent && parent.GetComponent<RectMask2D>() == null)
        {
            parent.gameObject.AddComponent<RectMask2D>();
        }

        poolSize = Mathf.Max(1, poolSize);
        pool = new Image[poolSize];
        poolRT = new RectTransform[poolSize];
        speed = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            Image img = Instantiate(dropPrefab, parent);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            img.raycastTarget = false;
            img.gameObject.SetActive(false);

            pool[i] = img;
            poolRT[i] = rt;
            poolIds.Add(img.gameObject.GetInstanceID());
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        spawnAcc += Mathf.Max(0, spawnPerSecond) * dt;
        while (spawnAcc >= 1f)
        {
            SpawnDrop();
            spawnAcc -= 1f;
        }

        parent.GetWorldCorners(parentWorldCorners);
        float parentBottomWorldY = parentWorldCorners[0].y;

        for (int i = 0; i < poolSize; i++)
        {
            Image img = pool[i];
            if (img == null || !img.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform rt = poolRT[i];
            Vector2 pos = rt.anchoredPosition;
            pos.y -= speed[i] * dt;
            rt.anchoredPosition = pos;

            float dropHalfHWorld = rt.rect.height * rt.lossyScale.y * 0.5f;
            float dropBottomWorldY = rt.position.y - dropHalfHWorld;
            if (dropBottomWorldY <= parentBottomWorldY - spawnPadding)
            {
                img.gameObject.SetActive(false);
            }
        }

        if (cleanupNonPoolDrops)
        {
            cleanupTimer += dt;
            if (cleanupTimer >= Mathf.Max(0.1f, cleanupInterval))
            {
                cleanupTimer = 0f;
                CleanupLeakedDrops();
            }
        }
    }

    private void SpawnDrop()
    {
        Image img = pool[index];
        RectTransform rt = poolRT[index];
        int id = index;
        index = (index + 1) % poolSize;

        if (img == null || rt == null)
        {
            return;
        }

        float parentXMin = parent.rect.xMin;
        float parentXMax = parent.rect.xMax;
        float parentYMax = parent.rect.yMax;

        float dropHalfW = rt.rect.width * 0.5f;
        float dropHalfH = rt.rect.height * 0.5f;

        float minX = parentXMin + dropHalfW + spawnPadding;
        float maxX = parentXMax - dropHalfW - spawnPadding;
        if (minX > maxX)
        {
            minX = maxX = 0f;
        }

        float x = Random.Range(minX, maxX);
        float y = parentYMax - dropHalfH;

        rt.anchoredPosition = new Vector2(x, y);
        rt.localRotation = Quaternion.identity;

        speed[id] = Random.Range(speedMinMax.x, speedMinMax.y);
        img.gameObject.SetActive(true);
    }

    private void CleanupLeakedDrops()
    {
        if (parent == null)
        {
            return;
        }

        string expectedName = dropPrefab != null ? dropPrefab.gameObject.name : string.Empty;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            GameObject go = child.gameObject;
            if (poolIds.Contains(go.GetInstanceID()))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(expectedName) && !go.name.StartsWith(expectedName))
            {
                continue;
            }

            Destroy(go);
        }
    }
}
