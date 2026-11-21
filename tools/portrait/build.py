import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from canvas import cs
from mages import mage
from rogues import rogue
from wardens import warden
from vivid import vivid
from lamp_hd import lamp_hd
from lamp_flat import lamp_flat, PALETTE as LAMP_FLAT
# 見た目の名前 → (絵, 色の差し替え)。色は Ramp と同じ 12 個の数（明るい順に4段）
FLAME=(255,170,60,236,120,30,190,76,16,130,44,10)
P={
 'Lampwright':(lamp_hd(),{'c':(76,108,190,52,84,160,36,60,124,24,40,86),'d':(40,60,120,30,46,96,22,34,72,14,22,50),'I':(120,210,220,80,170,190,52,128,150,30,88,110),'S':(224,170,140,200,146,118,170,118,96,130,90,74),
                  'K':(214,132,124,190,110,104,160,88,84,120,64,62),'y':(222,192,140,190,158,106,150,120,76,106,82,50),
                  'i':(120,120,132,84,84,96,60,60,72,38,38,48),'n':(80,70,70,58,50,52,42,36,38,28,24,26)}),
 'Lampwright_Adv0':(mage('flare'),{'c':(220,70,70,176,40,48,128,26,34,88,16,24),'d':(140,34,40,110,24,30,84,16,22,60,10,16),'F':FLAME}),
 'Lampwright_Adv0_3rd':(mage('pyre'),{'c':(150,40,46,110,24,32,76,14,22,50,8,14),'d':(90,18,24,70,12,18,52,8,12,36,4,8),'F':FLAME}),
 'Lampwright_Adv1':(mage('beacon'),{'c':(250,248,236,222,216,196,178,170,150,130,122,104),'d':(214,206,182,190,180,154,160,150,124,126,116,94)}),
 'Lampwright_Adv1_3rd':(mage('star'),{'c':(70,86,170,44,56,130,30,38,94,18,24,64),'d':(40,50,116,30,38,96,22,28,74,14,18,52)}),
 'Shadeblade':(rogue('base'),{'k':(70,68,86,52,50,66,38,36,50,26,24,36),'T':(84,80,96,62,58,74,44,40,54,30,28,38)}),
 'Shadeblade_Adv0':(rogue('night'),{'n':(110,80,150,72,50,108,50,34,78,32,22,52),'k':(80,58,116,56,40,86,40,28,64,26,18,44),'r':(190,120,255,150,80,220,106,52,170,72,34,120),'T':(80,60,110,60,44,86,44,30,64,30,20,44)}),
 'Shadeblade_Adv0_3rd':(rogue('eclipse'),{'n':(70,54,100,44,32,70,30,20,50,18,12,32),'k':(50,38,76,36,26,58,26,18,42,16,10,28),'r':(190,120,255,150,80,220,106,52,170,72,34,120),'T':(56,42,80,40,30,60,28,20,44,18,12,30)}),
 'Shadeblade_Adv1':(rogue('blood'),{'n':(120,52,62,86,34,44,60,22,30,40,14,20),'k':(70,26,34,56,20,28,42,14,20,28,8,14),'T':(90,40,48,66,28,34,48,18,24,32,10,16)}),
 'Shadeblade_Adv1_3rd':(rogue('bloodking'),{'n':(120,52,62,86,34,44,60,22,30,40,14,20),'k':(60,18,26,46,12,20,34,8,14,22,4,8),'T':(90,40,48,66,28,34,48,18,24,32,10,16)}),
 'Warden':(warden('base'),{}),
 'Warden_Adv0':(warden('sun'),{'v':(255,244,200,236,210,120,196,160,70,140,108,40),'m':(255,250,230,222,214,186,170,160,130,118,110,86),'F':(220,190,100,200,166,80,168,134,56,124,96,36)}),
 'Warden_Adv0_3rd':(warden('dawn'),{'v':(255,244,200,236,210,120,196,160,70,140,108,40),'m':(255,250,230,222,214,186,170,160,130,118,110,86),'F':(220,190,100,200,166,80,168,134,56,124,96,36)}),
 'Warden_Adv1':(warden('bastion'),{'m':(170,190,230,110,130,176,74,90,130,46,56,86),'v':(70,96,170,44,64,130,30,44,94,20,28,64),'F':(40,56,116,32,46,98,24,34,76,16,22,54)}),
 'Warden_Adv1_3rd':(warden('aegis'),{'m':(170,190,230,110,130,176,74,90,130,46,56,86),'v':(70,96,170,44,64,130,30,44,94,20,28,64),'F':(40,56,116,32,46,98,24,34,76,16,22,54)}),
 'Vividbearer':(vivid(),{}),
}
# 色を1ドットずつ書き出した絵（陰影を計算しない）：見た目の名前 → (行, 文字→色)
from hd_forms import forms
FLAT={}
FLAT.update(forms({n:d for n,(_,d) in P.items()}))
# 外で描いた絵（ai/見た目の名前.png。import_ai.py でドットに戻して色を整えたもの）は、その絵で置き換える
import glob
from PIL import Image
from flat import POOL
for f in sorted(glob.glob(os.path.join(os.path.dirname(os.path.abspath(__file__)),'ai','*.png'))):
    name=os.path.splitext(os.path.basename(f))[0]
    im=Image.open(f).convert('RGBA'); w,h=im.size; px=im.load()
    cmap={}; rows_=[]
    for y in range(h):
        r=''
        for x in range(w):
            c=px[x,y]
            if c[3]<128: r+='.'; continue
            if c not in cmap: cmap[c]=POOL[len(cmap)]
            r+=cmap[c]
        rows_.append(r)
    FLAT[name]=(rows_,{ch:c for c,ch in cmap.items()})
