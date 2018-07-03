#version 330 core

out vec4 outColor;
in vec3 localPos;

uniform samplerCube envMap;
uniform float roughness;

const float PI = 3.14159265359;

const uint SAMPLE_COUNT = 4096u;
const float resolution = 512.0; 
const float saT_inv  = 1.0 / (4.0 * PI / (6.0 * resolution * resolution));

float RadicalInverse_VdC(uint bits) 
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10; // / 0x100000000
}
vec2 Hammersley(uint i, uint N)
{
    return vec2(float(i)/float(N), RadicalInverse_VdC(i));
}  

vec3 SampleGGX(vec2 Xi, vec3 N, float roughness)
{
    float a = roughness*roughness;
    
    float p = 2.0 * PI * Xi.x;
    float ct = sqrt((1.0 - Xi.y) / (1.0 + (a*a - 1.0) * Xi.y));
    float st = sqrt(1.0 - ct*ct);
    
    vec3 CT = vec3( vec2(cos(p), sin(p)) * st, ct );
    
    vec3 U  = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 T  = normalize(cross(U, N));
    
    return normalize(  T * CT.x + cross(N, T) * CT.y + N * CT.z );
}  
float DistributeGGX(float NdotH, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;

    float denom = (NdotH*NdotH * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return a2 / (denom + 0.00001);
}
  
void main()
{       
    vec3 N = normalize(localPos);    

    float weight = 0.0;   
    vec3 pfColor = vec3(0.0);     
    for(uint i = 0u; i < SAMPLE_COUNT; ++i)
    {
        vec2 Xi = Hammersley(i, SAMPLE_COUNT);
        vec3 H  = SampleGGX(Xi, N, roughness);
        vec3 L  = normalize(2.0 * dot(N, H) * H - N);

        float NdotL = dot(N, L);
        if(NdotL <= 0.0) continue;

        float NdotH = max(dot(N, H), 0.0);
        float HdotV = NdotH;

        float D   = DistributeGGX(NdotH, roughness);
        float pdf = D * NdotH / (4.0 * HdotV); 

        float saS = 1.0 / (float(SAMPLE_COUNT) * pdf + 0.0001);

        float mipLevel = roughness == 0.0 ? 0.0 : 0.5 * log2(saS * saT_inv); 

        pfColor += textureLod(envMap, L, mipLevel).rgb * NdotL;
        weight  += NdotL;
    }

    outColor = vec4(pfColor / weight, 1.0);
}  
