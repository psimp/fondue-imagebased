#version 330 core

in vec3 localPos;

out vec4 FragColor;

uniform samplerCube envMap;

void main()
{       
    const float PI = 3.14159265359;

    vec3 N = normalize(localPos);

    vec3 U    = vec3(0.0, 1.0, 0.0);
    vec3 R    = cross(U, N);
    U         = cross(N, R);

    vec3 irr = vec3(0.0);  
    const float d = 0.020;
    float nsamples = 0.0; 
    for(float p = 0.0; p < 2.0 * PI; p += d)
    for(float t = 0.0; t < 0.5 * PI; t += d)
    {
        nsamples++;

        vec3 spTangent = vec3(sin(t) * cos(p),  sin(t) * sin(p), cos(t));
        vec3 spWorld = spTangent.x * R + spTangent.y * U + spTangent.z * N; 

        irr += texture(envMap, spWorld).rgb * cos(t) * sin(t);
    }

    irr = PI * irr / float(nsamples);    

    FragColor = vec4(irr, 1.0f);
}
