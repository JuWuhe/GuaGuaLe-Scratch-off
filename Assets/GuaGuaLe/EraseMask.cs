using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EraseMask : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    // 是否擦除了
    public bool isStartEraser;
    // 是否擦除结束了
    public bool isEndEraser;
    
    // 开始事件
    private Action eraserStartEvent;
    // 结束事件
    private Action eraserEndEvent;
    
    // 被擦除的图片
    [SerializeField]
    private RawImage uiTex;
    
    private Texture2D tex;
    private Texture2D myTex;
    private int mWidth;
    private int mHeight;
    
    [Header("Brush Size")]
    public int brushSize = 50;
    [Header("Rate")]
    public int rate = 90;
    
    // 计算进度
    private float maxColorA;
    private float colorA;

    private void Awake()
    {
        tex = (Texture2D)uiTex.mainTexture;
        myTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
        mWidth = myTex.width;
        mHeight = myTex.height;
        myTex.SetPixels(tex.GetPixels());
        myTex.Apply();
        
        uiTex.texture = myTex;
        
        maxColorA = myTex.GetPixels().Length;
        colorA = 0;
        
        isEndEraser = false;
        isStartEraser = false;
    }

    #region MouseAction
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isEndEraser) { return; }
        CheckPoint(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isEndEraser) { return; }
        CheckPoint(eventData.position);
    }
    
    #endregion
    
    /// <summary>
    /// 刮除点击周围
    /// </summary>
    /// <param name="screenPos"></param>
    private void CheckPoint(Vector3 screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector3 localPos = uiTex.gameObject.transform.InverseTransformPoint(worldPos);
        if (localPos.x > -mWidth / 2 && localPos.x < mWidth / 2 && localPos.y > -mHeight / 2 && localPos.y < mHeight / 2)
        {
            for (int i = (int)localPos.x - brushSize; i < (int)localPos.x + brushSize; i++)
            {
                for (int j = (int)localPos.y - brushSize; j < (int)localPos.y + brushSize; j++)
                {
                    // 不在圆范围内的剔除
                    if (Mathf.Pow(i - localPos.x, 2) + Mathf.Pow(j - localPos.y, 2) > Mathf.Pow(brushSize, 2))
                        continue;
                    // 越界剔除
                    if (i < 0 && i < -mWidth / 2)  { continue; }
                    if (i > 0 && i >  mWidth / 2)  { continue; }
                    if (j < 0 && j < -mHeight / 2) { continue; }
                    if (j > 0 && j > mHeight / 2)  { continue; }
                    
                    // 获取像素
                    // 纹理坐标从左下角开始
                    // Texture coordinates start at lower left corner.
                    Color col = myTex.GetPixel(i + (int)mWidth / 2, j + (int)mHeight / 2);
                    if (col.a != 0f)
                    {
                        col.a = 0.0f;
                        colorA++;
                        myTex.SetPixel(i + (int)mWidth / 2, j + (int)mHeight / 2, col);
                    }
                }
            }
            // 开始刮的时候 去判断进度
            if (!isStartEraser)
            {
                isStartEraser = true;
                InvokeRepeating(nameof(GetTransparentPercent), 0f, 0.2f);
                eraserStartEvent += () =>
                {
                    Debug.Log("开始擦除");
                };
                eraserStartEvent?.Invoke();
            }
            myTex.Apply();
        }
    }
    
    /// <summary>
    /// 当前擦除进度
    /// </summary>
    private double fate;
    /// <summary> 
    /// 检测当前擦除进度
    /// </summary>
    /// <returns></returns>
    private void GetTransparentPercent()
    {
        if (isEndEraser) { return; }
        fate = colorA / maxColorA * 100;
        fate = Math.Round(fate, 2);
        // Debug.Log("当前百分比: " + fate);
        if (fate >= rate)
        {
            isEndEraser = true;
            CancelInvoke(nameof(GetTransparentPercent));
            //触发结束事件
            eraserEndEvent += () =>
            {
                uiTex.gameObject.SetActive(false);
            };
            eraserEndEvent?.Invoke();
        }
    }
}