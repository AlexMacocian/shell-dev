// SOURCE: https://www.shadertoy.com/view/sXXGDl  ("Welcome to Arrakeen" by mrange, CC0)
// Adapted for glslViewer: the original is a multi-pass shader (Common +
// Buffer A + Image). glslViewer renders a single pass, so Buffer A's
// raymarched terrain is inlined into mainImage() — wherever the Image
// pass would have sampled `texelFetch(iChannel0, xy, 0)` it now calls
// `bufferA(fragCoord)` directly. Temporal antialiasing is dropped (TAA
// needs the previous frame's render-target, which a single pass can't
// provide), so the YCoCg encode/decode round-trip is harmless ballast
// kept for parity with the source.
//
// Seamless loop: the camera is driven by iTime (not iFrame, to keep the
// pacing fps-independent and identical to the Shadertoy original) and
// the crater heightfield is mirror-wrapped so frame 0 == frame at
// iTime == ${LOOP_SECONDS}. See the "Seamless-loop constants" block
// below.
//
// A gl_FragColor main() at the bottom of this file calls mainImage().
// Aliases u_resolution/u_time/u_frame to the Shadertoy iResolution/
// iTime/iFrame globals so the upstream code stays unchanged. round() is
// polyfilled for the GLSL 1.20 profile glslViewer compiles against.

#ifdef GL_ES
precision highp float;
#endif

uniform vec2  u_resolution;
uniform float u_time;
uniform int   u_frame;

#define iResolution vec3(u_resolution, 1.0)
#define iTime       u_time
#define iFrame      u_frame

// Declared because the Image pass historically reads iChannel0 (Buffer
// A's texture). bufferA() replaces every read, so this uniform is just
// a stub to keep any stray identifier valid.
uniform sampler2D iChannel0;

float round(float x) { return floor(x + 0.5); }
vec2  round(vec2  v) { return floor(v + 0.5); }
vec3  round(vec3  v) { return floor(v + 0.5); }

// (DUNE_LOGO disabled — the original draws a stylized DUNE word over the
// frame; we skip it so the wallpaper stays clean.)

// =============================================================================
// Common pass
// =============================================================================

// License: WTFPL, author: sam hocevar, found: https://stackoverflow.com/a/17897228/418488
const vec4 hsv2rgb_K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
#define HSV2RGB(c)  (c.z * mix(hsv2rgb_K.xxx, clamp(abs(fract(c.xxx + hsv2rgb_K.xyz) * 6.0 - hsv2rgb_K.www) - hsv2rgb_K.xxx, 0.0, 1.0), c.y))
#define ROT(a)      mat2(cos(a), sin(a), -sin(a), cos(a))

const int   max_iter_hi  = 70;
const int   max_iter_lo  = 60;
const float epsilon      = 1e-4;
const float max_distance = 60.0;
const float tolerance    = 1e-3;
const float crater_off   = 0.3;

// ----- Seamless-loop constants -------------------------------------------
// The crater heightfield uses an irrational 2x2 rotation (R = mat2(6,8,
// -8,6)/5 ≈ rotate atan(4/3)·scale 2 per iteration), so it has no
// natural period. We force one: mirror-wrap the z-coord at the top of
// hf_lo/hf_hi with period ARRAKEEN_HF_PERIOD, and advance the camera by
// exactly one full WORLD period over LOOP_SECONDS so frame 0 and frame
// N see the same wrapped terrain. The mirror means the apparent
// terrain motion reverses at the loop midpoint — at the Shadertoy's
// native 0.6 units/s pacing the reversal is barely perceptible.
//
// ${LOOP_SECONDS} is substituted by GlslConverter from the json entry.
const float ARRAKEEN_SPEED        = 0.6;
const float ARRAKEEN_LOOP_SECONDS = ${LOOP_SECONDS};
// World-space distance the camera travels per loop. Must equal the
// terrain wrap period in world coords.
const float ARRAKEEN_WORLD_PERIOD = ARRAKEEN_SPEED * ARRAKEEN_LOOP_SECONDS;
// hf_lo/hf_hi receive p = 0.25 * world.xz, so the wrap period in their
// local input space is the world period scaled by the same 0.25 factor.
const float ARRAKEEN_HF_PERIOD    = 0.25 * ARRAKEEN_WORLD_PERIOD;

// GLSL 1.20 disallows function-call const initializers (normalize/
// HSV2RGB are not constant expressions), so these palette constants are
// promoted to globals and initialised once at the top of mainImage()
// via initConstants(). Order of use is safe because mainImage is the
// sole entry point and runs initConstants before any helper.
vec3 ld;
vec3 sun_col0, sun_col1, dust_col, sky_col, city_col, moon_col;

