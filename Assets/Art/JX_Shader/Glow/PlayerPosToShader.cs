using System.Linq;
using UnityEngine;

[ExecuteAlways]   
public class PushPlayerPosWS : MonoBehaviour
{
    
    public Transform player;

    
    public Renderer[] targets;

    static readonly int PlayerPosWS_ID = Shader.PropertyToID("_PlayerPosWS");
    MaterialPropertyBlock mpb;

    void OnEnable()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (mpb == null) mpb = new MaterialPropertyBlock();

        if (targets == null || targets.Length == 0)
        {
            targets = FindObjectsOfType<Renderer>(true)
                .Where(r => r.sharedMaterial && r.sharedMaterial.HasProperty(PlayerPosWS_ID))
                .ToArray();
        }

        Push();
    }

    void Update() => Push();

    void Push()
    {
        if (!player || targets == null) return;

        Vector3 p = player.position;
        Vector4 v = new Vector4(p.x, p.y, p.z, 1f);   

        foreach (var r in targets)
        {
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetVector(PlayerPosWS_ID, v);
            r.SetPropertyBlock(mpb);
        }
    }

    
    void OnDrawGizmos()
    {
        if (!player) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, 0.25f);
    }
}
