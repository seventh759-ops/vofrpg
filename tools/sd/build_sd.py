"""ダンジョンのSDキャラ（32×32、または縦長の 32×64 など。32 ドットで 1 マス）を、外で作った絵の色そのままで使うためのデータを作る。
tools/sd/見た目の名前.png（正面の絵）→ Game/Assets/Scripts/Core/SdRows.cs
・色ごとに1文字を割り当てた行と、文字→色の表
・足の色（いちばん下の数段にある茶色・靴の色）の文字：歩きのコマで足だけを持ち上げるのに使う
src/ には、サイトから書き出した8方向の元の絵を残しておく。"""
import glob, os, sys
from PIL import Image
D = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(D, '..', 'portrait'))
from flat import POOL

def build():
    names = []; rows_out = []; pal_out = []; FEET = {}
    for f in sorted(glob.glob(os.path.join(D, '*.png'))):
        name = os.path.splitext(os.path.basename(f))[0]
        im = Image.open(f).convert('RGBA'); w, h = im.size; px = im.load()
        bottom = max(y for y in range(h) for x in range(w) if px[x, y][3] >= 128)
        cmap = {}; boots = set(); rows = []
        # 体の真ん中（足元より少し上の段の、色のある粒の真ん中）。杖など、体から離れた所の茶色は足に数えない
        xs = sorted(x for y in range(max(0, bottom - 12), bottom - 3) for x in range(w) if px[x, y][3] >= 128)
        mid = xs[len(xs) // 2] if xs else w // 2
        for y in range(h):
            r = ''
            for x in range(w):
                c = px[x, y]
                if c[3] < 128: r += '.'; continue
                c = (c[0], c[1], c[2], 255)
                boot = y >= bottom - 3 and abs(x - mid) <= 7 and c[0] > c[2] + 10 and c[0] >= c[1] and 40 < c[0] < 220
                key = (c, boot)   # 足の粒は、同じ色でも別の文字にする（杖などと分ける）
                if key not in cmap: cmap[key] = POOL[len(cmap)]
                ch = cmap[key]
                if boot: boots.add(ch)
                r += ch
            rows.append(r)
        names.append(name)
        rows_out.append('            ["%s"] = new[]\n            {\n%s\n            },' % (name, '\n'.join('                "%s",' % r.replace('\\', '\\\\').replace('"', '\\"') for r in rows)))
        pal_out.append('            ["%s"] = new Dictionary<char, Color32> { %s },' % (name, ', '.join("{ '%s', new Color32(%d, %d, %d, %d) }" % ((ch.replace('\\', '\\\\').replace("'", "\\'"),) + c) for (c, _), ch in cmap.items())))
        FEET[name] = ''.join(sorted(boots))
    feet = '\n'.join('            ["%s"] = "%s",' % (n, FEET[n].replace('\\', '\\\\').replace('"', '\\"')) for n in names)
    src = '''using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// ダンジョンのSDキャラ（32×32）の、色をそのまま使う絵（tools/sd/build_sd.py で作る。手で直さない）。
    /// 陰影は計算しない。歩きのコマは、足の色の文字（FlatSdFeet）のある列だけを持ち上げて作る。
    /// </summary>
    public static partial class PixelArt
    {
        static readonly Dictionary<string, string[]> FlatSd = new Dictionary<string, string[]>
        {
%s
        };

        static readonly Dictionary<string, Dictionary<char, Color32>> FlatSdColors = new Dictionary<string, Dictionary<char, Color32>>
        {
%s
        };

        static readonly Dictionary<string, string> FlatSdFeet = new Dictionary<string, string>
        {
%s
        };
    }
}
''' % ('\n'.join(rows_out), '\n'.join(pal_out), feet)
    open(os.path.join(D, '..', '..', 'Game', 'Assets', 'Scripts', 'Core', 'SdRows.cs'), 'w', encoding='utf-8').write(src)
    print('built', names)

if __name__ == '__main__':
    build()
