using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class Ui3DStyleV2 : MonoBehaviour
{
    [Header("Renderers (留空自动抓取)")]
    public List<Renderer> backgroundRenderers = new();
    public List<Renderer> textRenderers = new();

    [Header("Shader property names (匹配 Shader Graph Reference)")]
    public string bgBaseProp = "_Color";        // BG: Base
    public string bgEmissionProp = "_Emission"; // BG: Emission(颜色型HDR)
    public string textColorProp = "_FaceColor"; // TMP

    [Header("State colors (HDR) — 背景")]
    [ColorUsage(true, true)] public Color bg_Hover = new Color(0, 1, 1, 1);
    [ColorUsage(true, true)] public Color bg_Selected = new Color(1, 1, 0, 1);

    [Header("State colors (HDR) — 文字")]
    [ColorUsage(true, true)] public Color text_Hover = new Color(0, 1, 1, 1);
    [ColorUsage(true, true)] public Color text_Selected = new Color(1, 1, 0, 1);

    [Header("过渡")]
    public float transitionSeconds = 0.5f;
    public bool useCurve = false;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool _hovered, _selected;
    Coroutine _co;

    struct Target
    {
        public Renderer r;
        public int baseId;     // 文字或BG的主色
        public int emisId;     // BG 发光色(可无)
        public MaterialPropertyBlock mpb;

        public Color origBase; // 启动时从材质读取到的默认色
        public Color origEmis; // 启动时的默认发光色
        public Color curBase;  // 插值当前色
        public Color curEmis;
    }
    readonly List<Target> _bg = new();
    readonly List<Target> _tx = new();

    void Awake()
    {
        if (backgroundRenderers.Count == 0 || textRenderers.Count == 0)
            AutoCollect();

        BuildTargets(backgroundRenderers, bgBaseProp, bgEmissionProp, _bg, isBg: true);
        BuildTargets(textRenderers, textColorProp, null, _tx, isBg: false);

        // 不做任何 Apply，默认外观由材质决定
    }

    void AutoCollect()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            bool isText = r.GetComponent<TMP_Text>() != null
                       || (r.sharedMaterial && r.sharedMaterial.shader
                           && r.sharedMaterial.shader.name.Contains("TextMeshPro"));
            if (isText) textRenderers.Add(r);
            else backgroundRenderers.Add(r);
        }
    }

    void BuildTargets(List<Renderer> src, string baseProp, string emisProp, List<Target> dst, bool isBg)
    {
        dst.Clear();
        foreach (var r in src)
        {
            if (!r) continue;
            var t = new Target
            {
                r = r,
                mpb = new MaterialPropertyBlock(),
                baseId = -1,
                emisId = -1
            };

            var mat = r.sharedMaterial;
            if (mat)
            {
                if (!string.IsNullOrEmpty(baseProp) && mat.HasProperty(baseProp))
                {
                    t.baseId = Shader.PropertyToID(baseProp);
                    t.origBase = mat.GetColor(baseProp); // 记录默认
                    t.curBase = t.origBase;
                }
                if (!string.IsNullOrEmpty(emisProp) && mat.HasProperty(emisProp))
                {
                    t.emisId = Shader.PropertyToID(emisProp);
                    t.origEmis = mat.GetColor(emisProp);
                    t.curEmis = t.origEmis;
                }
            }
            r.GetPropertyBlock(t.mpb);
            dst.Add(t);
        }
    }

    // -------- 事件 --------
    public void SetHovered(bool v) { _hovered = v; ApplyStateSmooth(); }
    public void ToggleSelected() { _selected = !_selected; ApplyStateSmooth(); }

    // -------- 状态决策 --------
    void ApplyStateSmooth()
    {
        // 选中优先；选中时忽略悬停
        Color bgTarget = _selected ? bg_Selected : (_hovered ? bg_Hover : default);
        Color txTarget = _selected ? text_Selected : (_hovered ? text_Hover : default);

        bool toDefaultBG = !_selected && !_hovered;
        bool toDefaultTX = !_selected && !_hovered;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(LerpTo(bgTarget, txTarget, toDefaultBG, toDefaultTX));
    }

    IEnumerator LerpTo(Color bgTarget, Color txTarget, bool clearBG, bool clearTX)
    {
        float dur = Mathf.Max(0f, transitionSeconds);
        if (!useCurve && dur <= 0f)
        {
            ApplyImmediate(bgTarget, txTarget, clearBG, clearTX);
            yield break;
        }

        // 起点
        var bgStartBase = new Color[_bg.Count];
        var bgStartEmis = new Color[_bg.Count];
        for (int i = 0; i < _bg.Count; i++) { bgStartBase[i] = _bg[i].curBase; bgStartEmis[i] = _bg[i].curEmis; }

        var txStartBase = new Color[_tx.Count];
        for (int i = 0; i < _tx.Count; i++) txStartBase[i] = _tx[i].curBase;

        // 目标：默认→回原色；否则→给定目标
        var bgEndBase = new Color[_bg.Count];
        var bgEndEmis = new Color[_bg.Count];
        for (int i = 0; i < _bg.Count; i++)
        {
            bgEndBase[i] = clearBG ? _bg[i].origBase : bgTarget;
            bgEndEmis[i] = clearBG ? _bg[i].origEmis : bgTarget;
        }
        var txEndBase = new Color[_tx.Count];
        for (int i = 0; i < _tx.Count; i++)
        {
            txEndBase[i] = clearTX ? _tx[i].origBase : txTarget;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            float k = Mathf.Clamp01(t);
            if (useCurve) k = curve.Evaluate(k);

            for (int i = 0; i < _bg.Count; i++)
            {
                var e = _bg[i];
                var cb = Color.LerpUnclamped(bgStartBase[i], bgEndBase[i], k);
                var ce = Color.LerpUnclamped(bgStartEmis[i], bgEndEmis[i], k);

                if (e.baseId != -1) e.mpb.SetColor(e.baseId, cb);
                if (e.emisId != -1) e.mpb.SetColor(e.emisId, ce);
                e.r.SetPropertyBlock(e.mpb);

                e.curBase = cb; e.curEmis = ce; _bg[i] = e;
            }
            for (int i = 0; i < _tx.Count; i++)
            {
                var e = _tx[i];
                var cb = Color.LerpUnclamped(txStartBase[i], txEndBase[i], k);

                if (e.baseId != -1) e.mpb.SetColor(e.baseId, cb);
                e.r.SetPropertyBlock(e.mpb);

                e.curBase = cb; _tx[i] = e;
            }
            yield return null;
        }

        // 到达默认：清空 MPB 以完全还原材质（不 override）
        if (clearBG) for (int i = 0; i < _bg.Count; i++) { var e = _bg[i]; e.mpb.Clear(); e.r.SetPropertyBlock(e.mpb); _bg[i] = e; }
        if (clearTX) for (int i = 0; i < _tx.Count; i++) { var e = _tx[i]; e.mpb.Clear(); e.r.SetPropertyBlock(e.mpb); _tx[i] = e; }
    }

    void ApplyImmediate(Color bgTarget, Color txTarget, bool clearBG, bool clearTX)
    {
        if (clearBG)
        {
            for (int i = 0; i < _bg.Count; i++) { var e = _bg[i]; e.mpb.Clear(); e.r.SetPropertyBlock(e.mpb); e.curBase = e.origBase; e.curEmis = e.origEmis; _bg[i] = e; }
        }
        else
        {
            for (int i = 0; i < _bg.Count; i++)
            {
                var e = _bg[i];
                if (e.baseId != -1) e.mpb.SetColor(e.baseId, bgTarget);
                if (e.emisId != -1) e.mpb.SetColor(e.emisId, bgTarget);
                e.r.SetPropertyBlock(e.mpb);
                e.curBase = bgTarget; e.curEmis = bgTarget; _bg[i] = e;
            }
        }

        if (clearTX)
        {
            for (int i = 0; i < _tx.Count; i++) { var e = _tx[i]; e.mpb.Clear(); e.r.SetPropertyBlock(e.mpb); e.curBase = e.origBase; _tx[i] = e; }
        }
        else
        {
            for (int i = 0; i < _tx.Count; i++)
            {
                var e = _tx[i];
                if (e.baseId != -1) e.mpb.SetColor(e.baseId, txTarget);
                e.r.SetPropertyBlock(e.mpb);
                e.curBase = txTarget; _tx[i] = e;
            }
        }
    }
}
