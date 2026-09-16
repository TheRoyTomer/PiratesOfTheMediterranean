#ifndef KWS_COMMON_HELPERS
#define KWS_COMMON_HELPERS


float3 KWS_ParticlesPos; //particle system transform world position
float3 KWS_ParticlesScale; //particle system transform localScale

float2 KWS_PointOnUnitCircle(float angle)
{
	return float2(cos(angle), sin(angle));
}
            

inline float3 TileWarpParticlesOffsetXZ(float3 vertex, float3 center)
{
	float3 halfScale = KWS_ParticlesScale * 0.5;
	float3 quadOffset = vertex.xyz - center.xyz;
	vertex.xz = frac((center.xz + halfScale.xz - KWS_ParticlesPos.xz) / KWS_ParticlesScale.xz) * KWS_ParticlesScale.xz; //aabb warp
	vertex.xz += KWS_ParticlesPos.xz + quadOffset.xz - halfScale.xz; //ofset relative to pivot and size
	return vertex;
}


float4 GaussianBlur5x5(Texture2D tex, SamplerState state, float2 uv, float2 texelSize)
{
	int kernelRadius = 2;
	float kernel[5] = { 1.0, 4.0, 6.0, 4.0, 1.0 };
	float4 acc = 0;
	float sum = 0;

	for (int y = -kernelRadius; y <= kernelRadius; y++)
	{
		for (int x = -kernelRadius; x <= kernelRadius; x++)
		{
			float w = kernel[abs(x)] * kernel[abs(y)];
			float2 sampleUV = uv + float2(x, y) * texelSize;
			float4 val = tex.SampleLevel(state, sampleUV, 0);

			acc += val * w;
			sum += w;
		}
	}

	return acc / sum;
}

float4 GaussianBlur3x3(Texture2D tex, SamplerState state, float2 uv, float2 texelSize)
{
	int kernelRadius = 1;
	float kernel[3] = { 1.0, 2.0, 1.0 };
	float4 acc = 0;
	float sum = 0;

	for (int y = -kernelRadius; y <= kernelRadius; y++)
	{
		for (int x = -kernelRadius; x <= kernelRadius; x++)
		{
			float w = kernel[abs(x)] * kernel[abs(y)];
			float2 sampleUV = uv + float2(x, y) * texelSize;
			float4 val = tex.SampleLevel(state, sampleUV, 0);

			acc += val * w;
			sum += w;
		}
	}

	return acc / sum;
}

float2 SpriteUV(float Width, float Height, float time, float2 uv)
{
	float2 size = float2(1.0f / Width, 1.0f / Height);
	uint totalFrames = Width * Height;

	uint index = time;
	
	uint indexX = index % Width;
	uint indexY = floor((index % totalFrames) / Width);

	float2 offset = float2(size.x * indexX, -size.y * indexY);
	float2 newUV = frac(uv) * size;
	
	newUV.y = newUV.y + size.y * (Height - 1);
	return newUV + offset;
}

float KWS_Noise13(float3 p3)
{
	p3 = frac(p3 * .1031);
	p3 += dot(p3, p3.zyx + 31.32);
	return frac((p3.x + p3.y) * p3.z);
}

float KWS_InterleavedGradientNoise(float2 pixel, float frame)
{
	frame = frame % 60;
	pixel += (frame * 5.588238f);
	return frac(52.9829189f * frac(0.06711056f * pixel.x + 0.00583715f * pixel.y));
}

float KWS_InterleavedGradientNoiseStatic(float2 pixel)
{
	return frac(52.9829189f * frac(0.06711056f * pixel.x + 0.00583715f * pixel.y));
}

float2 SafeNormalize2(float2 v)
{
	float len = length(v);
	return len > 1e-5 ? v / len : float2(0, 1);
}

inline float3 KWS_BlendNormals(float3 n1, float3 n2)
{
	return normalize(float3(n1.x + n2.x, n1.y * n2.y, n1.z + n2.z));
}

inline float3 KWS_BlendNormals(float3 n1, float3 n2, float3 n3)
{
	return normalize(float3(n1.x + n2.x + n3.x, n1.y * n2.y * n3.y, n1.z + n2.z + n3.z));
}

inline float3 KWS_BlendNormals(float3 n1, float3 n2, float3 n3, float3 n4)
{
	return normalize(float3(n1.x + n2.x + n3.x + n4.x, n1.y * n2.y * n3.y * n4.y, n1.z + n2.z + n3.z + n4.z));
}

inline float2 KWS_BlendNormalsXZ(float2 n1, float2 n2, float2 n3, float2 n4)
{
	return normalize(float3(n1.x + n2.x + n3.x + n4.x, 1, n1.y + n2.y + n3.y + n4.y)).xz;
}

inline half3 KWS_GetDerivativeNormal(float3 pos, float rojectionParamsX)
{
	return normalize(cross(ddx(pos), ddy(pos) * rojectionParamsX));
}

inline float3 DecodeNormalXZ(float2 enc)
{
	float2 nXZ = enc * 2.0 - 1.0  + (1.0 / 255.0);
	float y = sqrt(saturate(1 - dot(nXZ, nXZ)));
	return float3(nXZ.x, y, nXZ.y);
}

inline float KWS_Pow2(float x)
{
	return x * x;
}

inline float KWS_Pow3(float x)
{
	return x * x * x;
}

inline float KWS_Pow5(float x)
{
	float val = x * x;
	return val * val * x;
}

