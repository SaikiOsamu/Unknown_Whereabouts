using System.Collections.Generic;
using UnityEngine;

public class DisableDuringFade : MonoBehaviour
{
    public Behaviour[] targets;

    void Awake()
    {
        if (targets == null || targets.Length == 0)
        {
            var bs = GetComponents<Behaviour>();
            var list = new List<Behaviour>();
            foreach (var b in bs) if (b && !(b is DisableDuringFade)) list.Add(b);
            targets = list.ToArray();
        }
    }

    void Reset()
    {
        var bs = GetComponents<Behaviour>();
        var list = new List<Behaviour>();
        foreach (var b in bs) if (b && !(b is DisableDuringFade)) list.Add(b);
        targets = list.ToArray();
    }

    void OnEnable()
    {
        FadeManager.OnBlockChanged += HandleBlock;
        var inst = FadeManager.Instance;
        if (inst != null && inst.IsBlocked) HandleBlock(true);
    }

    void OnDisable()
    {
        FadeManager.OnBlockChanged -= HandleBlock;
    }

    void HandleBlock(bool blocked)
    {
        if (targets == null) return;
        foreach (var b in targets) if (b) b.enabled = !blocked;
    }
}
