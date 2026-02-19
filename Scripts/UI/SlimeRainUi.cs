using UnityEngine;
using UnityEngine.UI;

public class UIRain : MonoBehaviour
{
    [Header("Parent")]
    public RectTransform parent;          // RainLayer (Panel)

    [Header("Drop Prefab")]
    public Image dropPrefab;              // Prefab капли (UI Image)

    [Header("Rain Settings")]
    public int spawnPerSecond = 120;      // сколько капель в секунду
    public Vector2 speedMinMax = new Vector2(400f, 900f);

    [Header("Pool Settings")]
    public int poolSize = 400;            // максимум капель на экране
    public float spawnPadding = 50f;

    private Image[] pool;
    private RectTransform[] poolRT;
    private float[] speed;

    private int index;
    private float spawnAcc;

    void Awake()
    {
        if (parent == null)
            parent = (RectTransform)transform;

        // создаём пул объектов
        pool = new Image[poolSize];
        poolRT = new RectTransform[poolSize];
        speed = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            Image img = Instantiate(dropPrefab, parent);
            img.raycastTarget = false;
            img.enabled = false;

            pool[i] = img;
            poolRT[i] = img.rectTransform;
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Спавн капель
        spawnAcc += spawnPerSecond * dt;
        while (spawnAcc >= 1f)
        {
            SpawnDrop();
            spawnAcc -= 1f;
        }

        // Движение капель вниз
        Vector2 size = parent.rect.size;
        float bottomY = -size.y * 0.5f - spawnPadding;

        for (int i = 0; i < poolSize; i++)
        {
            if (!pool[i].enabled)
                continue;

            Vector2 pos = poolRT[i].anchoredPosition;
            pos.y -= speed[i] * dt;
            poolRT[i].anchoredPosition = pos;

            // Удаляем каплю если ушла вниз
            if (pos.y < bottomY)
                pool[i].enabled = false;
        }
    }

    void SpawnDrop()
    {
        Vector2 size = parent.rect.size;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        Image img = pool[index];
        RectTransform rt = poolRT[index];

        int id = index;
        index = (index + 1) % poolSize;

        img.enabled = true;

        // Позиция сверху экрана
        float x = Random.Range(-halfW - spawnPadding, halfW + spawnPadding);
        float y = halfH + spawnPadding;

        rt.anchoredPosition = new Vector2(x, y);

        // Без поворота
        rt.localRotation = Quaternion.identity;

        // Скорость падения
        speed[id] = Random.Range(speedMinMax.x, speedMinMax.y);
    }
}
