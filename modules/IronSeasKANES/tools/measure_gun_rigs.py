"""Measure the turret rig numbers of the six Iron Seas KANES guns from their model.mu vertices, and check them against
the values written into the part cfgs by patch_ironseas_stage3.py (RIG). Reads files only.

usage: python -I measure_gun_rigs.py <GameData/IronSeasKANES dir>

Frames: model frame = part frame (rescaleFactor 1, every root transform is identity). Up on the boat is -Z.

Method (per gun):
  * The mesh is split into islands: connected triangles, vertices welded by position (10 um).
  * Barrel: the longest island (extent along its principal axis) whose length is at least 6x its thickness.
    Bore axis = its principal axis through its vertex centroid. Muzzle devices: islands whose vertices all lie within
    1.6x the barrel radius of that axis and reach the front quarter of the barrel. Muzzle = the point on the axis at
    the furthest forward vertex of barrel + devices.
  * Trunnion and pivot: from islands picked by triangle count and bounding-box size (see RECIPES), as described
    in each recipe. Trunnion axes are parallel to the part X axis on every gun, so circles are fitted in Y-Z.
  * x is reported but the cfgs use x = 0 (all |x| < 2 mm: the guns are symmetric about the Y-Z plane).

KSP note: these models were written by a converter that adds one extra float (1.0) after each transform's scale.
KSP's PartReader reads it as an unknown entry id and ignores it; this reader does the same.
"""
import sys, os, math, struct, collections

UP = (0.0, 0.0, -1.0)


# ---------------------------------------------------------------------------------------------------- .mu reading
class _R:
    def __init__(s, b): s.b = b; s.i = 0
    def int(s): v = struct.unpack_from('<i', s.b, s.i)[0]; s.i += 4; return v
    def flt(s, n=1):
        v = struct.unpack_from('<%df' % n, s.b, s.i); s.i += 4 * n; return v if n > 1 else v[0]
    def byte(s): v = s.b[s.i]; s.i += 1; return v
    def str(s):
        n = sh = 0
        while True:
            c = s.b[s.i]; s.i += 1; n |= (c & 0x7f) << sh
            if not c & 0x80: break
            sh += 7
        v = s.b[s.i:s.i + n].decode('utf-8', 'replace'); s.i += n; return v


def _qrot(q, v):
    x, y, z, w = q
    tx = 2 * (y * v[2] - z * v[1]); ty = 2 * (z * v[0] - x * v[2]); tz = 2 * (x * v[1] - y * v[0])
    return (v[0] + w * tx + (y * tz - z * ty), v[1] + w * ty + (z * tx - x * tz), v[2] + w * tz + (x * ty - y * tx))


def read_meshes(path):
    """Returns [(verts in model frame, triangles)] for every MeshFilter in the file."""
    r = _R(open(path, 'rb').read())
    assert r.int() == 76543, 'not a .mu file'
    version = r.int(); r.str()
    out = []

    def mesh():
        assert r.int() == 13
        vc = r.int(); r.int()
        verts, tris = [], []
        while True:
            et = r.int()
            if et == 22: return verts, tris
            if et == 14: verts = [r.flt(3) for _ in range(vc)]
            elif et in (15, 16): r.i += vc * 8
            elif et == 17: r.i += vc * 12
            elif et == 18: r.i += vc * 16
            elif et == 19:
                n = r.int(); t = struct.unpack_from('<%di' % n, r.b, r.i); r.i += 4 * n
                tris += [t[k:k + 3] for k in range(0, n, 3)]
            elif et == 20: r.i += vc * 32
            elif et == 21: n = r.int(); r.i += n * 64
            elif et == 32: r.i += vc * 4
            else: raise ValueError('mesh entry %d' % et)

    def node(chain):
        r.str(); pos = r.flt(3); rot = r.flt(4); scl = r.flt(3)
        chain = chain + [(pos, rot, scl)]
        while r.i < len(r.b):
            et = r.int()
            if et == 0: node(chain)
            elif et == 1: return
            elif et == 2:
                for _ in range(r.int()):
                    r.str(); r.flt(3); r.flt(3); r.int()
                    for _ in range(r.int()):
                        r.str(); r.str(); r.int(); r.int(); r.int(); n = r.int(); r.i += n * 20
                r.str(); r.byte()
            elif et in (3, 25):
                if et == 25: r.byte()
                r.byte(); mesh()
            elif et in (4, 26):
                if et == 26: r.byte()
                r.flt(); r.flt(3)
            elif et in (5, 27):
                if et == 27: r.byte()
                r.flt(); r.flt(); r.int(); r.flt(3)
            elif et in (6, 28):
                if et == 28: r.byte()
                r.flt(3); r.flt(3)
            elif et == 7:
                verts, tris = mesh()
                w = []
                for v in verts:  # local -> model frame (the root transform itself is skipped, as in KSP)
                    for p, q, s in reversed(chain[1:]):
                        v = _qrot(q, (v[0] * s[0], v[1] * s[1], v[2] * s[2]))
                        v = (v[0] + p[0], v[1] + p[1], v[2] + p[2])
                    w.append(v)
                out.append((w, tris))
            elif et == 8:
                if version > 0: r.byte(); r.byte()
                n = r.int(); r.i += 4 * n
            elif et == 23:
                r.int(); r.flt(); r.flt(); r.flt(4); r.int()
                if version > 1: r.flt()
            elif et == 24: r.str(); r.int()
            elif et == 30: r.int(); r.flt(4); r.int(); r.byte(); r.flt(4)
            elif et in (10, 12): r.i = len(r.b); return  # materials/textures: hierarchy done
            # anything else: unknown id, ignored like KSP does
    node([])
    return out