for n in FLAT: P.pop(n,None)
rows='\n'.join(cs(n,c.rows()) for n,(c,_) in P.items())
# 色を1ドットずつ決めた絵は PNG にして、base64 の文字列でコードに入れる（文字の行と色の表をそのまま書くと、
# 静的コンストラクタが巨大になって Android（IL2CPP）のビルドが失敗するため）。起動中に Texture2D.LoadImage で読む
import base64, io
def png_b64(rows_, colors):
    h=len(rows_); w=len(rows_[0]); im=Image.new('RGBA',(w,h),(0,0,0,0)); px=im.load()
    for y,r in enumerate(rows_):
        for x,ch in enumerate(r):
            c=colors.get(ch)
            if c: px[x,y]=tuple(c) if len(c)==4 else (*c,255)
    buf=io.BytesIO(); im.save(buf,'PNG',optimize=True); return base64.b64encode(buf.getvalue()).decode()
flat_png='\n'.join('            ["%s"] = "%s",' % (n,png_b64(r,d)) for n,(r,d) in FLAT.items())
def pal(d): return ', '.join("{ '%s', Ramp(%s) }" % (k, ', '.join(map(str,v))) for k,v in d.items())
pals='\n'.join('            ["%s"] = new Dictionary<char, Color32[]> { %s },' % (n,pal(d)) for n,(_,d) in P.items() if d)
src=f'''using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{{
    /// <summary>
    /// 町の画面の大きな立ち姿の、専用の絵（素材の文字の行。tools/portrait の build.py で作る。手で直さない）。
    /// 陰影は PortraitPixels が付ける。PortraitPalette はその絵だけの色（ダンジョンの絵の色の差し替えより優先）。
    /// </summary>
    public static partial class PixelArt
    {{
        static readonly Dictionary<string, string[]> Portraits = new Dictionary<string, string[]>
        {{
{rows}
        }};

        /// <summary>
        /// 色を1ドットずつ決めた立ち絵の PNG（base64）。陰影は計算しない。
        /// （大きな表をコードに書くと Android のビルドで静的コンストラクタが巨大になって失敗するので、画像の文字列で持つ）
        /// </summary>
        static readonly Dictionary<string, string> FlatPortraitPng = new Dictionary<string, string>
        {{
{flat_png}
        }};

        static readonly Dictionary<string, Dictionary<char, Color32[]>> PortraitPalette = new Dictionary<string, Dictionary<char, Color32[]>>
        {{
{pals}
        }};
    }}
}}
'''
open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'../../Game/Assets/Scripts/Core/PortraitRows.cs'),'w').write(src)
print('ok', len(P))
