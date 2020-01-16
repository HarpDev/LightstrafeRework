Shader "Custom/GooShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _FlowMap ("Flow (RG, A noise)", 2D) = "black" {}
		[NoScaleOffset] _NormalMap ("Normals", 2D) = "bump" {}
		_UJump ("U jump per phase", Range(-0.25, 0.25)) = 0.25
		_VJump ("V jump per phase", Range(-0.25, 0.25)) = 0.25
		_FlowSpeed ("Flow Speed", Float) = 1
		_FlowStrength ("Flow Strength", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Distortion ("Distortion", Range(0, 3)) = 1
		_Power ("Power", Range(0, 10)) = 5
		_Scale ("Scale", Range(0, 2)) = 1
		_Attenuation ("Attenuation", Range(0, 5)) = 2
		_Ambient ("Ambient", Range(0, 5)) = 2
        _Specular ("Specular Color", Color) = (1,1,1,1)
		[HDR] _Emission ("Emission", color) = (0 ,0 ,0 , 1)

		_ScrollXSpeed ("X Scroll Speed", Range(0, 100)) = 0.1
		_ScrollYSpeed ("Y Scroll Speed", Range(0, 10)) = 0.1

        [Header(Lighting Parameters)]
        [IntRange]_StepAmount ("Shadow Steps", Range(1, 16)) = 2
        _StepWidth ("Step Size", Range(0, 1)) = 0.25
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularFalloff ("Specular Falloff", Range(0, 2)) = 1

		_RimColor ("Rim Color", Color) = (1, 1, 1, 1)
		_RimPower ("Rim Power", Range(0, 10)) = 2

		_ControlTime ("Time", float) = 0
		_ModelOrigin ("Model Origin", Vector) = (0,0,0,0)
		_ImpactOrigin ("Impact Origin", Vector) = (-5,0,0,0)
		_Frequency ("Frequency", Range(0, 1000)) = 10
		_Amplitude ("Amplitude", Range(0, 5)) = 0.1
		_WaveFalloff ("Wave Falloff", Range(1, 8)) = 4
		_MaxWaveDistortion ("Max Wave Distortion", Range(0.1, 2.0)) = 1
		_ImpactSpeed ("Impact Speed", Range(0, 10)) = 0.5
		_WaveSpeed ("Wave Speed", Range(-10, 10)) = -5
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf Standard fullforwardshadows
		#pragma surface surf Stepped fullforwardshadows alpha vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex, _FlowMap, _NormalMap;
		float _UJump, _VJump;
		float _FlowSpeed;
		float _FlowStrength;
		float4 _ScrollXSpeed;
		float4 _ScrollYSpeed;

        struct Input
        {
            float2 uv_MainTex;
			float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		half3 _Emission;
        fixed4 _Specular;
		float _Distortion;
		float _Power;
		float _Scale;
		sampler2D _LocalThickness;
		float _Attenuation;
		float _Ambient;

        float _StepWidth;
        float _StepAmount;
        float _SpecularSize;
        float _SpecularFalloff;

		fixed4 _RimColor;
		float _RimPower;

        struct ToonSurfaceOutput{
            fixed3 Albedo;
            half3 Emission;
            fixed3 Specular;
            fixed Alpha;
            fixed3 Normal;
        };

		float thickness;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
		//our lighting function. Will be called once per light
		float4 LightingStepped(ToonSurfaceOutput s, float3 lightDir, half3 viewDir, float shadowAttenuation){
			//how much does the normal point towards the light?
			float towardsLight = dot(s.Normal, lightDir);

            //stretch values so each whole value is one step
            towardsLight = towardsLight / _StepWidth;
            //make steps harder
            float lightIntensity = floor(towardsLight);

            // calculate smoothing in first pixels of the steps and add smoothing to step, raising it by one step
            // (that's fine because we used floor previously and we want everything to be the value above the floor value, 
            // for example 0 to 1 should be 1, 1 to 2 should be 2 etc...)
            float change = fwidth(towardsLight);
            float smoothing = smoothstep(0, change, frac(towardsLight));
            lightIntensity = lightIntensity + smoothing;

            // bring the light intensity back into a range where we can use it for color
            // and clamp it so it doesn't do weird stuff below 0 / above one
            lightIntensity = lightIntensity / _StepAmount;
            lightIntensity = saturate(lightIntensity);

        #ifdef USING_DIRECTIONAL_LIGHT
            //for directional lights, get a hard vut in the middle of the shadow attenuation
            float attenuationChange = fwidth(shadowAttenuation) * 0.5;
            float shadow = smoothstep(0.5 - attenuationChange, 0.5 + attenuationChange, shadowAttenuation);
        #else
            //for other light types (point, spot), put the cutoff near black, so the falloff doesn't affect the range
            float attenuationChange = fwidth(shadowAttenuation);
            float shadow = smoothstep(0, attenuationChange, shadowAttenuation);
        #endif
            lightIntensity = lightIntensity * shadow;

            //calculate how much the surface points points towards the reflection direction
            float3 reflectionDirection = reflect(lightDir, s.Normal);
            float towardsReflection = dot(viewDir, -reflectionDirection);

            //make specular highlight all off towards outside of model
            float specularFalloff = dot(viewDir, s.Normal);
            specularFalloff = pow(specularFalloff, _SpecularFalloff);
            towardsReflection = towardsReflection * specularFalloff;

            //make specular intensity with a hard corner
            float specularChange = fwidth(towardsReflection);
            float specularIntensity = smoothstep(1 - _SpecularSize, 1 - _SpecularSize + specularChange, towardsReflection);
            //factor inshadows
            specularIntensity = specularIntensity * shadow;

            float4 color;
            //calculate final color
            color.rgb = s.Albedo * lightIntensity * _RimColor.rgb;
            color.rgb = lerp(color.rgb, s.Specular * _RimColor.rgb, saturate(specularIntensity));

            color.a = s.Alpha;
 
			float3 L = lightDir;
			float3 V = viewDir;
			float3 N = s.Normal;
 
			float3 H = normalize(L + N * _Distortion);
			float VdotH = pow(saturate(dot(V, -H)), _Power) * _Scale;
			float3 I = _Attenuation * (VdotH + _Ambient) * thickness;

			color.rgb = color.rgb + _RimColor * I;

            return color;
		}

		float _ControlTime;
		float4 _ModelOrigin;
		float4 _ImpactOrigin;
    
		half _Frequency; //Base frequency for our waves.
		half _Amplitude; //Base amplitude for our waves.
		half _WaveFalloff; //How quickly our distortion should fall off given distance.
		half _MaxWaveDistortion; //Smaller number here will lead to larger distortion as the vertex approaches origin.
		half _ImpactSpeed; //How quickly our wave origin moves across the sphere.
		half _WaveSpeed; //Oscillation speed of an individual wave.

		void vert(inout appdata_full v, out Input o) {
			v.vertex.x += sign(v.vertex.x) * sin(_Time.w) / 50;
			v.vertex.y += sign(v.vertex.y) * cos(_Time.w) / 50;

			float4 world_space_vertex = mul(unity_ObjectToWorld, v.vertex);
			float4 direction = normalize(_ModelOrigin - _ImpactOrigin);
			float4 origin = _ImpactOrigin + _ControlTime * _ImpactSpeed * direction;
      
			//Get the distance in world space from our vertex to the wave origin.
			float dist = distance(world_space_vertex, origin);
      
			//Adjust our distance to be non-linear.
			dist = pow(dist, _WaveFalloff);
      
			//Set the max amount a wave can be distorted based on distance.
			dist = max(dist, _MaxWaveDistortion);
      
			//Convert direction and _ImpactOrigin to model space for later trig magic.
			float4 l_ImpactOrigin = mul(unity_WorldToObject, _ImpactOrigin);
			float4 l_direction = mul(unity_WorldToObject, direction);
      
			//Magic
			float impactAxis = l_ImpactOrigin + dot((v.vertex - l_ImpactOrigin), l_direction);
      
			v.vertex.xyz += v.normal * sin(impactAxis * _Frequency + _ControlTime * _WaveSpeed) * _Amplitude * (1 / dist);

			UNITY_INITIALIZE_OUTPUT(Input,o);
		}

		#include "Flow.cginc"
        void surf (Input i, inout ToonSurfaceOutput o)
        {
			//sample and tint albedo texture
			float2 flowVector = tex2D(_FlowMap, i.uv_MainTex).rg * 2 - 1;
			flowVector *= _FlowStrength;
			
			float noise = tex2D(_FlowMap, i.uv_MainTex).a;
			float time = _Time.y * _FlowSpeed + noise;
			float2 jump = float2(_UJump, _VJump);

			float3 uvwA = FlowUVW(i.uv_MainTex, flowVector, jump, time, false);
			float3 uvwB = FlowUVW(i.uv_MainTex, flowVector, jump, time, true);

			float3 normalA = UnpackNormal(tex2D(_NormalMap, uvwA.xy)) * uvwA.z;
			float3 normalB = UnpackNormal(tex2D(_NormalMap, uvwB.xy)) * uvwB.z;
			o.Normal = normalize(normalA + normalB);

			fixed4 texA = tex2D(_MainTex, uvwA.xy) * uvwA.z;
			fixed4 texB = tex2D(_MainTex, uvwB.xy) * uvwB.z;

			fixed4 col = (texA + texB);
			col *= _Color;
			o.Albedo = col.rgb;

            o.Specular = _Specular;

            float3 shadowColor = col.rgb;
			half rim = 1.0 - saturate(dot(normalize(i.viewDir), o.Normal));
			o.Emission = _Emission + shadowColor + _RimColor.rgb * pow(rim, _RimPower) * 4;
			o.Alpha = 0.75 + pow(rim, _RimPower) / 4;

			thickness = tex2D(_LocalThickness, i.uv_MainTex).r;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