# ---------------------------------------------------------------------------------------------------- geometry
def sub(a, b): return tuple(a[i] - b[i] for i in range(3))
def add(a, b): return tuple(a[i] + b[i] for i in range(3))
def mul(a, s): return tuple(x * s for x in a)
def dot(a, b): return sum(a[i] * b[i] for i in range(3))
def norm(a): l = math.sqrt(dot(a, a)); return mul(a, 1 / l)


def islands(verts, tris):
    key, weld = {}, []
    for v in verts:
        weld.append(key.setdefault(tuple(round(c / 1e-5) for c in v), len(key)))
    par = list(range(len(key)))

    def find(a):
        while par[a] != a:
            par[a] = par[par[a]]; a = par[a]
        return a
    for a, b, c in tris:
        ra = find(weld[a]); par[find(weld[b])] = ra; par[find(weld[c])] = ra
    groups = collections.defaultdict(lambda: [set(), 0])
    for t in tris:
        g = groups[find(weld[t[0]])]; g[0].update(t); g[1] += 1
    out = []
    for vs, nt in groups.values():
        pts = [verts[i] for i in vs]
        lo = tuple(min(p[k] for p in pts) for k in range(3)); hi = tuple(max(p[k] for p in pts) for k in range(3))
        out.append(dict(pts=pts, tris=nt, lo=lo, hi=hi, size=sub(hi, lo)))
    return out


def pca(pts):
    n = len(pts); c = tuple(sum(p[k] for p in pts) / n for k in range(3))
    C = [[sum((p[i] - c[i]) * (p[j] - c[j]) for p in pts) / n for j in range(3)] for i in range(3)]
    v = (0.3, 0.7, 0.4)
    for _ in range(300):
        v = norm(tuple(sum(C[i][j] * v[j] for j in range(3)) for i in range(3)))
    return c, v


def dist_line(p, c, d):
    q = sub(p, c); return math.sqrt(max(0.0, dot(q, q) - dot(q, d) ** 2))


def circle_yz(pts):
    """Least-squares circle in the Y-Z plane (Kasa fit): centre (y, z), radius, max residual."""
    A = [[0.0] * 4 for _ in range(3)]
    for p in pts:
        y, z = p[1], p[2]; row = (y, z, 1.0); rhs = -(y * y + z * z)
        for i in range(3):
            A[i][3] += row[i] * rhs
            for j in range(3): A[i][j] += row[i] * row[j]
    for c in range(3):
        piv = max(range(c, 3), key=lambda k: abs(A[k][c])); A[c], A[piv] = A[piv], A[c]
        for k in range(3):
            if k != c:
                f = A[k][c] / A[c][c]; A[k] = [A[k][m] - f * A[c][m] for m in range(4)]
    D, E, F = (A[i][3] / A[i][i] for i in range(3))
    cy, cz = -D / 2, -E / 2; rad = math.sqrt(cy * cy + cz * cz - F)
    return cy, cz, rad, max(abs(math.hypot(p[1] - cy, p[2] - cz) - rad) for p in pts)


def pick(isl, tris, size, tol=0.002):
    got = [i for i in isl if i['tris'] == tris and all(abs(i['size'][k] - size[k]) <= tol for k in range(3))]
    assert got, 'no island with %d triangles and size %s' % (tris, size)
    return got


# Per gun: how the trunnion and the pivot are found. Island signatures are (triangle count, bbox size in m).
def r_20mm(isl):
    arms = pick(isl, 46, (0.046, 0.094, 0.166))            # the two cradle fork arms on top of the column
    pts = [p for a in arms for p in a['pts']]
    top = min(p[2] for p in pts)
    y = (min(p[1] for p in pts) + max(p[1] for p in pts)) / 2
    return dict(trunnion=(0.0, y, top), pivot=(0.0, 0.0, max(p[2] for p in pts)),
                how='trunnion: centre line and flat top face of the fork arms (no trunnion pin is modelled); '
                    'pivot: bottom of the fork arms = top of the fixed column')