inline float KWS_Pow10(float x)
{
	float val = x * x * x;
	return val * val * x;
}

inline float KWS_Pow20(float x)
{
	float x2 = x * x;       // x^2
	float x4 = x2 * x2;     // x^4
	float x5 = x4 * x;      // x^5
	float x10 = x5 * x5;    // x^10
	return x10 * x10;       // x^20
}


inline float2 KWS_Pow2(float2 x)
{
	return x * x;
}

inline float2 KWS_Pow3(float2 x)
{
	return x * x * x;
}

inline float2 KWS_Pow5(float2 x)
{
	float2 val = x * x;
	return val * val * x;
}

inline float2 KWS_Pow10(float2 x)
{
	float2 val = x * x * x;
	return val * val * x;
}

inline float2 KWS_Pow20(float2 x)
{
	float2 x2 = x * x;       // x^2
	float2 x4 = x2 * x2;     // x^4
	float2 x5 = x4 * x;      // x^5
	float2 x10 = x5 * x5;    // x^10
	return x10 * x10;       // x^20
}




inline float3 KWS_Pow2(float3 x)
{
	return x * x;
}

inline float3 KWS_Pow3(float3 x)
{
	return x * x * x;
}

inline float3 KWS_Pow5(float3 x)
{
	float3 val = x * x;
	return val * val * x;
}

inline float3 KWS_Pow10(float3 x)
{
	float3 val = x * x * x;
	return val * val * x;
}

inline float3 KWS_Pow20(float3 x)
{
	float3 x2 = x * x;       // x^2
	float3 x4 = x2 * x2;     // x^4
	float3 x5 = x4 * x;      // x^5
	float3 x10 = x5 * x5;    // x^10
	return x10 * x10;       // x^20
}



inline float4 KWS_Pow2(float4 x)
{
	return x * x;
}

inline float4 KWS_Pow3(float4 x)
{
	return x * x * x;
}

inline float4 KWS_Pow5(float4 x)
{
	float4 val = x * x;
	return val * val * x;
}

inline float4 KWS_Pow10(float4 x)
{
	float4 val = x * x * x;
	return val * val * x;
}

inline float4 KWS_Pow20(float4 x)
{
	float4 x2 = x * x;       // x^2
	float4 x4 = x2 * x2;     // x^4
	float4 x5 = x4 * x;      // x^5
	float4 x10 = x5 * x5;    // x^10
	return x10 * x10;       // x^20
}



float KWS_MAX(float2 v)
{
	return max(v.x, v.y);
}

float KWS_MAX(float3 v)
{
	return max(max(v.x, v.y), v.z);
}

float KWS_MAX(float4 v)
{
	return max(max(v.x, v.y), max(v.z, v.w));
}

float KWS_MIN(float2 v)
{
	return min(v.x, v.y);
}

float KWS_MIN(float3 v)
{
	return min(min(v.x, v.y), v.z);
}

float KWS_MIN(float4 v)
{
	return min(min(v.x, v.y), min(v.z, v.w));
}

float2 GetRTHandleUV(float2 UV, float2 texelSize, float numberOfTexels, float2 scale)
{
	float2 maxCoord = 1.0f - numberOfTexels * texelSize;
	return min(UV, maxCoord) * scale;
}

float2 GetRTHandleUVBilinear(float2 UV, float4 texelSize, float numberOfTexels, float2 scale)
{
	float2 maxCoord = 1.0f - numberOfTexels * texelSize.xy;
	UV = min(UV, maxCoord);
	return floor(UV * scale * texelSize.zw) * texelSize.xy;
}



// texture sample



struct StochasticData
{
	float3 v0;   // vertex 0 (xy = id, z = 0)
	float3 v1;   // vertex 1
	float3 v2;   // vertex 2
	float3 w;    // bary weights (xyz)
};

float2 KWS_hash2D2D (float2 s)
{
	return frac(sin(fmod(float2(dot(s, float2(127.1,311.7)), dot(s, float2(269.5,183.3))), 3.14159))*43758.5453);
}


StochasticData  GetStochasticData(float2 uv)
{
	StochasticData d;
 
	//uv transformed into triangular grid space with UV scaled by approximation of 2*sqrt(3)
	float2 skewUV = mul(float2x2 (1.0 , 0.0 , -0.57735027 , 1.15470054), uv * 3.464);
 
	float2 vxID  = floor(skewUV);
	float3 barry = float3(frac(skewUV), 0.0);
	barry.z = 1.0 - barry.x - barry.y;

	if (barry.z > 0.0)
	{
		d.v0 = float3(vxID, 0.0);
		d.v1 = float3(vxID + float2(0, 1), 0.0);
		d.v2 = float3(vxID + float2(1, 0), 0.0);
		d.w  = barry.zyx; // (w0, w1, w2)
	}
	else
	{
		d.v0 = float3(vxID + float2(1, 1), 0.0);
		d.v1 = float3(vxID + float2(1, 0), 0.0);
		d.v2 = float3(vxID + float2(0, 1), 0.0);
		d.w  = float3(-barry.z, 1.0 - barry.y, 1.0 - barry.x);
	}

	return d;
}