void initConstants() {
  ld       = normalize(vec3(-1.5, 0.3, 2.0));
  sun_col0 = HSV2RGB(vec3(0.06, 0.9,  3e-3));
  sun_col1 = HSV2RGB(vec3(0.0,  0.95, 3e-5));
  dust_col = HSV2RGB(vec3(0.03, 1.0,  2.0));
  sky_col  = HSV2RGB(vec3(0.58, 0.7,  0.5));
  city_col = HSV2RGB(vec3(0.06, 0.5,  0.6));
  moon_col = HSV2RGB(vec3(0.02, 0.9,  1.0));
}

float hash1(float co) {
  return fract(sin(co * 12.9898) * 13758.5453);
}

vec2 hash2(vec2 p) {
  p += 0.123;
  p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
  return fract(sin(p) * 43758.5453123);
}

vec2 shash2(vec2 p) {
  return -1.0 + 2.0 * hash2(p);
}

// License: Unknown, author: gelami, found: https://www.shadertoy.com/view/DsfGWX
vec3 RGBtoYCoCg(vec3 c) {
  return mat3(0.25, 0.5, -0.25, 0.5, 0.0, 0.5, 0.25, -0.5, -0.25) * c;
}

vec3 YCoCgToRGB(vec3 c) {
  return mat3(1.0, 1.0, 1.0, 1.0, 0.0, -1.0, -1.0, 1.0, -1.0) * c;
}

// jitter() is only useful when feeding TAA. Kept for parity but the
// inlined Buffer A no longer adds it to the fragment coord.
vec2 jitter(int frame) {
  return fract(float(frame) * vec2(0.7548776662, 0.5698402910)) - 0.5;
}

vec3 ray_origin(int frameNo) {
  return vec3(9.0, 2.0, iTime * ARRAKEEN_SPEED);
}

vec3 look_at(int frameNo) {
  return vec3(9.0, 1.88, 4.0 + iTime * ARRAKEEN_SPEED);
}

mat3 camera(vec3 ro, vec3 lo) {
  vec3 Z = normalize(lo - ro);
  vec3 X = normalize(cross(Z, vec3(0.0, 1.0, 0.0)));
  vec3 Y = cross(X, Z);
  return mat3(X, Y, Z);
}

// =============================================================================
// Buffer A pass (TAA removed; render() called directly from mainImage)
// =============================================================================

float crater(vec2 p, vec2 o) {
  float d = length(p + o) - 0.25;
  d *= 8.0;
  d = d > 0.0
        ? smoothstep(2.0, -2.0, d)
        : 2.0 * smoothstep(1.0, -1.0, -d) - 0.5;
  p = abs(p);
  d *= smoothstep(0.5, 0.3, max(p.x, p.y));
  return d;
}

float hf_lo(vec2 p) {
  const mat2 R = mat2(6.0, 8.0, -8.0, 6.0) / 5.0;
  // Mirror-wrap z: yw triangles in [0, ARRAKEEN_HF_PERIOD/2] with
  // period ARRAKEEN_HF_PERIOD, so the terrain is identical at
  // p.y == p.y + ARRAKEEN_HF_PERIOD (i.e. across one camera loop).
  float yw = mod(p.y, ARRAKEEN_HF_PERIOD);
  p.y = yw > ARRAKEEN_HF_PERIOD * 0.5 ? ARRAKEEN_HF_PERIOD - yw : yw;
  float a = 1.0;
  float h = 0.0;
  p *= 0.2;
  for (int i = 0; i < 4; ++i) {
    vec2 N = floor(p + 0.5);
    vec2 H = shash2(N);
    vec2 C = p - N;
    if (fract(H.x + H.y) > 0.7 - 0.1 * float(i)) {
      h += a * crater(C, crater_off * H);
    }
    p *= R;
    a *= 0.48;
  }
  return h;
}

float hf_hi(vec2 p) {
  const mat2 R = mat2(6.0, 8.0, -8.0, 6.0) / 5.0;
  float yw = mod(p.y, ARRAKEEN_HF_PERIOD);
  p.y = yw > ARRAKEEN_HF_PERIOD * 0.5 ? ARRAKEEN_HF_PERIOD - yw : yw;
  float a = 1.0;
  float h = 0.0;
  p *= 0.2;
  for (int i = 0; i < 9; ++i) {
    vec2 N = floor(p + 0.5);
    vec2 H = shash2(N);
    vec2 C = p - N;
    if (fract(H.x + H.y) > 0.7 - 0.1 * float(i)) {
      h += a * crater(C, crater_off * H);
    }
    p *= R;
    a *= 0.48;
  }
  return h;
}

float df_lo(vec3 p) { return (p.y - hf_lo(0.25 * p.xz)) + 1.0; }
float df_hi(vec3 p) { return (p.y - hf_hi(0.25 * p.xz)) + 1.0; }

