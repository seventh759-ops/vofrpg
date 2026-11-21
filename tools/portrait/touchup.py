"""取り込んだ立ち絵（ai/src/）の、ぼやけた部分（顔・胸の飾りなど）を、くっきりした色の組に置き換えて ai/ に書き出す。
範囲ごとに「使ってよい色」を決め、範囲の中の粒をいちばん近い色にそろえる（中間のにごった色をなくす）。
宝石には左上に照りを1粒足す。"""
import os
from PIL import Image
D = os.path.dirname(os.path.abspath(__file__))

OUT = (34, 26, 46)
SKIN = [(252, 228, 208), (238, 200, 176), (206, 158, 136), (150, 100, 92)]
GOLD = [(255, 228, 140), (226, 176, 74), (168, 112, 40), (104, 66, 28)]
WHITE = [(255, 255, 255), (232, 234, 244), (196, 198, 218), (148, 148, 176)]
SILVER = [(236, 236, 246), (198, 200, 220), (150, 150, 180), (104, 102, 132)]
GEM_R = [(255, 140, 140), (226, 40, 52), (150, 16, 30)]
GEM_P = [(214, 160, 255), (160, 80, 220), (86, 40, 140)]
GEM_B = [(150, 210, 255), (70, 140, 240), (34, 64, 150)]
GEM_G = [(140, 240, 160), (50, 190, 100), (20, 100, 60)]
NAVY = [(70, 96, 170), (40, 56, 110)]
BROWN_H = [(176, 120, 76), (130, 84, 52), (88, 54, 36)]
EYE_BR = [(170, 120, 80), (110, 70, 44)]
BLUE_ROBE = [(90, 130, 210), (52, 86, 170), (32, 50, 110)]

# 見た目の名前 → [(範囲 x0,y0,x1,y1（含む）, 使ってよい色)]
REGIONS = {
    'Vividbearer': [
        ((38, 10, 51, 22), [OUT] + SKIN + SILVER + [GEM_R[1], GEM_R[2], GEM_P[0], GEM_P[1], GEM_P[2], (255, 255, 255)]),   # 顔
        ((38, 6, 52, 9), [OUT] + GOLD + GEM_B + SILVER),                                                                   # 冠
        ((38, 23, 54, 46), [OUT] + GOLD + GEM_R + GEM_P + GEM_B + GEM_G + WHITE + NAVY),                                   # 襟と胸の飾り
    ],
}

def near(c, pal):
    return min(pal, key=lambda p: sum((c[i] - p[i]) ** 2 for i in range(3)))

import colorsys
def classify(c, pal):
    """色合いで仲間を決め、その仲間の色の組の中でいちばん近い色にする。くすんだ色（白・銀・灰）はそのまま。"""
    h, l, s_ = colorsys.rgb_to_hls(*(v / 255 for v in c[:3])); h *= 360
    mx = max(c[:3]); sat = (mx - min(c[:3])) / max(mx, 1)
    if mx < 70: return OUT if OUT in pal else None
    fam = None
    if SKIN[0] in pal and 12 <= h <= 38 and 0.15 <= sat <= 0.5 and l > 0.55: fam = SKIN
    elif 30 <= h <= 60 and sat > 0.22: fam = GOLD
    elif (h < 14 or h > 340) and sat > 0.35: fam = GEM_R
    elif 255 <= h <= 330 and sat > 0.4: fam = GEM_P
    elif 190 <= h < 255 and sat > 0.45: fam = GEM_B
    elif 90 <= h < 175 and sat > 0.35: fam = GEM_G
    if fam is None or fam[0] not in pal: return None
    n = near(c, fam)
    if abs(sum(n) - sum(c[:3])) > 150: return None   # 明るさが大きく変わるものは置き換えない
    return n

# 色の置き換え（範囲の中の、決まった色を別の色へ）と、1粒ずつの描き直し（目など）
H = lambda s: tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))
RECOLOR = {
}
LASH, IRIS, IRISD, HL = H('2a1a14'), H('9a6438'), H('5a3820'), (255, 255, 255)
PIXELS = {
}

def touch(name):
    im = Image.open(os.path.join(D, 'ai', 'src', name + '.png')).convert('RGBA'); px = im.load()
    for (x0, y0, x1, y1), pal in REGIONS.get(name, []):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                c = px[x, y]
                if c[3] == 0: continue
                n = classify(c, pal)
                if n: px[x, y] = (*n, 255)
        # 宝石の照り：鮮やかな色の粒のかたまりの左上に、一番明るい色を1粒
        gems = [g for g in (GEM_R, GEM_P, GEM_B, GEM_G) if g[1] in pal]
        for g in gems:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    if px[x, y][:3] == g[1] and px[x - 1, y][:3] not in (g[1], g[0]) and px[x, y - 1][:3] not in (g[1], g[0]):
                        px[x, y] = (*g[0], 255)
    for (x0, y0, x1, y1), m in RECOLOR.get(name, []):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                c = px[x, y]
                if c[3] and c[:3] in m: px[x, y] = (*m[c[:3]], 255)
    for (x, y), c in PIXELS.get(name, {}).items(): px[x, y] = (*c, 255)
    im.save(os.path.join(D, 'ai', name + '.png'))

if __name__ == '__main__':
    import sys
    for n in (sys.argv[1:] or sorted(set(REGIONS) | set(RECOLOR) | set(PIXELS))): touch(n)
