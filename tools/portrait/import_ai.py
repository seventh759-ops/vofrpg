"""拡大されてぼやけたドット絵（AI で作った立ち絵など）を、元のドット1粒ずつの絵に戻して色を整える。

使い方: python3 import_ai.py 入力.webp 出力.png [ドットの大きさ(省略で自動)]
・ドットの大きさ（元の1粒が何ピクセルか）を色の変わり目の間隔から見つけ、粒の中心の色をとる
・色を減らす：くすんだ色（白・銀・肌・影）と、鮮やかな色（目・宝石・金）を分けて減らす（小さな宝石の色が消えないように）
・外の縁：背景に接する暗い粒は、ひとつの縁の色にそろえる
・ぼやけで出た、ぽつんと浮いた1粒（くすんだ色だけ）を、まわりの色にそろえる
"""
import sys
import numpy as np
from PIL import Image

def block_size(a):
    """色の変わり目の間隔で、いちばん多い間隔（4以上）を元の粒の大きさとみなす。"""
    gaps = []
    for y in range(0, a.shape[0], 7):
        row = a[y, :, :3].astype(int)
        d = np.where(np.any(np.abs(np.diff(row, axis=0)) > 30, axis=1))[0]
        gaps += [g for g in np.diff(d) if g >= 4]
    best, score = 8.0, -1
    for s in np.arange(4.0, 16.0, 0.1):
        r = np.array(gaps) / s
        sc = np.sum(np.abs(r - np.round(r)) < 0.12) - 0.02 * len(gaps) * (s < 5)
        if sc > score: best, score = s, sc
    return best

def kmeans(px, k, seed=0, it=30):
    if len(px) <= k: return px.copy(), np.arange(len(px))
    rng = np.random.default_rng(seed)
    cent = px[rng.choice(len(px), k, replace=False)].astype(float)
    for _ in range(it):
        lab = ((px[:, None, :] - cent[None]) ** 2).sum(-1).argmin(1)
        for j in range(k):
            if (lab == j).any(): cent[j] = px[lab == j].mean(0)
    return cent, lab

def convert(src, bs=None, neutral=30, vivid=18, outline=(30, 24, 44)):
    im = Image.open(src).convert('RGBA'); a = np.asarray(im)
    bs = bs or block_size(a)
    W = int(round(im.width / bs)); H = int(round(im.height / bs))
    col = np.zeros((H, W, 3)); alpha = np.zeros((H, W), bool)
    for y in range(H):
        for x in range(W):
            cy = int((y + 0.5) * bs); cx = int((x + 0.5) * bs)
            blk = a[max(0, cy - 2):cy + 3, max(0, cx - 2):cx + 3].reshape(-1, 4)
            alpha[y, x] = np.median(blk[:, 3]) > 128
            op = blk[blk[:, 3] > 128]
            if len(op): col[y, x] = np.median(op[:, :3], axis=0)
    px = col[alpha]
    mx, mn = px.max(1), px.min(1); sat = (mx - mn) / np.maximum(mx, 1)
    vmask = (sat > 0.35) & (mx > 60)
    q = np.full((H, W), -1)
    cn, ln = kmeans(px[~vmask], neutral)
    cv, lv = kmeans(px[vmask], vivid, seed=1) if vmask.any() else (np.zeros((0, 3)), np.zeros(0, int))
    labels = np.zeros(len(px), int); labels[~vmask] = ln; labels[vmask] = lv + len(cn)
    pal = np.vstack([cn, cv, [outline]]); OUT = len(pal) - 1; NV = len(cn)
    q[alpha] = labels
    lum = lambda c: 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
    for y in range(H):
        for x in range(W):
            if q[y, x] < 0: continue
            edge = any(not (0 <= y + dy < H and 0 <= x + dx < W) or q[y + dy, x + dx] < 0 for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge and lum(pal[q[y, x]]) < 110: q[y, x] = OUT
    for _ in range(2):
        q2 = q.copy()
        for y in range(1, H - 1):
            for x in range(1, W - 1):
                v = q[y, x]
                if v < 0 or v == OUT or v >= NV: continue
                if v in (q[y + 1, x], q[y - 1, x], q[y, x + 1], q[y, x - 1]): continue
                n8 = [q[y + dy, x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if (dy or dx) and q[y + dy, x + dx] >= 0]
                if not n8: continue
                vals, cnt = np.unique(n8, return_counts=True)
                if cnt.max() >= 6: q2[y, x] = vals[cnt.argmax()]
        q = q2
    out = np.zeros((H, W, 4), np.uint8)
    for y in range(H):
        for x in range(W):
            if q[y, x] >= 0: out[y, x] = (*np.clip(pal[q[y, x]], 0, 255).astype(np.uint8), 255)
    img = Image.fromarray(out, 'RGBA')
    return img.crop(img.getbbox()), bs

if __name__ == '__main__':
    img, bs = convert(sys.argv[1], float(sys.argv[3]) if len(sys.argv) > 3 else None)
    img.save(sys.argv[2]); print('dot size', round(bs, 2), 'size', img.size)
