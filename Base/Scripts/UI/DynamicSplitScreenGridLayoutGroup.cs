using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicSplitscreenGridLayoutGroup : MonoBehaviour
{
    private GridLayoutGroup gridLayout;

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();

        if (gridLayout == null)
        {
            Debug.LogError("GridLayoutGroup component not found!");
            return;
        }

        // Initial setup
        UpdateGridLayout();

    }

    public void RefreshDynamicGrid()
    {
        UpdateGridLayout();
    }

    void OnTransformChildrenChanged()
    {
        UpdateGridLayout();
    }

    private void ClearLayoutElem()
    {
        if (transform.childCount < 1) { return; }
        // Two equal-sized screens on top
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<LayoutElement>())
            {
                Destroy(transform.GetChild(i).gameObject.GetComponent<LayoutElement>());
            }
        }
    }

    void UpdateGridLayout()
    {
        Vector2 gridLayoutScreenSize = new Vector2(gridLayout.GetComponent<RectTransform>().rect.width, gridLayout.GetComponent<RectTransform>().rect.height);
        gridLayout.cellSize = new Vector2(gridLayoutScreenSize.x / gridLayout.constraintCount, gridLayoutScreenSize.y / gridLayout.constraintCount);

        int childCount = transform.childCount;
        Debug.Log(childCount);

        if (childCount == 0) return;
        if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            
            if (childCount == 1)
            {
                gridLayout.constraintCount = 1;
                gridLayout.cellSize = gridLayoutScreenSize;
                ClearLayoutElem();
            }
            else if (childCount == 2)
            {
                gridLayout.constraintCount = 2;
                gridLayout.cellSize = new Vector2(gridLayoutScreenSize.x / 2, gridLayoutScreenSize.y);
                ClearLayoutElem();
            }
            else if (childCount == 3)
            {
                gridLayout.constraintCount = 2;

                // Two equal-sized screens on top
                for (int i = 0; i < 2; i++)
                {
                    RectTransform childRect = transform.GetChild(i).GetComponent<RectTransform>();
                    childRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gridLayoutScreenSize.x / 2);
                    childRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gridLayoutScreenSize.y / 2);
                }

                // Third child takes full width at bottom
                RectTransform bottomChildRect = transform.GetChild(2).GetComponent<RectTransform>();
                LayoutElement layoutElem;
                if (bottomChildRect.gameObject.GetComponent<LayoutElement>() == null)
                {
                    layoutElem = bottomChildRect.gameObject.AddComponent<LayoutElement>();
                }
                else
                {
                    layoutElem = bottomChildRect.gameObject.GetComponent<LayoutElement>();
                }
                layoutElem.ignoreLayout = true;
                bottomChildRect.offsetMax = new Vector2(0, 0);
                bottomChildRect.offsetMin = new Vector2(0, 0);
                bottomChildRect.anchorMin = new Vector2(0, 0);
                bottomChildRect.anchorMax = new Vector2(1, 0);
                bottomChildRect.sizeDelta = new Vector2(0, (gridLayoutScreenSize.y / 2)-gridLayout.spacing.y);
                bottomChildRect.pivot = new Vector2(0.5f, 0);
            }
            else if (childCount == 4)
            {
                gridLayout.constraintCount = 2;
                gridLayout.cellSize = new Vector2(gridLayoutScreenSize.x / 2, gridLayoutScreenSize.y /2);
                ClearLayoutElem();
            }
        }
    }

}