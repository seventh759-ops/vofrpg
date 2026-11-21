"""外で作った 32×48 などのSDキャラを取り込む：python3 add_sd.py 入力.png 見た目の名前
縁の半透明の粒は半分より濃いものだけ残し、似た色（10 刻み）をまとめる。元の絵は src/見た目の名前/ に残す。"""
import os, shutil, sys
import numpy as np
from PIL import Image
D = os.path.dirname(os.path.abspath(__file__))
src, name = sys.argv[1], sys.argv[2]
os.makedirs(os.path.join(D, 'src', name), exist_ok=True)
shutil.copy(src, os.path.join(D, 'src', name, 'front_%dx%d.png' % Image.open(src).size))
a = np.asarray(Image.open(src).convert('RGBA')).astype(int)
mask = a[..., 3] >= 128
c = (np.round(a[..., :3] / 10) * 10).clip(0, 255).astype(np.uint8)
out = np.zeros(a.shape, np.uint8); out[..., :3] = c; out[..., 3] = np.where(mask, 255, 0)
Image.fromarray(out, 'RGBA').save(os.path.join(D, name + '.png'))
print(name, a.shape[1], 'x', a.shape[0], len({tuple(p) for p in c[mask]}), 'colors')
