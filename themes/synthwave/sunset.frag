// SOURCE: https://www.shadertoy.com/view/tsScRK
// Adapted for glslViewer: aliases u_resolution/u_time/u_delta to the
// iResolution/iTime/iTimeDelta Shadertoy globals so the original
// mainImage() stays unchanged. A gl_FragColor main() at the bottom of
// this file calls mainImage(). round() is polyfilled because the
// default GLSL profile glslViewer compiles against (1.20) doesn't
// expose it.

#ifdef GL_ES
precision highp float;
#endif

uniform vec2  u_resolution;
uniform float u_time;
uniform float u_delta;

#define iResolution vec3(u_resolution, 1.0)
#define iTime       u_time
#define iTimeDelta  u_delta

// Stub iChannel0 — never actually sampled because
// disable_sound_texture_sampling turns every textureMirror() call into
// vec4(0), but declaring the uniform keeps any stray identifier valid.
uniform sampler2D iChannel0;

float round(float x) { return floor(x + 0.5); }
vec2  round(vec2  v) { return floor(v + 0.5); }
vec3  round(vec3  v) { return floor(v + 0.5); }

//#define AA 4
//#define VAPORWAVE
//#define stereo 1. // -1. for cross-eyed (defaults to parallel view)
#define speed 1.
#define wave_thing
//#define city

//you can add any sound texture in iChannel0 to turn it into a cool audio visualizer 
// (it looks better with lower speeds though)
//you should commment out or remove the following line to enable it (it's disabled mainly for performance reasons):
#define disable_sound_texture_sampling

#ifndef disable_sound_texture_sampling
    #undef speed 
    // lower value of speed when using as audio visualizer
    #define speed 5.
#endif

//self-explainatory
#define audio_vibration_amplitude .125

float jTime;


#ifdef disable_sound_texture_sampling
#define textureMirror(a, b) vec4(0)
#else
vec4 textureMirror(sampler2D tex, vec2 c){
    vec2 cf = fract(c);
    return texture(tex,mix(cf,1.-cf,mod(floor(c),2.)));
}
#endif


float amp(vec2 p){
    return smoothstep(1.,8.,abs(p.x));   
}

float pow512(float a){
    a*=a;//^2
    a*=a;//^4
    a*=a;//^8
    a*=a;//^16
    a*=a;//^32
    a*=a;//^64
    a*=a;//^128
    a*=a;//^256
    return a*a;
}
float pow1d5(float a){
    return a*sqrt(a);
}
// Period over which procedural terrain must repeat so the recorded video
// loops seamlessly: camera advances speed*LOOP_SECONDS units per loop, so
// every position-dependent function below wraps its z-coord to this.
#define TERRAIN_PERIOD (speed*${LOOP_SECONDS})

float hash21(vec2 co){
    return fract(sin(dot(co.xy,vec2(1.9898,7.233)))*45758.5433);
}
float terrainHash21(vec2 co){
    // Triangle-wave wrap in z with period TERRAIN_PERIOD: values mirror
    // around TERRAIN_PERIOD*0.5 so the camera (which advances exactly
    // TERRAIN_PERIOD units per loop) sees identical terrain at t=0 and
    // t=LOOP_SECONDS. Mirroring keeps cell-to-cell continuity (no ridge).
    float y = mod(co.y, TERRAIN_PERIOD);
    co.y = y > TERRAIN_PERIOD*0.5 ? TERRAIN_PERIOD - y : y;
    return hash21(co);
}
float hash(vec2 uv){
    float a = amp(uv);
    #ifdef wave_thing
    // Spatial freq chosen so one full ripple cycle fits TERRAIN_PERIOD,
    // keeping the wave bands seamless across the camera's per-loop travel.
    float w = a>0.?(1.-.4*pow512(.51+.49*sin((6.2831853/TERRAIN_PERIOD)*(uv.y+.5*uv.x)-(6.2831853/${LOOP_SECONDS})*jTime))):0.;
    #else
    float w=1.;
    #endif
    return (a>0.?
        a*pow1d5(
        //texture(iChannel0,uv/iChannelResolution[0].xy).r
        terrainHash21(uv)
        )*w
        :0.)-(textureMirror(iChannel0,vec2((uv.x*29.+uv.y)*.03125,1.)).x)*audio_vibration_amplitude;
}