float raymarch_lo(vec3 ro, vec3 ri, float iz) {
  float d;
  float z = iz;
  for (int i = 0; i < max_iter_lo; ++i) {
    d = df_lo(z * ri + ro);
    if (d < tolerance || z > max_distance) break;
    z += d;
  }
  return z;
}

float raymarch_hi(vec3 ro, vec3 ri, float iz) {
  float d;
  float nd = 1e3;
  float nz = iz;
  float z  = iz;
  int   i;
  for (i = 0; i < max_iter_hi; ++i) {
    d = df_hi(z * ri + ro);
    if (d < nd) {
      nd = d;
      nz = z;
    }
    if (d < tolerance || z > max_distance) break;
    z += d;
  }
  if (i == max_iter_hi) {
    z = nz;
  }
  return z;
}

// License: MIT, author: IQ, found: https://www.shadertoy.com/view/4ds3zn
vec3 normal_hi(vec3 p) {
  const vec2 e = vec2(epsilon, -epsilon);
  return normalize(
      e.xyy * df_hi(p + e.xyy)
    + e.yyx * df_hi(p + e.yyx)
    + e.yxy * df_hi(p + e.yxy)
    + e.xxx * df_hi(p + e.xxx)
  );
}

vec3 render(vec3 ro, vec3 ri, out float oz) {
  float pz = (0.0 - ro.y) / ri.y;
  if (pz < 0.0) {
    oz = max_distance;
    return vec3(0.0);
  }
  float z = raymarch_hi(ro, ri, pz);
  if (z >= max_distance) {
    oz = max_distance;
    return vec3(0.0);
  }
  oz = z;
  vec3 p = z * ri + ro;
  vec3 n_hi = normal_hi(p);
  float s = raymarch_lo(p + 0.05 * n_hi, ld, 0.0);
  float dif = max(1.1 - n_hi.y, 0.0);
  dif *= dif;
  return (step(max_distance, s) * max(dot(ld, n_hi), 0.0) + 0.3 * dif) * moon_col;
}

// Inlined Buffer A entry point. Returns YCoCg colour in xyz and the hit
// distance in w (>= max_distance means sky / no hit), matching the
// Shadertoy buffer's storage layout.
vec4 bufferA(vec2 C) {
  float z = 0.0;
  vec2 r = iResolution.xy;
  vec2 p = (C + C - r) / r.y;
  vec3 ro = ray_origin(iFrame);
  vec3 la = look_at(iFrame);
  mat3 cmat = camera(ro, la);
  vec3 ri = normalize(cmat * vec3(p, 2.0));
  vec3 o  = render(ro, ri, z);
  o = RGBtoYCoCg(o);
  return vec4(o, z);
}

// =============================================================================
// Image pass: sky, sun, dust, optional city + Dune logo, AGX tone-mapping
// =============================================================================

// License: MIT, author: Inigo Quilez, found: https://iquilezles.org/articles/intersectors/
vec2 ray_sphere(vec3 ro, vec3 rd, vec4 sph) {
  vec3 oc = ro - sph.xyz;
  float b = dot(oc, rd);
  float c = dot(oc, oc) - sph.w * sph.w;
  float h = b * b - c;
  if (h < 0.0) return vec2(-1.0);
  h = sqrt(h);
  return vec2(-b - h, -b + h);
}

// License: Unknown, author: bwrensch, found: https://www.shadertoy.com/view/cd3XWr
vec3 agxDefaultContrastApprox(vec3 x) {
  vec3 x2 = x * x;
  vec3 x4 = x2 * x2;
  return + 15.5   * x4 * x2
         - 40.14  * x4 * x
         + 31.96  * x4
         - 6.868  * x2 * x
         + 0.4298 * x2
         + 0.1191 * x
         - 0.00232;
}

vec3 agx(vec3 val) {
  const mat3 agx_mat = mat3(
    0.842479062253094, 0.0423282422610123, 0.0423756549057051,
    0.0784335999999992, 0.878468636469772, 0.0784336,
    0.0792237451477643, 0.0791661274605434, 0.879142973793104);

  const float min_ev = -12.47393;
  const float max_ev =   4.026069;

  val = agx_mat * val;
  val = clamp(log2(val), min_ev, max_ev);
  val = (val - min_ev) / (max_ev - min_ev);
  val = agxDefaultContrastApprox(val);
  return val;
}

vec3 agxEotf(vec3 val) {
  const mat3 agx_mat_inv = mat3(
     1.19687900512017,  -0.0528968517574562, -0.0529716355144438,
    -0.0980208811401368, 1.15190312990417,   -0.0980434501171241,
    -0.0990297440797205,-0.0989611768448433,  1.15107367264116);
  val = agx_mat_inv * val;
  return val;
}