float4 Tex2DArrayStochastic(Texture2DArray texArr, SamplerState smp, float2 uv, float2 dx, float2 dy, int layer)
{
	StochasticData d = GetStochasticData(uv);

	float4 s0 = texArr.SampleGrad(smp, float3(uv + KWS_hash2D2D(d.v0.xy), layer), dx, dy);
	float4 s1 = texArr.SampleGrad(smp, float3(uv + KWS_hash2D2D(d.v1.xy), layer), dx, dy);
	float4 s2 = texArr.SampleGrad(smp, float3(uv + KWS_hash2D2D(d.v2.xy), layer), dx, dy);

	return s0 * d.w.x + s1 * d.w.y + s2 * d.w.z;
}


float4 Tex2DStochastic(Texture2D texArr, SamplerState smp, float2 uv, float2 dx, float2 dy)
{
	StochasticData d = GetStochasticData(uv);

	float4 s0 = texArr.SampleGrad(smp, uv + KWS_hash2D2D(d.v0.xy), dx, dy);
	float4 s1 = texArr.SampleGrad(smp, uv + KWS_hash2D2D(d.v1.xy), dx, dy);
	float4 s2 = texArr.SampleGrad(smp, uv + KWS_hash2D2D(d.v2.xy), dx, dy);

	return s0 * d.w.x + s1 * d.w.y + s2 * d.w.z;
}



inline float4 Texture2DSampleAA(Texture2D tex, SamplerState state, float2 uv)
{
	half4 color = tex.Sample(state, uv.xy);

	float2 uv_dx = ddx(uv);
	float2 uv_dy = ddy(uv);

	color += tex.Sample(state, uv.xy + (0.25) * uv_dx + (0.75) * uv_dy);
	color += tex.Sample(state, uv.xy + (-0.25) * uv_dx + (-0.75) * uv_dy);
	color += tex.Sample(state, uv.xy + (-0.75) * uv_dx + (0.25) * uv_dy);
	color += tex.Sample(state, uv.xy + (0.75) * uv_dx + (-0.25) * uv_dy);

	color /= 5.0;

	return color;
}

float4 cubic(float v)
{
	float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
	float4 s = n * n * n;
	float x = s.x;
	float y = s.y - 4.0 * s.x;
	float z = s.z - 4.0 * s.y + 6.0 * s.x;
	float w = 6.0 - x - y - z;
	return float4(x, y, z, w) * (1.0 / 6.0);
}


