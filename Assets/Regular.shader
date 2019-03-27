// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Regular" {
 
    SubShader {
 
        Pass
        {
            Tags {"Queue"="Geometry+2" }
            Cull Front
            //ZWrite On
            ColorMask 0
         
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
                return half4(0,1,0,1);
            }
 
            ENDCG
     
        }
        Pass
        {
            Tags {"Queue"="Geometry+3" }
            cull back
            //ztest Equal
            ColorMask 0
         
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
                return half4(0,0,1,1);
            }
 
            ENDCG
     
        }
    }
    FallBack "Diffuse"
}