vec3 agxLook(vec3 val) {
  const vec3 lw = vec3(0.2126, 0.7152, 0.0722);
  float luma = dot(val, lw);
  const vec3 offset = vec3(0.0);
  const vec3 slope  = vec3(1.0);
  const vec3 power  = vec3(1.0, 1.1, 1.0);
  float sat = 1.0;
  val = pow(val * slope + offset, power);
  return luma + sat * (val - luma);
}

vec3 dune(vec3 o, vec2 p) {
  vec2 C = p *= sign(p.x);
  p.x -= step(1.0, C).x + 0.5;
  float d = min(0.013 - abs(length(min(p = C.x > 1.0 ? p : -p.yx, vec2(0.0, 1.0))) - 0.17), 0.2 - p.x);
  float aa = sqrt(2.0) / iResolution.y;
  o = mix(o, 2.0 * sqrt(o), smoothstep(-aa, aa, d));
  return o;
}

float city(vec2 p) {
  float t = 0.0;
  float n;
  float d  = length(p - vec2(0.0, -15.0)) - 32.0;
  float aa = length(fwidth(p));
  if (p.y < 0.0) return 1.0;

  t = smoothstep(aa, -aa, abs(d) - 0.2) * 0.5;

  if (d > 0.0) return t;
  t += 0.125;
  for (float i = 1.0; i < 4.0; ++i) {
    p *= 0.8;
    n = floor(p.x + 0.5);
    if (n == 0.0) continue;
    if (abs(p.x - n) > 0.4) continue;
    t = max(t, i / 3.0 * step(0.0, 8.0 * 100.0 * hash1(n + 2.18 + i * 0.123) / (100.0 + n * n) - p.y));
  }
  return t;
}

void mainImage(out vec4 O, vec2 C) {
  initConstants();

  // Replaces the original `texelFetch(iChannel0, ivec2(C), 0)` lookup
  // into Buffer A's render target with a direct call to the inlined
  // raymarcher. P.xyz is YCoCg colour, P.w is hit distance.
  vec4 P  = bufferA(C);
  vec4 pd = 1e3 * vec4(9.0, -2.0, 1e1, 5.5);

  vec2 r  = iResolution.xy;
  vec2 p  = (C + C - r) / r.y;
  vec2 dp;

  vec3 ro = ray_origin(iFrame);
  vec3 la = look_at(iFrame);

  mat3 cmat = camera(ro, la);

  vec3 ri = normalize(cmat * vec3(p, 2.0));
  vec3 o  = P.xyz;
  vec3 pp, np, s;

  float fo  = exp(-0.0005 * P.w * P.w);
  float ifo = 1.0 - fo;
  float dfo = smoothstep(0.2, 0.0, ri.y + 0.125);
  float sfo = smoothstep(0.5, 0.0, ri.y + 0.04);
  float pfo = smoothstep(0.0, 0.1, ri.y + 0.04);

  dp = ray_sphere(vec3(0.0, 2.0, 0.0), ri, pd);
  pp = dp.x * ri + vec3(0.0, 2.0, 0.0);
  np = normalize(pp - pd.xyz);

  o = YCoCgToRGB(o);
  o = max(vec3(0.0), o);

  s  = dust_col * dfo;
  s += sky_col  * sfo;
  s *= ifo * ifo * ifo;
  o += s;

  s  = sun_col0 / max(1.0005   - dot(ri * vec3(1.0, 1.07, 1.0), ld), 0.0);
  s += sun_col1 / (0.5 * max(1.000001 - dot(ri, normalize(vec3(-0.775, -0.03, 1.0))), 0.0));
  s += sky_col  * (2e-3 / (1.0001 - dot(ri, normalize(vec3(0.0, -0.04, 1.0)))));
  s *= (1.5 - dfo);
  o += s;

  s  = (dp.y - dp.x) * (max(0.0, dot(np, ld))) * vec3(1e-4);
  s += sky_col * 5e-3 / max(abs(ri.x) + 0.2 * ri.y * ri.y, 1e-4);
  s *= pfo;
  o += s;

  s = city_col * city(200.0 * (ri.xy - vec2(0.0, -0.07))) * smoothstep(-0.06, 0.01, ri.y) * step(max_distance, P.w);
  o += s;

  o = clamp(o, 0.0, 9.0);
#ifdef DUNE_LOGO
  o = dune(o, p);
#endif
  o = agx(o);
  o = agxLook(o);
  o = agxEotf(o);
  O = vec4(o, 1.0);
}

// glslViewer entry point: dispatches to the Shadertoy mainImage().
void main() {
  vec4 color;
  mainImage(color, gl_FragCoord.xy);
  gl_FragColor = vec4(color.rgb, 1.0);
}
