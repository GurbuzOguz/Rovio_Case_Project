using System.Collections.Generic;
using UnityEngine;
using Zenject;
using DG.Tweening;

public class GridView : MonoBehaviour
{
    [Header("Tile")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileScaleDuration = 0.4f;

    [SerializeField] private Transform tilesParent;

    [Header("Products")]
    [SerializeField] private GameObject productPrefab;
    [SerializeField] private float productHeightOffset = 0.1f;
    [SerializeField] private float productScaleDuration = 0.2f;
    [SerializeField] private float productSpawnDelay = 0.02f;

    [SerializeField] private Transform productsParent;

    private IGridService _gridService;
    private LevelLayout _levelLayout;
    private IProductViewService _productViewService;

    // colorId -> display color
    private readonly Dictionary<int, Color> _productColors =
        new Dictionary<int, Color>();

    private Vector3 _tileInitialScale = Vector3.one;
    private Vector3 _productInitialScale = Vector3.one;

    [Inject]
    public void Construct(
        IGridService gridService,
        LevelLayout levelLayout,
        [InjectOptional] IProductViewService productViewService)
    {
        _gridService = gridService;
        _levelLayout = levelLayout;
        _productViewService = productViewService;
    }

    private void Awake()
    {
        if (tilePrefab == null)
        {
            Debug.LogWarning("GridView: Tile prefab is not assigned.");
        }
        else
        {
            _tileInitialScale = tilePrefab.transform.localScale;
        }

        if (productPrefab == null)
        {
            Debug.LogWarning("GridView: Product prefab is not assigned.");
        }
        else
        {
            _productInitialScale = productPrefab.transform.localScale;
        }

        _productColors.Clear();

        ProductPalette palette = _levelLayout != null ? _levelLayout.productPalette : null;

        if (palette == null || palette.entries == null || palette.entries.Count == 0)
        {
            Debug.LogWarning("GridView: ProductPalette on LevelLayout is null or empty. Products will not be colored.");
        }
        else
        {
            foreach (var entry in palette.entries)
            {
                _productColors[entry.colorId] = entry.displayColor;
            }
        }
    }

    private void Start()
    {
        if (_gridService == null)
        {
            Debug.LogError("GridView: IGridService is not injected.");
            return;
        }

        if (_productViewService == null)
        {
            Debug.LogWarning("GridView: IProductViewService not injected. Products won't animate when collected.");
        }

        EnsureParents();
        BuildGrid();
        SpawnProducts();
    }

    private void EnsureParents()
    {
        if (tilesParent == null)
        {
            var tilesGo = new GameObject("Tiles");
            tilesGo.transform.SetParent(transform, false);
            tilesParent = tilesGo.transform;
        }

        if (productsParent == null)
        {
            var productsGo = new GameObject("Products");
            productsGo.transform.SetParent(transform, false);
            productsParent = productsGo.transform;
        }
    }

    private void BuildGrid()
    {
        if (tilePrefab == null)
        {
            return;
        }

        ProductPalette palette = _levelLayout != null ? _levelLayout.productPalette : null;

        for (int y = 0; y < _gridService.Rows; y++)
        {
            for (int x = 0; x < _gridService.Columns; x++)
            {
                var worldPos = _gridService.GridToWorld(x, y);
                var tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, tilesParent);
                tile.name = $"Tile_{x}_{y}";

                tile.transform.localScale = Vector3.zero;
                tile.transform
                    .DOScale(_tileInitialScale, tileScaleDuration)
                    .SetEase(Ease.OutBounce)
                    .SetLink(tile.gameObject, LinkBehaviour.KillOnDestroy);
            }
        }
    }

    private void SpawnProducts()
    {
        int autoColorIndex = 0;
        int spawnedCount = 0;

        ProductPalette palette = _levelLayout != null ? _levelLayout.productPalette : null;

        for (int y = 0; y < _gridService.Rows; y++)
        {
            for (int x = 0; x < _gridService.Columns; x++)
            {
                int colorId;

                if (_gridService.HasProductAt(x, y))
                {
                    colorId = _gridService.GetProductAt(x, y);
                }
                else if (palette != null && palette.entries != null && palette.entries.Count > 0)
                {
                    // Auto-fill empty tiles with colors in round-robin from palette
                    var entry = palette.entries[autoColorIndex % palette.entries.Count];
                    colorId = entry.colorId;
                    autoColorIndex++;
                    _gridService.AddProductAt(x, y, colorId);
                }
                else
                {
                    // No palette or entries, skip auto-fill
                    continue;
                }

                if (!_productColors.TryGetValue(colorId, out var baseColor))
                {
                    continue;
                }

                if (productPrefab == null)
                {
                    continue;
                }

                var worldPos = _gridService.GridToWorld(x, y) + Vector3.up * productHeightOffset;
                var product = Instantiate(productPrefab, worldPos, Quaternion.identity, productsParent);
                product.name = $"Product_{colorId}_{x}_{y}";

                var pv = product.GetComponent<ProductView>();
                if (pv == null)
                {
                    pv = product.AddComponent<ProductView>();
                }
                pv.Initialize(new Vector2Int(x, y), colorId);
                _productViewService?.Register(new Vector2Int(x, y), pv);
                if (_productViewService == null)
                {
                    // Register edemiyoruz, box pull animasyonu çalışmayacak
                }

                var renderer = product.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb);

#if UNITY_2021_2_OR_NEWER
                // URP Lit / HDRP Lit genellikle _BaseColor kullanıyor
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    mpb.SetColor("_BaseColor", baseColor);
                }
                else
#endif
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                {
                    mpb.SetColor("_Color", baseColor);
                }

                renderer.SetPropertyBlock(mpb);

                product.transform.localScale = Vector3.zero;
                float delay = spawnedCount * productSpawnDelay;
                product.transform
                    .DOScale(_productInitialScale, productScaleDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(delay)
                    .SetLink(product.gameObject, LinkBehaviour.KillOnDestroy);

                spawnedCount++;
            }
        }
    }
}

