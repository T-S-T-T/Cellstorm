Shader "Cellstorm/MetaballThreshold"
{
    Properties
    {
        _MainTex ("Field", 2D) = "black" {}
        _Threshold ("Threshold", Range(0.05, 0.95)) = 0.5
        _Softness ("Edge softness", Range(0.001, 0.2)) = 0.03
        _OutlineWidth ("Outline width", Range(0, 0.5)) = 0.12
        _OutlineColor ("Outline color", Color) = (0.05, 0.30, 0.20, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Threshold;
            float _Softness;
            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 f = tex2D(_MainTex, i.uv);
                float a = f.a;

                // Hard-edged blob from the soft accumulated field.
                float mask = smoothstep(_Threshold - _Softness, _Threshold + _Softness, a);

                // The field stores premultiplied colour, so divide alpha back out.
                fixed3 baseCol = f.rgb / max(a, 0.0001);

                // Darker rim just inside the edge.
                float rim = 1.0 - smoothstep(_Threshold, _Threshold + _OutlineWidth, a);
                fixed3 col = lerp(baseCol, _OutlineColor.rgb, rim * _OutlineColor.a);

                return fixed4(col, mask);
            }
            ENDCG
        }
    }
}