float edgeMin(float dx,vec2 da, vec2 db,vec2 uv){
    uv.x+=5.;
    vec3 i = round(vec3(uv,uv.x+uv.y));
    // Matches terrainHash21's triangle wrap (period = TERRAIN_PERIOD).
    float y = mod(i.y, TERRAIN_PERIOD);
    i.y = y > TERRAIN_PERIOD*0.5 ? TERRAIN_PERIOD - y : y;
    i.z = i.x + i.y;
    vec3 c = fract(i*(vec3(0,1,2)+0.61803398875));
    float a1 = textureMirror(iChannel0,vec2(c.y,0.)).x>.6?.15:1.;
    float a2 = textureMirror(iChannel0,vec2(c.x,0.)).x>.6?.15:1.;
    float a3 = textureMirror(iChannel0,vec2(c.z,0.)).x>.6?.15:1.;

    return min(min((1.-dx)*db.y*a3,da.x*a2),da.y*a1);
}

vec2 trinoise(vec2 uv){
    const float sq = 1.2247448713915890; // sqrt(3./2.) precomputed: GLSL 1.20 disallows function-call const initializers.
    uv.x *= sq;
    uv.y -= .5*uv.x;
    vec2 d = fract(uv);
    uv -= d;

    bool c = dot(d,vec2(1))>1.;

    vec2 dd = 1.-d;
    vec2 da = c?dd:d,db = c?d:dd;
    
    float nn = hash(uv+float(c));
    float n2 = hash(uv+vec2(1,0));
    float n3 = hash(uv+vec2(0,1));

    
    float nmid = mix(n2,n3,d.y);
    float ns = mix(nn,c?n2:n3,da.y);
    float dx = da.x/db.y;
    return vec2(mix(ns,nmid,dx),edgeMin(dx,da, db,uv+d));
}


vec2 map(vec3 p){
    vec2 n = trinoise(p.xz);
    return vec2(p.y-2.*n.x,n.y);
}

vec3 grad(vec3 p){
    const vec2 e = vec2(.005,0);
    float a =map(p).x;
    return vec3(map(p+e.xyy).x-a
                ,map(p+e.yxy).x-a
                ,map(p+e.yyx).x-a)/e.x;
}

vec2 intersect(vec3 ro,vec3 rd){
    float d =0.,h=0.;
    for(int i = 0;i<500;i++){ //look nice with 50 iterations
        vec3 p = ro+d*rd;
        vec2 s = map(p);
        h = s.x;
        d+= h*.5;
        if(abs(h)<.003*d)
            return vec2(d,s.y);
        if(d>150.|| p.y>2.) break;
    }
    
    return vec2(-1);
}


void addsun(vec3 rd,vec3 ld,inout vec3 col){
    
	float sun = smoothstep(.21,.2,distance(rd,ld));
    
    if(sun>0.){
        float yd = (rd.y-ld.y);

        float a =sin(3.1*exp(-(yd)*14.)); 

        sun*=smoothstep(-.8,0.,a);

        col = mix(col,vec3(1.,.8,.4)*.75,sun);
    }
}


float starnoise(vec3 rd){
    float c = 0.;
    vec3 p = normalize(rd)*300.;
	for (float i=0.;i<4.;i++)
    {
        vec3 q = fract(p)-.5;
        vec3 id = floor(p);
        float c2 = smoothstep(.5,0.,length(q));
        c2 *= step(hash21(id.xz/id.y),.06-i*i*0.005);
        c += c2;
        p = p*.6+.5*p*mat3(3./5.,0,4./5.,0,1,0,-4./5.,0,3./5.);
    }
    c*=c;
    float g = dot(sin(rd*10.512),cos(rd.yzx*10.512));
    c*=smoothstep(-3.14,-.9,g)*.5+.5*smoothstep(-.3,1.,g);
    return c*c;
}

