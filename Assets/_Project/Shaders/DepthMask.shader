Shader "Game/DepthMask"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry-10" }
        ColorMask 0
        ZWrite On
        ZTest LEqual
        Cull Back

        Pass
        {
            Name "DepthMask"
        }
    }
}
