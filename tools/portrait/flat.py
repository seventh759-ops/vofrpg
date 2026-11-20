"""素材の文字の絵（M）を、色を1ドットずつ書き出した絵にする。
光は左上から。影・照り・落ち影・金属の縁・輪郭・光の縁を、決めた置き方で文字として描き込む（ゲームでは計算しない）。"""
import re, os
from canvas import C
HERE=os.path.dirname(os.path.abspath(__file__))
SRC=open(os.path.join(HERE,'../../Game/Assets/Scripts/Core/PixelArt.cs')).read()
def _ramps(block_name):
    i=SRC.index(block_name); j=SRC.index('};',i)
    out={}
    for m in re.finditer(r"\{ '(.)', Ramp\(([\d,\s]+)\) \}",SRC[i:j]):
        v=[int(t) for t in m.group(2).split(',')]
        out[m.group(1)]=[tuple(v[k:k+3]) for k in range(0,12,3)]
    return out
BASE=_ramps('static readonly Dictionary<char, Color32[]> ramps')
VIVID=_ramps('static readonly Dictionary<char, Color32[]> VividOver')
GLOW={'l':(255,230,130),'L':(255,170,60),'o':(255,250,220),'p':(200,130,255),'R':(255,70,80),'B':(150,210,255),'W':(150,255,200),
      '1':(255,250,220),'2':(200,130,255),'3':(150,210,255),'4':(150,255,200),'5':(255,170,60),'6':(255,70,80)}
SHINY=set('mgxHuq')        # 上を明るく下を暗くする素材（金属・金・白・銀）
HAIR=set('hHqD')
MAIL=set()                  # 鎖かたびらの編み目を付ける素材（絵ごとに flatten の mail で指定）
POOL=[ch for ch in 'ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!#$%&*+-=?@^_~:;<>/|']

def ramp(pal,ch):
    if ch in pal: v=pal[ch]; return [tuple(v[k:k+3]) for k in range(0,12,3)]
    return BASE.get(ch)

def flatten(M, pal, aura=None, extra=None, mail=''):
    """M: 素材の行（文字のリスト）。pal: その絵だけの色 {文字:(12個の数)}。aura: 光の縁の色 (r,g,b)。
    extra(T,M) で、その絵だけの影・照りを足せる（T は段の番号 0〜3 か、('G',色) の表）。"""
    global MAIL; MAIL=set(mail)
    H=len(M); W=len(M[0])
    def m(x,y): return M[y][x] if 0<=x<W and 0<=y<H else '.'
    T=[[None]*W for _ in range(H)]
    def run(x,y):
        ch=M[y][x]; l=x
        while m(l-1,y)==ch: l-=1
        r=x
        while m(r+1,y)==ch: r+=1
        return x-l, r-x, r-l+1
    for y in range(H):
        for x in range(W):
            ch=M[y][x]
            if ch=='.': continue
            if ch in GLOW: T[y][x]=('G',GLOW[ch]); continue
            dl,dr,wid=run(x,y)
            t=1
            if ch=='Q': T[y][x]=1; continue
            if ch in SHINY:
                if m(x,y-1)!=ch: t=0
                elif m(x,y+1)!=ch: t=2
                if dr==0 and m(x,y+1)!=ch: t=3
                elif dr==0: t=2
            elif ch in 'sS':
                if dr<=1 or (m(x,y-1) in 'hHq' or m(x,y-2) in 'hHq'): t=2
                elif dl==0: t=0
            elif ch in 'hHq' and False: pass
            else:
                big = wid>=10
                if dr<=(1 if big else 0): t=3 if big else 2
                elif big and dr<=4: t=2
                elif dl<=(1 if big else 0): t=0
                # 上にほかの素材があれば、その下に落ち影
                for k in (1,2,3):
                    up=m(x,y-k)
                    if up!='.' and up!=ch and up not in GLOW and up not in 'sS':
                        t=max(t,3 if k==1 else 2); break
            if ch in HAIR:
                k=(x - y//3) % 7
                if k==0 and m(x,y-1)==ch: t=0                      # 毛束の照り
                elif k==4: t=max(t,2)                               # 毛束の影
                if m(x+1,y)!=ch or m(x,y+1)!=ch: t=3 if t>=2 else 2
            if ch in MAIL:
                t = 1 if (x//2+y//2)%2==0 else 2                    # 鎖の編み目
                if (x%4==0 and y%4==0): t=0
            # 素材の境目：右と下でほかの素材に接するところは一段暗く（線で描いたように見せる）
            rb=(m(x+1,y),m(x,y+1))
            if not (ch in 'sSxeIpRBKQ') and any(n!='.' and n!=ch and n not in GLOW and n not in 'xeIKQ' for n in rb): t=min(3,t+1)
            if ch in SHINY and t==0 and dl<=3 and m(x,y-1)!=ch: t='hi'      # 金属のふちの白い照り
            T[y][x]=t
    if extra: extra(T,M)
    # 色の表に直す
    out=[['.']*W for _ in range(H)]
    colors={}; key={}
    def code(col):
        if col not in key:
            key[col]=POOL[len(key)]; colors[key[col]]=col
        return key[col]
    for y in range(H):
        for x in range(W):
            t=T[y][x]; ch=M[y][x]
            if t is None: continue
            if isinstance(t,tuple): out[y][x]=code(t[1]+(255,)); continue
            r=ramp(pal,ch) or BASE['c']
            if t=='hi':
                c0=r[0]; out[y][x]=code(tuple(min(255,int(v+(255-v)*0.6)) for v in c0)+(255,)); continue
            out[y][x]=code(r[t]+(255,))
    # 輪郭（きらめきの数字は輪郭を付けない）
    def solid(x,y): return 0<=x<W and 0<=y<H and out[y][x] not in '.' and M[y][x] not in '123456' and out[y][x]!=key.get((22,16,30,255))
    oc=code((22,16,30,255))
    o2=[r[:] for r in out]
    for y in range(H):
        for x in range(W):
            if out[y][x]!='.': continue
            nb=[(x+dx,y+dy) for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)) if solid(x+dx,y+dy)]
            if not nb: continue
            # 輪郭は、接している素材のいちばん暗い色をさらに暗くした色（黒一色より、なじむ）
            nx,ny=nb[0]; mch=M[ny][nx]
            r=ramp(pal,mch)
            if r and mch not in GLOW:
                d=r[3]; o2[y][x]=code((int(d[0]*0.45+22*0.55),int(d[1]*0.45+16*0.55),int(d[2]*0.45+30*0.55),255))
            else: o2[y][x]=oc
    out=o2
    if aura:
        for ring,a in ((0,200),(1,110)):
            ac=code(tuple(aura)+(a,))
            o2=[r[:] for r in out]
            for y in range(H):
                for x in range(W):
                    if out[y][x]=='.' and any(0<=x+dx<W and 0<=y+dy<H and out[y+dy][x+dx] not in '.' and M[y+dy][x+dx] not in '123456' and colors.get(out[y+dy][x+dx],(0,0,0,0))[3]>a for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))):
                        o2[y][x]=ac
            out=o2
    # きらめき・光るもののまわりに淡い光（1ドット）
    o2=[r[:] for r in out]
    for y in range(H):
        for x in range(W):
            if M[y][x] in '123456' :
                g=GLOW[M[y][x]]; gc=code(g+(110,))
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
                    if 0<=x+dx<W and 0<=y+dy<H and out[y+dy][x+dx]=='.': o2[y+dy][x+dx]=gc
    out=o2
    return [''.join(r) for r in out], colors