inline float4 Texture2DArraySampleBicubic(Texture2DArray tex, SamplerState state, float2 uv, float4 texelSize, float idx)
{
	uv = uv * texelSize.zw - 0.5;
	float2 fxy = frac(uv);
	uv -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	half4 sample0 = tex.Sample(state, float3(offset.xz, idx));
	half4 sample1 = tex.Sample(state, float3(offset.yz, idx));
	half4 sample2 = tex.Sample(state, float3(offset.xw, idx));
	half4 sample3 = tex.Sample(state, float3(offset.yw, idx));

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline float4 Texture2DArraySampleLevelBicubic(Texture2DArray tex, SamplerState state, float2 uv, float4 texelSize, float idx, float level)
{
	uv.xy = uv.xy * texelSize.zw - 0.5;
	float2 fxy = frac(uv.xy);
	uv.xy -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	float4 sample0 = tex.SampleLevel(state, float3(offset.xz, idx), level);
	float4 sample1 = tex.SampleLevel(state, float3(offset.yz, idx), level);
	float4 sample2 = tex.SampleLevel(state, float3(offset.xw, idx), level);
	float4 sample3 = tex.SampleLevel(state, float3(offset.yw, idx), level);

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline float3 Texture2DArraySampleLevelBicubic(Texture2DArray<float3> tex, SamplerState state, float2 uv, float4 texelSize, float idx, float level)
{
	uv.xy = uv.xy * texelSize.zw - 0.5;
	float2 fxy = frac(uv.xy);
	uv.xy -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	float3 sample0 = tex.SampleLevel(state, float3(offset.xz, idx), level).xyz;
	float3 sample1 = tex.SampleLevel(state, float3(offset.yz, idx), level).xyz;
	float3 sample2 = tex.SampleLevel(state, float3(offset.xw, idx), level).xyz;
	float3 sample3 = tex.SampleLevel(state, float3(offset.yw, idx), level).xyz;

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline float4 Texture2DSampleLevelBicubic(Texture2D tex, SamplerState state, float2 uv, float4 texelSize, float level)
{
	uv = uv * texelSize.zw - 0.5;
	float2 fxy = frac(uv);
	uv -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	half4 sample0 = tex.SampleLevel(state, offset.xz, level);
	half4 sample1 = tex.SampleLevel(state, offset.yz, level);
	half4 sample2 = tex.SampleLevel(state, offset.xw, level);
	half4 sample3 = tex.SampleLevel(state, offset.yw, level);

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline float4 Texture2DSampleBicubic(Texture2D tex, SamplerState state, float2 uv, float4 texelSize)
{
	uv = uv * texelSize.zw - 0.5;
	float2 fxy = frac(uv);
	uv -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	half4 sample0 = tex.Sample(state, offset.xz);
	half4 sample1 = tex.Sample(state, offset.yz);
	half4 sample2 = tex.Sample(state, offset.xw);
	half4 sample3 = tex.Sample(state, offset.yw);

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline float4 Texture2DSampleBilinear(Texture2D tex, SamplerState state, float2 uv, float4 texelSize)
{
	uv = uv * texelSize.zw + 0.5;
	float2 iuv = floor(uv);
	float2 fuv = frac(uv);
	uv = iuv + fuv * fuv * (3.0 - 2.0 * fuv); // fuv*fuv*fuv*(fuv*(fuv*6.0-15.0)+10.0);;
	uv = (uv - 0.5) * texelSize.xy;
	return tex.Sample(state, uv);
}

inline float4 Texture2DSampleLevelBilinear(Texture2D tex, SamplerState state, float2 uv, float4 texelSize, float level)
{
	uv = uv * texelSize.zw + 0.5;
	float2 iuv = floor(uv);
	float2 fuv = frac(uv);
	uv = iuv + fuv * fuv * (3.0 - 2.0 * fuv); // fuv*fuv*fuv*(fuv*(fuv*6.0-15.0)+10.0);;
	uv = (uv - 0.5) * texelSize.xy;
	return tex.SampleLevel(state, uv, level);
}

float4 Texture2DArraySampleFlowmapJump(Texture2DArray tex, SamplerState state, float2 uv, float slice, float2 direction, float time, float scale)
{
	float2 d = sin(uv * 3.1415);
	time -= (d.x + d.y) * 0.25 + 0.5;
	
	float progressA = frac(time) - 0.5;
	float progressB = frac(time + 0.5) - 0.5;

	float2 jump = float2(0.248, 0.201);
	float2 offsetA = (time - progressA) * jump;
	float2 offsetB = (time - progressB) * jump + 0.5;

	float4 colorA = tex.Sample(state, float3((uv - progressA * direction + offsetA) * scale, slice));
	float4 colorB = tex.Sample(state, float3((uv - progressB * direction + offsetB) * scale, slice));

	
	float weight = saturate(abs(progressA * 2.0));
	//float weight = smoothstep(0.0, 1.0, abs(progressA * 2));
	//weight = weight * weight * weight * (weight * (weight * 6.0 - 15.0) + 10.0);
	
	return lerp(colorA, colorB, weight);
}

float4 Texture2DSampleFlowmapJump(Texture2D tex, SamplerState state, float2 uv, float2 direction, float time, float scale)
{
	float2 d = sin(uv * 3.1415);
	time -= (d.x + d.y) * 0.25 + 0.5;
	
	float progressA = frac(time) - 0.5;
	float progressB = frac(time + 0.5) - 0.5;

	float2 jump = float2(0.248, 0.201);
	float2 offsetA = (time - progressA) * jump;
	float2 offsetB = (time - progressB) * jump + 0.5;

	float4 colorA = tex.Sample(state, (uv - progressA * direction + offsetA) * scale);
	float4 colorB = tex.Sample(state, (uv - progressB * direction + offsetB) * scale);

	
	float weight = saturate(abs(progressA * 2.0));
	//float weight = smoothstep(0.0, 1.0, abs(progressA * 2));
	//weight = weight * weight * weight * (weight * (weight * 6.0 - 15.0) + 10.0);
	
	
	float EmpiricallyPatternFixVal = 1.06;
	float flowLerpFix = lerp(EmpiricallyPatternFixVal, 1,  abs(weight * 2 - 1)); 

	return lerp(colorA, colorB, weight) * 1;
}

float4 Texture2DSampleFlowmapJumpStochastic(Texture2D tex, SamplerState state, float2 uv, float2 direction, float time, float scale)
{
	float2 d = sin(uv * 3.1415);
	time -= (d.x + d.y) * 0.25 + 0.5;
	
	float progressA = frac(time) - 0.5;
	float progressB = frac(time + 0.5) - 0.5;

	float2 jump = float2(0.248, 0.201);
	float2 offsetA = (time - progressA) * jump;
	float2 offsetB = (time - progressB) * jump + 0.5;

	float2 gradx = ddx(uv);
	float2 grady = ddy(uv);
	float4 colorA = Tex2DStochastic(tex, state, (uv - progressA * direction + offsetA) * scale, gradx, grady);
	float4 colorB = Tex2DStochastic(tex, state, (uv - progressB * direction + offsetB) * scale, gradx, grady);
	
	float weight = saturate(abs(progressA * 2.0));
	
	float EmpiricallyPatternFixVal = 1.06;
	float flowLerpFix = lerp(EmpiricallyPatternFixVal, 1,  abs(weight * 2 - 1)); 

	return lerp(colorA, colorB, weight);
}



float4 Texture2DSampleFlowmap(Texture2D tex, SamplerState state, float2 uv, float2 direction, float time)
{
	float progressA = frac(time) - 0.5;
	float progressB = frac(time + 0.5) - 0.5;

	float4 colorA = tex.Sample(state, uv - progressA * direction);
	float4 colorB = tex.Sample(state, uv - progressB * direction);

	float weight = saturate(abs(progressA * 2.0));

	float flowLerpFix = lerp(1, 0.75, saturate(length(direction)) * abs(weight * 2 - 1));
	
	return lerp(colorA, colorB, weight) * flowLerpFix;

	//float flowSpeed = saturate(length(direction));

	//half time1 = frac(time + 0.5);
	//half time2 = frac(time);
	
	//float2 uvOffset1 = -direction * time1;
	//float2 uvOffset2 = -direction * time2;
	//float flowLerp = abs((0.5 - time1) / 0.5);
	//float flowLerpFix = lerp(1, 0.75, flowSpeed * abs(flowLerp * 2 - 1));

	//float4 colorA = tex.Sample(state, uv + uvOffset1);
	//float4 colorB = tex.Sample(state, uv + uvOffset2);
	//return lerp(colorA, colorB, flowLerp) * flowLerpFix;
}


inline bool IsOutsideUvBorders(float2 uv)
{
	uv = uv * 1.01 - 0.005;
	return any(uv != saturate(uv));
}

inline bool IsOutsideUV(float2 uv)
{
	return uv.x < 0.002 || uv.x > 0.998 || uv.y < 0.002 || uv.y > 0.998;
}

inline bool IsInsideUV(float2 uv)
{
	return !IsOutsideUV(uv);
}


inline float4 SampleTextureArray2(Texture2D tex0, Texture2D tex1, uint id, float2 uv)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return tex0.SampleLevel(sampler_linear_clamp, uv, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}

inline float4 SampleTextureArray4(Texture2D tex0, Texture2D tex1, Texture2D tex2, Texture2D tex3, uint id, float2 uv)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return tex0.SampleLevel(sampler_linear_clamp, uv, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		case 2: return tex2.SampleLevel(sampler_linear_clamp, uv, 0);
		case 3: return tex3.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}

inline float4 SampleTextureArray8(Texture2D tex0, Texture2D tex1, Texture2D tex2, Texture2D tex3, Texture2D tex4, Texture2D tex5, Texture2D tex6, Texture2D tex7, uint id, float2 uv)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return tex0.SampleLevel(sampler_linear_clamp, uv, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		case 2: return tex2.SampleLevel(sampler_linear_clamp, uv, 0);
		case 3: return tex3.SampleLevel(sampler_linear_clamp, uv, 0);
		case 4: return tex4.SampleLevel(sampler_linear_clamp, uv, 0);
		case 5: return tex5.SampleLevel(sampler_linear_clamp, uv, 0);
		case 6: return tex6.SampleLevel(sampler_linear_clamp, uv, 0);
		case 7: return tex7.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}

inline float4 SampleTextureArray2_FirstBicubic(Texture2D tex0, Texture2D tex1, uint id, float2 uv, float4 tex0_texelSize)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return Texture2DSampleLevelBicubic(tex0, sampler_linear_clamp, uv, tex0_texelSize, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}

inline float4 SampleTextureArray4_FirstBicubic(Texture2D tex0, Texture2D tex1, Texture2D tex2, Texture2D tex3, uint id, float2 uv, float4 tex0_texelSize)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return Texture2DSampleLevelBicubic(tex0, sampler_linear_clamp, uv, tex0_texelSize, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		case 2: return tex2.SampleLevel(sampler_linear_clamp, uv, 0);
		case 3: return tex3.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}

inline float4 SampleTextureArray8_FirstBicubic(Texture2D tex0, Texture2D tex1, Texture2D tex2, Texture2D tex3, Texture2D tex4, Texture2D tex5, Texture2D tex6, Texture2D tex7, uint id, float2 uv, float4 tex0_texelSize)
{
	KWS_FORCECASE switch(id)
	{
		case 0: return Texture2DSampleLevelBicubic(tex0, sampler_linear_clamp, uv, tex0_texelSize, 0);
		case 1: return tex1.SampleLevel(sampler_linear_clamp, uv, 0);
		case 2: return tex2.SampleLevel(sampler_linear_clamp, uv, 0);
		case 3: return tex3.SampleLevel(sampler_linear_clamp, uv, 0);
		case 4: return tex4.SampleLevel(sampler_linear_clamp, uv, 0);
		case 5: return tex5.SampleLevel(sampler_linear_clamp, uv, 0);
		case 6: return tex6.SampleLevel(sampler_linear_clamp, uv, 0);
		case 7: return tex7.SampleLevel(sampler_linear_clamp, uv, 0);
		default: return 0;
	}
}




//Noises


float2 SimpleNoise1_grad(int2 z)
{
	int n = z.x + z.y * 11111;
	n = (n << 13) ^ n;
	n = (n * (n * n * 15731 + 789221) + 1376312589) >> 16;
	return float2(cos(float(n)), sin(float(n)));
}

float SimpleNoise1(float2 p)
{
	int2 i = int2(floor(p));
	float2 f = frac(p);

	float2 u = f * f * (3.0 - 2.0 * f);

	return lerp(lerp(dot(SimpleNoise1_grad(i + int2(0, 0)), f - float2(0.0, 0.0)),
	dot(SimpleNoise1_grad(i + int2(1, 0)), f - float2(1.0, 0.0)), u.x),
	lerp(dot(SimpleNoise1_grad(i + int2(0, 1)), f - float2(0.0, 1.0)),
	dot(SimpleNoise1_grad(i + int2(1, 1)), f - float2(1.0, 1.0)), u.x), u.y);
}



float3 random3(float3 c)
{
	float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
	float3 r;
	r.z = frac(512.0 * j);
	j *= 0.125;
	r.x = frac(512.0 * j);
	j *= 0.125;
	r.y = frac(512.0 * j);
	return r - 0.5;
}


// 3D simplex noise
float simplex3d(float3 p)
{
	static const float F3 = 0.3333333;
	static const float G3 = 0.1666667;

	// Find current tetrahedron T and its four vertices
	float3 s = floor(p + dot(p, F3));
	float3 x = p - s + dot(s, G3);

	// Calculate i1 and i2
	float3 e = step(0, x - x.yzx);
	float3 i1 = e * (1.0 - e.zxy);
	float3 i2 = 1.0 - e.zxy * (1.0 - e);

	// Calculate x1, x2, x3
	float3 x1 = x - i1 + G3;
	float3 x2 = x - i2 + 2.0 * G3;
	float3 x3 = x - 1.0 + 3.0 * G3;

	// Find four surflets and store them in d
	float4 w;
	float4 d;

	// Calculate surflet weights
	w.x = dot(x, x);
	w.y = dot(x1, x1);
	w.z = dot(x2, x2);
	w.w = dot(x3, x3);

	// w fades from 0.6 at the center of the surflet to 0.0 at the margin
	w = max(0.6 - w, 0.0);

	// Calculate surflet components
	d.x = dot(random3(s), x);
	d.y = dot(random3(s + i1), x1);
	d.z = dot(random3(s + i2), x2);
	d.w = dot(random3(s + 1.0), x3);

	// Multiply d by w^4
	w *= w;
	w *= w;
	d *= w;

	// Return the sum of the four surflets
	return clamp(dot(d, float4(52.0, 52.0, 52.0, 52.0)), -1, 1) * 0.5 + 0.5;
}


float hash21(float2 p){
	p = frac(p*float2(123.34, 345.45));
	p += dot(p, p+34.345);
	return frac(p.x*p.y);
}

float valNoise(float2 p)
{
	float2 i = floor(p), f = frac(p);
	float a = hash21(i);
	float b = hash21(i + float2(1,0));
	float c = hash21(i + float2(0,1));
	float d = hash21(i + float2(1,1));
	float2 u = f*f*(3.0-2.0*f);
	return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
}


inline float KWS_FormulaRoughDescent01(float x)
{
	return saturate(x * 1.0575 - sin(pow(x, 100) * 0.01) * 100);
}



////////////////////////////// SDF helpers /////////////////////////////////////////////


float KWS_SDF_Box(float3 pos, float3 size)
{
	float3 d = abs(pos) - size;
	return length(max(d, 0)) + KWS_MAX(min(d, 0));
}

float KWS_SDF_Box(float3 pos, float3x3 rotationMatrix, float3 size)
{
	float3 rotatedPos = mul(rotationMatrix, pos).xyz;
	float3 d = abs(rotatedPos) - size;
	return length(max(d, 0)) + KWS_MAX(min(d, 0));
}

float KWS_SDF_Sphere(float3 p, float r)
{
	return length(p) - r;
}

inline float2  KWS_SDF_SphereIntersection(float3 pos, float3 rayDir, float3 center, float radius)
{
	float3 oc = pos - center;
	float b = dot( oc, rayDir );
	float3 qc = oc - b*rayDir;
	float h = radius*radius - dot( qc, qc );
	if( h<0.0 ) return -1; // no intersection
	h = sqrt( h );
	return float2( -b-h, -b+h );
}

//inline float2 KWS_SDF_IntersectionBox(float3 pos, float3 rayDir, float3x3 rotationMatrix, float3 size)
//{
//	float3 rotatedRayDir = mul(rotationMatrix, rayDir).xyz;
//	float3 rotatedPos = mul(rotationMatrix, pos).xyz;

//	float3 m = 1.0 / rotatedRayDir;
//	float3 n = m * rotatedPos;
//	float3 k = abs(m) * size;
	
//	float3 t1 = -n - k;
//	float3 t2 = -n + k;

//	return float2(max(max(t1.x, t1.y), t1.z),
//	min(min(t2.x, t2.y), t2.z));
//}

inline float2 KWS_SDF_IntersectionBox(float3 startPos, float3 rayDir, float2x2 rotationMatrix, float3 boxCenter, float3 boxSize)
{
	startPos -= boxCenter;
	float3 rotatedRayDir = rayDir;
	float3 rotatedPos = startPos;

	rotatedRayDir.xz = mul(rayDir.xz,  rotationMatrix);
	rotatedPos.xz = mul(startPos.xz,  rotationMatrix);

	float3 m = 1.0 / rotatedRayDir;
	float3 n = m * rotatedPos;
	float3 k = abs(m) * boxSize;
	
	float3 t1 = -n - k;
	float3 t2 = -n + k;

	return float2(max(max(t1.x, t1.y), t1.z),
	min(min(t2.x, t2.y), t2.z));
}


inline float3 KWS_SDF_IntersectionBoxWithSDF(float3 pos, float3 rayDir, float3x3 rotationMatrix, float3 size)
{
	float3 rotatedRayDir = mul(rotationMatrix, rayDir).xyz;
	float3 rotatedPos = mul(rotationMatrix, pos).xyz;

	float3 m = 1.0 / rotatedRayDir;
	float3 n = m * rotatedPos;
	float3 k = abs(m) * size;
	
	float3 t1 = -n - k;
	float3 t2 = -n + k;

	float sdf = KWS_SDF_Box(rotatedPos, size);

	return float3(max(max(t1.x, t1.y), t1.z),
	min(min(t2.x, t2.y), t2.z),
	sdf);
}

//float KWS_SDF_BoxFade(float3 worldPos, float3 boxCenter, float3x3 boxRotation, float3 boxHalfSize, float fadePercent) //  0.0 - ~0.5)

//{
//	float3 localPos = mul(boxRotation, worldPos);

//	float3 distToEdge = boxHalfSize - abs(localPos);
//	float dist = min(min(distToEdge.x, distToEdge.y), distToEdge.z);
//	float fade = smoothstep(0.0, boxHalfSize.x * fadePercent, dist);
//	return length(distToEdge) / boxHalfSize;
//}

float2 KWS_SDF_Intersection(float2 a, float2 b, out int r)
{
	if (a.x < b.x)
	{
		if (a.y < b.x) return float2(100000, -100000);
		if (a.y < b.y)
		{
			r = 1; return float2(b.x, a.y);
		}
		{
			r = 1; return b;
		}
	}
	else if (a.x < b.y)
	{
		if (a.y < b.y)
		{
			r = 0; return a;
		}
		{
			r = 0; return float2(a.x, b.y);
		}
	}
	else
	{
		return float2(100000, -100000);
	}
}

float KWS_SDF_SphereDensity(float3 ro, float3 rd, float3 sc, float sr, float dbuffer)      
{
	// normalize the problem to the canonical sphere
	float ndbuffer = dbuffer / sr;
	float3  rc = (ro - sc)/sr;
	
	// find intersection with sphere
	float b = dot(rd,rc);
	float c = dot(rc,rc) - 1.0;
	float h = b*b - c;

	// not intersecting
	if( h<0.0 ) return 0.0;
	
	h = sqrt( h );
    
	//return h*h*h;

	float t1 = -b - h;
	float t2 = -b + h;

	// not visible (behind camera or behind ndbuffer)
	if( t2<0.0 || t1>ndbuffer ) return 0.0;

	// clip integration segment from camera to ndbuffer
	t1 = max( t1, 0.0 );
	//t2 = min( t2, ndbuffer );

	// analytical integration of an inverse squared density
	float i1 = -(c*t1 + b*t1*t1 + t1*t1*t1/3.0);
	float i2 = -(c*t2 + b*t2*t2 + t2*t2*t2/3.0);
	return (i2-i1)*(3.0/4.0);
}


float KWS_SDF_SphereDensity(float3 ro, float3 rd, float3 sc, float sr, float dbuffer, out float tEntry)
{
	tEntry = 0.0;
	
	float ndbuffer = dbuffer / sr;
	float3 rc = (ro - sc) / sr;

	float b = dot(rd, rc);
	float c = dot(rc, rc) - 1.0;
	float h = b * b - c;

	float t1 = 0.0;
	float t2 = 0.0;
	float result = 0.0;

	if (h >= 0.0)
	{
		h = sqrt(h);
		t1 = -b - h;
		t2 = -b + h;

		if (t2 > 0.0 && t1 < ndbuffer)
		{
			t1 = max(t1, 0.0);
			// t2 = min(t2, ndbuffer); 

			float i1 = -(c * t1 + b * t1 * t1 + t1 * t1 * t1 / 3.0);
			float i2 = -(c * t2 + b * t2 * t2 + t2 * t2 * t2 / 3.0);
			float integral = (i2 - i1) * (3.0 / 4.0);

			tEntry = t1 * sr;
			result = integral / max(t2 - t1, 0.0001);
		}
	}

	return result;
}

inline float KWS_SDF_OrientedEllipsoidXZ_Density(float3 ro, float3 rd, float3 center, float3 scale, float2x2 rotationXZ, float dbuffer, out float tEntry)
{
	float3 rc = ro - center;

	float2 rcXZ = mul(rc.xz, rotationXZ) / scale.xz;
	float2 rdXZ = mul(rd.xz, rotationXZ) / scale.xz;

	float rcY = rc.y / scale.y;
	float rdY = rd.y / scale.y;

	float3 rc_local = float3(rcXZ.x, rcY, rcXZ.y);
	float3 rd_local = float3(rdXZ.x, rdY, rdXZ.y);

	rd_local = normalize(rd_local);
	float lenCorrection = 1.0;
	float ndbuffer = dbuffer;

	float b = dot(rd_local, rc_local);
	float c = dot(rc_local, rc_local) - 1.0;
	float h = b * b - c;

	if (h < 0.0)
	{
		tEntry = 0.0;
		return 0.0;
	}

	h = sqrt(h);
	float t1 = -b - h;
	float t2 = -b + h;

	if (t2 < 0.0 || t1 > ndbuffer)
	{
		tEntry = 0.0;
		return 0.0;
	}

	t1 = max(t1, 0.0);

	float i1 = -(c * t1 + b * t1 * t1 + t1 * t1 * t1 / 3.0);
	float i2 = -(c * t2 + b * t2 * t2 + t2 * t2 * t2 / 3.0);
	float integral = (i2 - i1) * (3.0 / 4.0);

	tEntry = t1 / lenCorrection;
	return integral / (t2 - t1);
}

float KWS_SDF_BoxDensity(float3 ro, float3 rd, float3 boxCenter, float3 boxHalfSize, float dbuffer)
{
	// transform ray into box local space
	float3 localRo = ro - boxCenter;

	float3 tMin = (-boxHalfSize - localRo) / rd;
	float3 tMax = (+boxHalfSize - localRo) / rd;

	float3 t1 = min(tMin, tMax);
	float3 t2 = max(tMin, tMax);

	float tNear = max(max(t1.x, t1.y), t1.z);
	float tFar  = min(min(t2.x, t2.y), t2.z);

	if (tFar < 0.0 || tNear > tFar) return 0.0;

	// clip integration to depth buffer
	tNear = max(tNear, 0.0);
	//tFar  = min(tFar, dbuffer);

	if (tNear >= tFar) return 0.0;

	float3 p1 = ro + rd * tNear;
	float3 p2 = ro + rd * tFar;

	float3 mid = (p1 + p2) * 0.5;
	float3 localMid = abs(mid - boxCenter) / boxHalfSize;

	// density falloff from center to edges (simple radial box falloff)
	float falloff = 1.0 - saturate(max(max(localMid.x, localMid.y), localMid.z));

	float visibleLength = length(p2 - p1);
	float maxLength = length(boxHalfSize) * 2.0;

	return falloff * (visibleLength / maxLength);
}

float KWS_SDF_BoxDensity(float3 ro, float3 rd, float3 boxCenter, float3 boxHalfSize, float dbuffer, float2x2 rotationMatrixXZ, int falloffType, float falloffSharpness)
{
	// Apply XZ rotation to ray origin and direction
	float2 roXZ = mul((ro.xz - boxCenter.xz), rotationMatrixXZ);
	float2 rdXZ = mul(rd.xz,  rotationMatrixXZ);
	float3 localRo = float3(roXZ.x, ro.y - boxCenter.y, roXZ.y);
	float3 localRd = float3(rdXZ.x, rd.y, rdXZ.y);

	float3 tMin = (-boxHalfSize - localRo) / localRd;
	float3 tMax = (+boxHalfSize - localRo) / localRd;

	float3 t1 = min(tMin, tMax);
	float3 t2 = max(tMin, tMax);

	float tNear = max(max(t1.x, t1.y), t1.z);
	float tFar  = min(min(t2.x, t2.y), t2.z);

	if (tFar < 0.0 || tNear > tFar) return 0.0;

	// clip to scene depth
	tNear = max(tNear, 0.0);
	//tFar  = min(tFar, dbuffer);

	if (tNear >= tFar) return 0.0;

	float3 p1 = ro + rd * tNear;
	float3 p2 = ro + rd * tFar;
	float3 mid = (p1 + p2) * 0.5;

	// Compute local space distance to box center (with rotation applied)
	float2 midXZ = mul((mid.xz - boxCenter.xz), rotationMatrixXZ);
	float3 localMid = float3(midXZ.x, mid.y - boxCenter.y, midXZ.y);
	float3 normPos = abs(localMid / boxHalfSize); // ∈ [0..1]

	float d = max(max(normPos.x, normPos.y), normPos.z); // radial-like

	float falloff = 0.0;

	// Choose falloff function
	if (falloffType == 0) falloff = 1.0 - d;                               // linear
	else if (falloffType == 1) falloff = exp(-d * d * falloffSharpness);  // exponential
	else if (falloffType == 2) falloff = pow(1.0 - d, falloffSharpness);  // power
	else if (falloffType == 3) falloff = smoothstep(1.0, 0.0, d);         // smooth

	falloff = saturate(falloff);

	float visibleLength = length(p2 - p1);
	float maxLength = length(boxHalfSize) * 2.0;

	return falloff * (visibleLength / maxLength);
}



// end filtering




/// packing

float Pack_R16G16_UNorm(float2 rg)
{
	rg = 65534.0 * saturate(rg);
	uint packedValue = uint(round(rg.x)) + 65535u * uint(round(rg.y));
	return asfloat(packedValue);
}

float2 Unpack_R16G16_UNorm(float packedValue)
{
	uint val = asuint(packedValue);
	float2 x = float2(val % 65535u, val / 65535u);
	return x / 65534.0;
}


uint Pack_R11G11B10f(float3 rgb)
{
	uint r = (f32tof16(rgb.x) << 17) & 0xFFE00000;
	uint g = (f32tof16(rgb.y) << 6) & 0x001FFC00;
	uint b = (f32tof16(rgb.z) >> 5) & 0x000003FF;
	return r | g | b;
}

float3 Unpack_R11G11B10f(uint rgb)
{
	float r = f16tof32((rgb >> 17) & 0x7FF0);
	float g = f16tof32((rgb >> 6) & 0x7FF0);
	float b = f16tof32((rgb << 5) & 0x7FE0);
	return float3(r, g, b);
}

float2 Pack_R16G16B16f(float3 d, float range)
{
	d = saturate((d / range) * 0.5 + 0.5);

	float r10 = floor(d.x * 1023.0 + 0.5); // 10 bit
	float g11 = floor(d.y * 2047.0 + 0.5); // 11 bit
	float b11 = floor(d.z * 2047.0 + 0.5); // 11 bit

	float packed0 = r10 + g11 * 1024.0; // 1024 = 2^10

	float packed1 = b11;

	return float2(
		packed0 / (2048.0 * 2048.0),  //  ~4 194 304
		packed1 / 2048.0              //  ~2 048
	);
}

float3 Unpack_R16G16B16f(float2 enc, float range)
{
	float packed0 = enc.x * (2048.0 * 2048.0);
	float packed1 = enc.y * 2048.0;

	float g11 = floor(packed0 / 1024.0);
	float r10 = packed0 - g11 * 1024.0;
	float b11 = packed1;

	float3 d;
	d.x = r10 / 1023.0;
	d.y = g11 / 2047.0;
	d.z = b11 / 2047.0;

	return (d * 2.0 - 1.0) * range;
}

float Pack_UintTo_R8_UNorm(uint x)
{
	return x / 255.0;
}

uint Unpack_R8_UNorm_ToUint(float x)
{
	return (uint)(x * 255.0 + 0.5);
}



/// end packing



#endif