def r_50cal(isl):
    yoke = pick(isl, 43, (0.014, 0.094, 0.147))            # the two cradle yoke arms
    pts = [p for a in yoke for p in a['pts']]
    lo, hi = min(p[2] for p in pts), max(p[2] for p in pts)
    lug = [p for p in pts if p[2] <= lo + 0.4 * (hi - lo)]  # round lug at the top end
    cy, cz, rad, res = circle_yz(lug)
    pintle = pick(isl, 8, (0.028, 0.028, 0.029))[0]
    return dict(trunnion=(0.0, cy, cz), pivot=(0.0, 0.0, pintle['lo'][2]),
                how='trunnion: centre of the round lug at the top of the yoke arms (circle r = %.4f m, max residual '
                    '%.4f m); pivot: top of the pintle' % (rad, res))


def r_40mm(isl):
    brg = pick(isl, 16, (0.16, 0.12, 0.11))                # inner trunnion bearings (coaxial with two more pairs)
    cy, cz, rad, res = circle_yz([p for b in brg for p in b['pts']])
    base = pick(isl, 68, (1.09, 1.06, 0.18))[0]
    return dict(trunnion=(0.0, cy, cz), pivot=(0.0, 0.0, base['lo'][2]),
                how='trunnion: axis of the trunnion bearings on the carriage (circle r = %.4f m, max residual %.4f m); '
                    'pivot: top of the base plate' % (rad, res))


def r_4in(isl):
    pins = pick(isl, 28, (0.076, 0.167, 0.166))            # the two trunnion pins
    cy, cz, rad, res = circle_yz([p for b in pins for p in b['pts']])
    ring = pick(isl, 44, (0.517, 0.517, 0.066))[0]
    return dict(trunnion=(0.0, cy, cz), pivot=(0.0, 0.0, ring['lo'][2]),
                how='trunnion: centre of the trunnion pins (circle r = %.4f m, max residual %.4f m); pivot: top of the '
                    'fixed pedestal ring' % (rad, res))


RECIPES = {'20mmFF': r_20mm, '20mmRF': r_20mm, '50Cal': r_50cal, 'Single40mm': r_40mm,
           '4inchFF': r_4in, '4inchRF': r_4in}


def measure(model_path, recipe):
    meshes = read_meshes(model_path)
    assert len(meshes) == 1, 'expected one merged mesh, found %d mesh nodes' % len(meshes)
    verts, tris = meshes[0]
    isl = islands(verts, tris)
    best = None
    for i in isl:
        if len(i['pts']) < 8: continue
        c, d = pca(i['pts'])
        pr = [dot(sub(p, c), d) for p in i['pts']]
        length = max(pr) - min(pr); thick = 2 * max(dist_line(p, c, d) for p in i['pts'])
        if length >= 6 * thick and (best is None or length > best[0]):
            best = (length, i, c, d, thick / 2)
    length, barrel, c, d, rad = best
    out = recipe(isl)
    if dot(sub(c, out['trunnion']), d) < 0: d = mul(d, -1)       # bore points away from the trunnion
    front = max(dot(sub(p, c), d) for p in barrel['pts'])
    back = min(dot(sub(p, c), d) for p in barrel['pts'])
    for i in isl:
        if i is barrel: continue
        if all(dist_line(p, c, d) <= 1.6 * rad for p in i['pts']):
            pr = [dot(sub(p, c), d) for p in i['pts']]
            if max(pr) > front - 0.25 * (front - back):
                front = max(front, max(pr))
    out.update(muzzle=add(c, mul(d, front)), bore=d, islands=len(isl), tris=len(tris),
               elev=math.degrees(math.asin(dot(d, UP))))
    return out


def f3(v): return '(' + ', '.join('%.4f' % (0.0 if abs(x) < 5e-5 else x) for x in v) + ')'


if __name__ == '__main__':
    root = sys.argv[1]
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from patch_ironseas_stage3 import RIG
    bad = 0
    for gun, recipe in RECIPES.items():
        m = measure(os.path.join(root, 'BalaoClassFleetBoat', gun, 'model.mu'), recipe)
        cfg = {k: tuple(float(x) for x in RIG[gun][k][0].split(',')) for k in ('pivot', 'trunnion', 'bore', 'muzzle')}
        dp = max(math.dist(m[k], cfg[k]) for k in ('pivot', 'trunnion', 'muzzle'))
        da = math.degrees(math.acos(min(1.0, dot(norm(m['bore']), norm(cfg['bore'])))))
        ok = dp <= 0.002 and da <= 0.1
        bad += not ok
        print(f'{gun}: one merged mesh, {m["tris"]} triangles in {m["islands"]} islands')
        print(f'   pivot    {f3(m["pivot"])}   trunnion {f3(m["trunnion"])}')
        print(f'   bore     {f3(m["bore"])}  elevation {m["elev"]:.2f} deg   muzzle {f3(m["muzzle"])}')
        print(f'   {m["how"]}')
        print(f'   cfg values: max position difference {dp * 1000:.1f} mm, bore difference {da:.3f} deg -> {"OK" if ok else "MISMATCH"}')
    print('all six match the cfgs' if not bad else '%d gun(s) do not match' % bad)
    sys.exit(1 if bad else 0)
