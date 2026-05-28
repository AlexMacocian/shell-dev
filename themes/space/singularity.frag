// SOURCE: https://www.shadertoy.com/view/3csSWB  ("Singularity" by @XorDev)
// Adapted for glslViewer: aliases u_resolution/u_time to the
// iResolution/iTime Shadertoy globals so the original mainImage() stays
// unchanged. A gl_FragColor main() at the bottom of this file calls
// mainImage().

#ifdef GL_ES
precision highp float;
#endif

uniform vec2  u_resolution;
uniform float u_time;

#define iResolution vec3(u_resolution, 1.0)
#define iTime       u_time

/*
    "Singularity" by @XorDev

    A whirling blackhole.
    Feel free to code golf!
    
    FabriceNeyret2: -19
    dean_the_coder: -12
    iq: -4
*/

// =============================================================================
// Seamless-loop rate constants.
//
// Two iTime uses in mainImage:
//   line ~ : cos(... + iTime*0.2 + ...) — spiral rotation, rate 0.2
//   line ~ : sin(v.yx*i + iTime)        — wave coordinate distortion, rate 1.0
//
// For seamless looping the temporal contribution to each sin/cos must complete
// an integer N cycles of 2π per LOOP_SECS. We fix N (cycles per loop) so
// bumping duration_seconds in the theme json auto-rescales the rates without
// touching this file. N is chosen to land near the Shadertoy original at the
// default 20s loop.
//
//   SIN_RATE_1     = 1 * 2π / LOOP_SECS   (≈0.314 at 20s, was 1.0 — slowed)
//   SPIRAL_RATE    = 1 * 2π / LOOP_SECS   (≈0.314 at 20s, was 0.2 — one full
//                                          spiral rotation per loop)
// =============================================================================
#define LOOP_SECS ${LOOP_SECONDS}
const float _LOOP_TWO_PI = 6.2831853;
const float SIN_RATE_1   = 1.0 * _LOOP_TWO_PI / LOOP_SECS;
const float SPIRAL_RATE  = 1.0 * _LOOP_TWO_PI / LOOP_SECS;

void mainImage(out vec4 O, vec2 F)
{
    //Iterator and attenuation (distance-squared)
    float i = .2, a;
    //Resolution for scaling and centering
    vec2 r = iResolution.xy,
         //Centered ratio-corrected coordinates
         p = ( F+F - r ) / r.y / .7,
         //Diagonal vector for skewing
         d = vec2(-1,1),
         //Blackhole center
         b = p - i*d,
         //Rotate and apply perspective
         c = p * mat2(1, 1, d/(.1 + i/dot(b,b))),
         //Rotate into spiraling coordinates
         v = c * mat2(cos(.5*log(a=dot(c,c)) + iTime*SPIRAL_RATE + vec4(0,33,11,0)))/i,
         //Waves cumulative total for coloring
         w;
    
    //Loop through waves
    for(; i++<9.; w += 1.+sin(v) )
        //Distort coordinates
        v += .7* sin(v.yx*i + SIN_RATE_1*iTime) / i + .5;
    //Acretion disk radius
    i = length( sin(v/.3)*.4 + c*(3.+d) );
    //Red/blue gradient
    O = 1. - exp( -exp( c.x * vec4(.6,-.4,-1,0) )
                   //Wave coloring
                   /  w.xyyx
                   //Acretion disk brightness
                   / ( 2. + i*i/4. - i )
                   //Center darkness
                   / ( .5 + 1. / a )
                   //Rim highlight
                   / ( .03 + abs( length(p)-.7 ) )
             );
    }

// glslViewer entry point: dispatches to the Shadertoy mainImage().
// mainImage produces near-black where the swirl is absent; screen-blend
// the result over the theme background so the void picks up BG0 instead
// of being pure black. Also forces alpha=1 (the recorder treats <1 as
// transparency and yields a fully black mp4).
void main()
{
    vec4 color;
    mainImage(color, gl_FragCoord.xy);
    vec3 bg = ${BG};
    gl_FragColor = vec4(color.rgb + bg * (1.0 - color.rgb), 1.0);
}



//Original [432]
/*
void mainImage(out vec4 O,in vec2 F)
{
    vec2 p=(F*2.-iResolution.xy)/(iResolution.y*.7),
    d=vec2(-1,1),
    c=p*mat2(1,1,d/(.1+5./dot(5.*p-d,5.*p-d))),
    v=c;
    v*=mat2(cos(log(length(v))+iTime*.2+vec4(0,33,11,0)))*5.;
    vec4 o=vec4(0);
    for(float i;i++<9.;o+=sin(v.xyyx)+1.)
    v+=.7*sin(v.yx*i+iTime)/i+.5;
    O=1.-exp(-exp(c.x*vec4(.6,-.4,-1,0))/o
    /(.1+.1*pow(length(sin(v/.3)*.2+c*vec2(1,2))-1.,2.))
    /(1.+7.*exp(.3*c.y-dot(c,c)))
    /(.03+abs(length(p)-.7))*.2);
}*/
