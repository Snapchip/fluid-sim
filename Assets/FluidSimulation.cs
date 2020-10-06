using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    private RenderTexture In;
    private RenderTexture Out;
    private RenderTexture DrawIn;
    private RenderTexture DrawOut;

    private int nthreads = 8;
    private Vector2Int threadresolution => (resolution / nthreads);
    private int stepKernel;

    public Vector2Int resolution = new Vector2Int(512,512);
    [Range(0, 50)] public int stepsPerFrame = 8;
    [Range(.999f, 1)] public float decay = .999f;
    public ComputeShader Compute;
    public Material OutputMaterial;
    public List<Jet> jets;

    void OnEnable() => Reset();
    private void OnDisable() => Cleanup();   

    private void Reset()
    {
        Cleanup();
        In = CreateTexture(RenderTextureFormat.ARGBHalf);
        Out = CreateTexture(RenderTextureFormat.ARGBHalf);
        DrawIn = CreateTexture(RenderTextureFormat.ARGBHalf);
        DrawOut = CreateTexture(RenderTextureFormat.ARGBHalf);

        stepKernel = Compute.FindKernel("StepKernel");
        Compute.SetVector("resolution", new Vector2(resolution.x, resolution.y));
        Compute.SetFloat("decay", decay);        
    }

    void Update()
    {
        for (int i = 0; i < stepsPerFrame; i++) 
            Step();
    }

    void Step()
    {
        ComputeBuffer cb = new ComputeBuffer(jets.Count, UnsafeUtility.SizeOf<Jet>()) { name = "Jets" };
        cb.SetData(jets);
        Compute.SetInt("njets", jets.Count);
        Compute.SetBuffer(stepKernel, "jets", cb);
        
        Compute.SetTexture(stepKernel, "In", In);
        Compute.SetTexture(stepKernel, "Out", Out);
        Compute.SetTexture(stepKernel, "DrawIn", DrawIn);
        Compute.SetTexture(stepKernel, "DrawOut", DrawOut);

        Compute.Dispatch(stepKernel, threadresolution.x, threadresolution.y, 1);

        OutputMaterial.SetTexture("_MainTex", DrawOut);
        
        SwapTex(ref In, ref Out);
        SwapTex(ref DrawIn, ref DrawOut);
        cb.Release();
    }

    private RenderTexture CreateTexture(RenderTextureFormat format)
    {
        RenderTexture tex = new RenderTexture(resolution.x, resolution.y, 0, format);
        
        tex.enableRandomWrite = true;
        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.useMipMap = false;
        tex.Create();

        return tex;
    }
    void Cleanup()
    {
        if (In) In.Release();
        if (Out) Out.Release();
        if (DrawIn) DrawIn.Release();
        if (DrawOut) DrawOut.Release();
    }

    void SwapTex(ref RenderTexture In, ref RenderTexture Out)
    {
        RenderTexture tmp = In;
        In = Out;
        Out = tmp;
    }    
}
[System.Serializable]
public struct Jet
{
    public Vector2 position;
    public Vector2 velocity;
    public Color color;
    public float size;
};