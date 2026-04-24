using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoleculeUIManager : MonoBehaviour
{
    public static MoleculeUIManager Instance;

  
    public RectTransform popupPanel;

    [Header("Text Fields")]
    public TextMeshProUGUI moleculeNameText;
    public TextMeshProUGUI moleculeSymbolText;
    public TextMeshProUGUI moleculeDescriptionText;

    [Header("Close Button (optional)")]
    public Button closeButton;

    [Header("Animation Settings")]
    [Tooltip("How long the pop-in takes")]
    public float animInDuration = 0.45f;

    [Tooltip("How long the pop-out takes")]
    public float animOutDuration = 0.35f;

    [Tooltip("Seconds before the panel auto-dismisses (0 = never)")]
    public float autoDismissDelay = 4f;

    [Tooltip("Overshoot for the bounce effect (DOTween Ease.OutBack strength)")]
    public float overshootStrength = 1.6f;

    private Sequence _currentSequence;
    private bool _isVisible = false;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

       
        if (popupPanel != null)
            popupPanel.localScale = Vector3.zero;

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePopup);
    }


    public void ShowPopup(string moleculeName, string symbol, string description)
    {
        if (popupPanel == null)
        {
            Debug.LogWarning("[MoleculeUIManager] popupPanel not assigned!");
            return;
        }

        if (moleculeNameText != null) moleculeNameText.text = moleculeName;
        if (moleculeSymbolText != null) moleculeSymbolText.text = symbol;
        if (moleculeDescriptionText != null) moleculeDescriptionText.text = description;

        _currentSequence?.Kill();
        popupPanel.localScale = Vector3.zero;

        _currentSequence = DOTween.Sequence();


        _currentSequence.Append(
            popupPanel
                .DOScale(Vector3.one, animInDuration)
                .SetEase(Ease.OutBack, overshootStrength)
        );

        if (autoDismissDelay > 0f)
        {
            _currentSequence.AppendInterval(autoDismissDelay);
            _currentSequence.AppendCallback(() => HidePopup());
        }

        _currentSequence.SetUpdate(true); 
        _currentSequence.Play();

        _isVisible = true;
        Debug.Log($"[MoleculeUIManager] Showing popup: {moleculeName} ({symbol})");
    }

  
    public void HidePopup()
    {
        if (popupPanel == null || !_isVisible) return;

        _currentSequence?.Kill();

        _currentSequence = DOTween.Sequence();

        _currentSequence.Append(
            popupPanel
                .DOScale(Vector3.zero, animOutDuration)
                .SetEase(Ease.InBack)
        );

        _currentSequence.SetUpdate(true);
        _currentSequence.Play();

        _isVisible = false;
    }
}