vec3 gsky(vec3 rd,vec3 ld,bool mask){
    float haze = exp2(-5.*(abs(rd.y)-.2*dot(rd,ld)));
    

    //float st = mask?pow512(texture(iChannel0,(rd.xy+vec2(300.1,100)*rd.z)*10.).r)*(1.-min(haze,1.)):0.;
    //float st = mask?pow512(hash21((rd.xy+vec2(300.1,100)*rd.z)*10.))*(1.-min(haze,1.)):0.;
    float st = mask?(starnoise(rd))*(1.-min(haze,1.)):0.;
    vec3 back = vec3(.4,.1,.7)*(1.-.5*textureMirror(iChannel0,vec2(.5+.05*rd.x/rd.y,0.)).x
    *exp2(-.1*abs(length(rd.xz)/rd.y))
    *max(sign(rd.y),0.));
    #ifdef city
    float x = round(rd.x*30.);
    float h = hash21(vec2(x-166.));
    bool building = (h*h*.125*exp2(-x*x*x*x*.0025)>rd.y);
    if(mask && building)
        back*=0.,haze=.8, mask=mask && !building;
    #endif
    vec3 col=clamp(mix(back,vec3(.7,.1,.4),haze)+st,0.,1.);
    if(mask)addsun(rd,ld,col);
    return col;  
}


void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    fragColor=vec4(0);
    #ifdef AA
    for(float x = 0.;x<1.;x+=1./float(AA)){
    for(float y = 0.;y<1.;y+=1./float(AA)){
    #else
        const float AA=1.,x=0.,y=0.;
    #endif
    vec2 uv = (2.*(fragCoord+vec2(x,y))-iResolution.xy)/iResolution.y;
    
    const float shutter_speed = .25; // for motion blur
	//float dt = fract(texture(iChannel0,float(AA)*(fragCoord+vec2(x,y))/iChannelResolution[0].xy).r+iTime)*shutter_speed;
    float dt = fract(hash21(float(AA)*(fragCoord+vec2(x,y)))+iTime)*shutter_speed;
    jTime = mod(iTime-dt*iTimeDelta,4000.);
    vec3 ro = vec3(0.,1,(-20000.+jTime*speed));
    
    #ifdef stereo
        ro+=stereo*vec3(.2*(float(uv.x>0.)-.5),0.,0.); 
        const float de = .9;
        uv.x=uv.x+.5*(uv.x>0.?-de:de);
        uv*=2.;
    #endif
        
    vec3 rd = normalize(vec3(uv,4./3.));//vec3(uv,sqrt(1.-dot(uv,uv)));
    
    vec2 i = intersect(ro,rd);
    float d = i.x;
    
    vec3 ld = normalize(vec3(0,.125+.05*sin((6.2831853/${LOOP_SECONDS})*jTime),1));

    vec3 fog = d>0.?exp2(-d*vec3(.14,.1,.28)):vec3(0.);
    vec3 sky = gsky(rd,ld,d<0.);
    
    vec3 p = ro+d*rd;
    vec3 n = normalize(grad(p));
    
    float diff = dot(n,ld)+.1*n.y;
    vec3 col = vec3(.1,.11,.18)*diff;
    
    vec3 rfd = reflect(rd,n); 
    vec3 rfcol = gsky(rfd,ld,true);
    
    col = mix(col,rfcol,.05+.95*pow(max(1.+dot(rd,n),0.),5.));
    #ifdef VAPORWAVE
        col = mix(col,vec3(.4,.5,1.),smoothstep(.05,.0,i.y));
        col = mix(sky,col,fog);
        col = sqrt(col);
    #else
        col = mix(col,vec3(.8,.1,.92),smoothstep(.05,.0,i.y));
        col = mix(sky,col,fog);
        //no gamma for that old cg look
    #endif
    if(d<0.)
        d=1e6;
    d=min(d,10.);
    fragColor += vec4(clamp(col,0.,1.),d<0.?0.:.1+exp2(-d));
    #ifdef AA
    }
    }
    fragColor/=float(AA*AA);
    #endif
}

/** SHADERDATA
{
	"title": "another synthwave sunset thing",
	"description": "I was thinking of a way to make pseudo tesselation noise and i made this to illustrate it, i might not be the first one to come up with this solution.",
	"model": "car"
}
*/

// glslViewer entry point: dispatches to the Shadertoy mainImage().
// mainImage() writes a non-1 alpha (used for fog depth), which the
// recorder treats as transparency and yields a fully black mp4, so we
// force alpha=1 here.
void main()
{
    vec4 color;
    mainImage(color, gl_FragCoord.xy);
    gl_FragColor = vec4(color.rgb, 1.0);
}
