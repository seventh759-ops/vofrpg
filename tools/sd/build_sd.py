"""ダンジョンのSDキャラ（32×32、または縦長の 32×64 など。32 ドットで 1 マス）を、外で作った絵の色そのままで使うためのデータを作る。
tools/sd/見た目の名前.png（正面の絵）→ Game/Assets/Scripts/Core/SdRows.cs
・色ごとに1文字を割り当てた行と、文字→色の表
・足の色（いちばん下の数段にある茶色・靴の色）の文字：歩きのコマで足だけを持ち上げるのに使う
src/ には、サイトから書き出した8方向の元の絵を残しておく。"""
import glob, os, sys
from PIL import Image
D = os.path.dirname(os.path.abspath(__file__))

def build():
    import base64, io
    names = []; png_out = []; feet_out = []
    for f in sorted(glob.glob(os.path.join(D, '*.png'))):
        name = os.path.splitext(os.path.basename(f))[0]
        im = Image.open(f).convert('RGBA'); w, h = im.size; px = im.load()
        bottom = max(y for y in range(h) for x in range(w) if px[x, y][3] >= 128)
        # 体の真ん中（足元より少し上の段の、色のある粒の真ん中）。杖など、体から離れた所の茶色は足に数えない
        k = max(1, round(h / 32))   # 細かい絵ほど、足元の段数を増やす（ゲームの AnimRows と同じ）
        xs = sorted(x for y in range(max(0, bottom - 12 * k), bottom - 4 * k + 1) for x in range(w) if px[x, y][3] >= 128)
        mid = xs[len(xs) // 2] if xs else w // 2
        boot_x = []
        for y in range(bottom - 4 * k + 1, bottom + 1):
            for x in range(w):
                c = px[x, y]
                if c[3] < 128: continue
                # 細かい絵は、足元の真ん中の色はみな足（靴の色が茶色とは限らない）。32 ドットの絵は茶色だけ
                if abs(x - mid) <= w * 0.25 and (k > 1 or (c[0] > c[2] + 10 and c[0] >= c[1] and 40 < c[0] < 220)): boot_x.append(x)
        # 半透明の縁は、半分より濃いものだけ残す
        out = Image.new('RGBA', (w, h), (0, 0, 0, 0)); op = out.load()
        for y in range(h):
            for x in range(w):
                c = px[x, y]
                if c[3] >= 128: op[x, y] = (c[0], c[1], c[2], 255)
        buf = io.BytesIO(); out.save(buf, 'PNG', optimize=True)
        names.append(name)
        png_out.append('            ["%s"] = "%s",' % (name, base64.b64encode(buf.getvalue()).decode()))
        feet_out.append('            ["%s"] = new[] { %d, %d },' % (name, min(boot_x) if boot_x else -1, max(boot_x) if boot_x else -1))
    src = '''using System.Collections.Generic;

namespace VividOfFade
{
    /// <summary>
    /// ダンジョンのSDキャラ（32×48 など）の、色をそのまま使う絵（tools/sd/build_sd.py で作る。手で直さない）。
    /// 絵は PNG（base64）で持ち、起動中に読み込む（色の表をコードに書くと、Android のビルドで静的コンストラクタが巨大になって失敗するため）。
    /// FlatSdFeet は足の粒のある列の範囲（左端・右端。歩きのコマで、その列の足元だけを持ち上げる）。
    /// </summary>
    public static partial class PixelArt
    {
        static readonly Dictionary<string, string> FlatSdPng = new Dictionary<string, string>
        {
%s
        };

        static readonly Dictionary<string, int[]> FlatSdFeet = new Dictionary<string, int[]>
        {
%s
        };
    }
}
''' % ('\n'.join(png_out), '\n'.join(feet_out))
    open(os.path.join(D, '..', '..', 'Game', 'Assets', 'Scripts', 'Core', 'SdRows.cs'), 'w', encoding='utf-8').write(src)
    print('built', names)

if __name__ == '__main__':
    build()
