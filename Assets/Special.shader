// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Special" {
 
    SubShader {
 
        Pass
        {
            Tags {"Queue"="Geometry" }
            Cull Front
            //ZWrite On
            ColorMask 0
            ZTest Greater
         
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
 
            struct appdata {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 pos : SV_POSITION;
            };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag(v2f i) : SV_Target {
                return half4(1,1,0,1);
            }
 
            ENDCG
     
        }
        Pass
        {
            Tags {"Queue"="Geometry+1" }
            cull Front
            ztest Equal
            ColorMask rgba
         
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
 
            struct appdata {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 pos : SV_POSITION;
            };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag(v2f i) : SV_Target {
                return half4(1,0,0,1);
            }
 
            ENDCG
     
        }
    }
    FallBack "Diffuse"
}