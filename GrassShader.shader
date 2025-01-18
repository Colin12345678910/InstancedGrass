// SimpleInstancedGrass
//  Colin D. 
//  Jan 17th, 2025
//  This is a rather simple Shader, it's intentionally left unlit as GPU time starts to become precious when dealing with overdraw.
//  It does also handle the sway and calculations needed to manage pushing the grass around.
Shader "Custom/SimpleInstancedGrass"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _GrassID ("GrassID", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Math related to pushing Grass around, you want to additively use this message for each POI
            float4 PushGrass(float3 worldPos, float3 player)
            {
                float cameraDist = distance(worldPos, player);
                float4 dir = float4(0,0,0,0);
                if (cameraDist < 10.0f)
                {
                    worldPos.y = 0;
                    player.y = 0;
                    dir = float4(worldPos.xyz - player.xyz, 0);
                    dir = normalize(dir);
                    dir = mul(dir, unity_ObjectToWorld);
                }
                return dir * 10.0f / cameraDist;
            }
            // Mathmatical rand function, pretty simple, based on the Book of Shaders impl.
            float random(float2 p)
            {
            return frac(sin(dot(p, float2(1.0,113.0)))*43758.5453123);
            }
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 objectPos : TEXCOORD0;
                float2 rand : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(int, _GrassID)
            UNITY_INSTANCING_BUFFER_END(Props)
            float4 _Color;
            float4 _Player0;
            float4 _Player1;

            // This vert Function could maybe be moved to a seperate hlsl file.
            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1));
                float ourID = UNITY_ACCESS_INSTANCED_PROP(Props, _GrassID);

                float rand1 = (random(float2(ourID, 1345.0f)));
                float rand2 = (random(float2(2361.0f, ourID)));

                float4 dir = PushGrass(worldPos, _Player0);
                dir += PushGrass(worldPos, _Player1);

                v.vertex *= 0.5f;
                v.vertex.y *= 4.0f;
                if (v.vertex.y < 1.0f)
                {
                }
                else
                {
                    v.vertex.y *= max(random(float2(rand1, rand2)) * 5.0f, 1.0);
                    v.vertex.x += (_SinTime.w + rand2) * rand1 * 1.0;
                    v.vertex.z += (_SinTime.w - rand1) * -rand2 * 1.0;
                    v.vertex.y += 2;
                    v.vertex.xz += (dir.xz);
                }
                
                v.vertex.y += rand1 + rand2;

                o.objectPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.rand = float2(rand1, rand2);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float col = pow(i.objectPos.y * 0.2f, i.rand.x + i.rand.y * 0.5f);
                return _Color * float4(col, col, col, 1.0f);
            }
            ENDCG
        }
    }
}