def scale2x(rows):
    H=len(rows); W=len(rows[0])
    def g(x,y): return rows[y][x] if 0<=x<W and 0<=y<H else '.'
    out=[['.']*(W*2) for _ in range(H*2)]
    for y in range(H):
        for x in range(W):
            P=g(x,y); A=g(x,y-1); B=g(x+1,y); Cc=g(x-1,y); D=g(x,y+1)
            e0=e1=e2=e3=P
            if Cc==A and Cc!=D and A!=B: e0=A
            if A==B and A!=Cc and B!=D: e1=B
            if D==Cc and D!=B and Cc!=A: e2=Cc
            if B==D and B!=A and D!=Cc: e3=D
            out[2*y][2*x]=e0; out[2*y][2*x+1]=e1; out[2*y+1][2*x]=e2; out[2*y+1][2*x+1]=e3
    return [''.join(r) for r in out]

def hd_face(M, cx, cy, iris='I', hair='h', mouth='K', lash='e', brows=True):
    """2倍にした絵の顔に、目（白目・瞳・黒目・光・まつげ）・眉・鼻・口を描き直す。cx,cy は顔の中心。"""
    g=[list(r) for r in M]
    def put(x,y,ch):
        if 0<=y<len(g) and 0<=x<len(g[0]): g[y][x]=ch
    # 古い目を消す
    for y in range(cy-10,cy+12):
        for x in range(cx-14,cx+15):
            if 0<=y<len(g) and 0<=x<len(g[0]) and g[y][x] in 'exIpRBK': g[y][x]='s'
    for ex in (cx-7,cx+7):
        for y in range(cy+1,cy+5):
            for x in range(ex-3,ex+4): put(x,y,'x')
        for y in range(cy+1,cy+5):
            for x in range(ex-2,ex+2): put(x,y,iris)
        if iris in ('I',):
            put(ex-1,cy+2,'e'); put(ex,cy+2,'e'); put(ex-1,cy+3,'e'); put(ex,cy+3,'e')
        put(ex+1,cy+1,'x')
        for x in range(ex-4,ex+4): put(x,cy,lash)
        for x in range(ex-3,ex+3): put(x,cy-1,lash)
        if brows:
            for x in range(ex-4,ex+4): put(x,cy-5,hair)
    put(cx,cy+6,'S'); put(cx,cy+7,'S'); put(cx-1,cy+8,'S'); put(cx+1,cy+8,'S')
    for x in range(cx-2,cx+3): put(x,cy+11,mouth)
    return [''.join(r) for r in g]
