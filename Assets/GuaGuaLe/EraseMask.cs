using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EraseMask : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // 是否擦除了
    public bool isStartEraser;
    // 是否擦除结束了
    public bool isEndEraser;
    // 开始事件
    public Action eraserStartEvent;
    // 结束事件
    public Action eraserEndEvent;
    
    public RawImage uiTex; // 上层图片
    Texture2D tex;
    Texture2D MyTex;
    int mWidth;
    int mHeight;
    [Header("Brush Size")]
    public int brushSize = 50;
    [Header("Rate")]
    public int rate = 90;
    
    // 计算进度
    float maxColorA;
    float colorA;

    void Awake()
    {
        tex = (Texture2D)uiTex.mainTexture;
        MyTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
        mWidth = MyTex.width;
        mHeight = MyTex.height;
        
        MyTex.SetPixels(tex.GetPixels());
        MyTex.Apply();
        
        uiTex.texture = MyTex;
        maxColorA = MyTex.GetPixels().Length;
        // Debug.Log(maxColorA);
        colorA = 0;
        isEndEraser = false;
        isStartEraser = false;
    }

    bool twoPoints = false;
    Vector2 lastPos;   // 最后一个点
    Vector2 penultPos; // 倒数第二个点
    float distance = 1f;
    
    #region 事件
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isEndEraser) { return; }
        penultPos = eventData.position;
        CheckPoint(penultPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isEndEraser) { return; }
        if (twoPoints && Vector2.Distance(eventData.position, lastPos) > distance) // 如果两次记录的鼠标坐标距离大于一定的距离，开始记录鼠标的点
        {
            Vector2 pos = eventData.position;
            float dis = Vector2.Distance(lastPos, pos);

            CheckPoint(eventData.position);
        }
        else
        {
            twoPoints = true;
            lastPos = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isEndEraser) { return; }
        //CheckPoint(eventData.position);
        twoPoints = false;
    }
    #endregion
    
    /// <summary>
    /// 刮除点击周围
    /// </summary>
    /// <param name="pScreenPos"></param>
    void CheckPoint(Vector3 pScreenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(pScreenPos);
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
                    Color col = MyTex.GetPixel(i + (int)mWidth / 2, j + (int)mHeight / 2);
                    if (col.a != 0f)
                    {
                        col.a = 0.0f;
                        colorA++;
                        MyTex.SetPixel(i + (int)mWidth / 2, j + (int)mHeight / 2, col);
                    }
                }
            }
            // 开始刮的时候 去判断进度
            if (!isStartEraser)
            {
                isStartEraser = true;
                InvokeRepeating("getTransparentPercent", 0f, 0.2f);
                if (eraserStartEvent != null)
                    eraserStartEvent.Invoke();
            }
            MyTex.Apply();
        }
    }
    
    double fate;
    /// <summary> 
    /// 检测当前刮刮卡 进度
    /// </summary>
    /// <returns></returns>
    public void getTransparentPercent()
    {
        if (isEndEraser) { return; }
        fate = colorA / maxColorA * 100;
        fate = Math.Round(fate, 2);
        // Debug.Log("当前百分比: " + fate);
        if (fate >= rate)
        {
            isEndEraser = true;
            CancelInvoke("getTransparentPercent");
            uiTex.gameObject.SetActive(false);
            //触发结束事件
            if (eraserEndEvent != null)
                eraserEndEvent.Invoke();
        }
    }
}