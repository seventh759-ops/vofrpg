"""細かい版（192×272）の15種類。色はすべて1ドットずつ書き出す（flat.py）。"""
from canvas import C
from flat import flatten, scale2x, hd_face
from lamp_hd import lamp_hd
from mages import mage
from rogues import rogue
from wardens import warden
from vivid import vivid
from rogue_hd import rogue_hd
from warden_hd import warden_hd
from vivid_hd import vivid_hd

DEF={'Q':(244,170,160,236,156,146,220,140,130,200,120,112),'I':(120,210,220,80,170,190,52,128,150,30,88,110),'S':(224,170,140,200,146,118,170,118,96,130,90,74),
     'K':(214,132,124,190,110,104,160,88,84,120,64,62),'y':(222,192,140,190,158,106,150,120,76,106,82,50),
     'i':(120,120,132,84,84,96,60,60,72,38,38,48),'n':(80,70,70,58,50,52,42,36,38,28,24,26)}
FLAME=(255,170,60,236,120,30,190,76,16,130,44,10)
def poly_tone(T,M,pts,tone,mats):
    """多角形の中の明るさを足し引きする（tone: 0 照り／2 影／3 深い影）。"""
    W=len(M[0]);H=len(M); c=C(W,H); c.poly(pts,'#')
    d={0:-1.4,2:1.2,3:2.2}[tone]
    for y in range(H):
        for x in range(W):
            if c.g[y][x]=='#' and M[y][x] in mats and isinstance(T[y][x],float): T[y][x]=max(0.0,min(7.0,T[y][x]+d))
def mage_extra(T,M):
    # 長衣の大きな影と照り（灯術師の細かい版と同じ置き方）
    poly_tone(T,M,[(98,142),(104,142),(122,238),(104,238)],2,'c')
    poly_tone(T,M,[(104,150),(108,150),(122,238),(116,238)],3,'c')
    poly_tone(T,M,[(70,150),(74,150),(62,224),(57,224)],0,'c')
    poly_tone(T,M,[(96,100),(106,98),(100,140),(94,140)],2,'c')
    poly_tone(T,M,[(58,140),(66,140),(64,180),(54,180)],2,'c')
    poly_tone(T,M,[(112,150),(136,150),(136,180),(116,180)],2,'c')
    poly_tone(T,M,[(60,96),(76,86),(80,88),(64,104)],0,'c')
    # すそのひだ：影の筋と、その左の照りの筋
    for (x0,x1) in [(74,62),(84,82),(94,104)]:
        poly_tone(T,M,[(x0,150),(x0+2,150),(x1+2,226),(x1,226)],2,'c')
        poly_tone(T,M,[(x0-3,150),(x0-1,150),(x1-1,226),(x1-3,226)],0,'c')

def mage_hd(kind,pal,aura=None):
    c=lamp_hd(kind)
    return flatten(c.rows(),{**DEF,**pal},aura,mage_extra)

def scaled(c,pal,iris='I',hair='h',aura=None,fix=None):
    M=scale2x(c.rows())
    M=hd_face(M,92,51,iris=iris,hair=hair)
    if fix: M=fix(M)
    return flatten(M,{**DEF,**pal},aura)

def vivid_fix(M):
    g=[list(r) for r in M]
    for y in range(52,56):
        for x in range(97,101):
            if g[y][x] in 'I': g[y][x]='B'
            if g[y][x]=='e': g[y][x]='B'
    for y in range(52,56):
        for x in range(83,87):
            if g[y][x] in 'Ie': g[y][x]='p'
    return [''.join(r) for r in g]

def forms(P):
    """P: build.py の色の差し替え {見た目: {文字: 12個の数}}。返り値 {見た目: (行, 色)}"""
    pal=lambda n: P.get(n,{})
    out={}
    out['Lampwright']=mage_hd('base',{'c':(76,108,190,52,84,160,36,60,124,24,40,86),'d':(40,60,120,30,46,96,22,34,72,14,22,50)})
    out['Lampwright_Adv0']=mage_hd('flare',{**pal('Lampwright_Adv0'),'F':FLAME})
    out['Lampwright_Adv0_3rd']=mage_hd('pyre',{**pal('Lampwright_Adv0_3rd'),'F':FLAME},(255,130,40))
    out['Lampwright_Adv1']=mage_hd('beacon',pal('Lampwright_Adv1'))
    out['Lampwright_Adv1_3rd']=mage_hd('star',pal('Lampwright_Adv1_3rd'),(150,200,255))
    def hd(c,p,aura=None): return flatten(c.rows(),{**DEF,**p},aura,mail='J')
    STEEL={'m':(206,210,222,160,166,182,116,120,138,74,78,94),'J':(150,154,170,110,114,130,78,80,96,50,52,64),'i':(130,132,146,96,98,112,68,70,82,44,46,56),'b':(120,88,62,92,66,46,68,48,34,46,32,24)}
    out['Shadeblade']=hd(rogue_hd('base'),{**STEEL,'n':(70,68,82,50,48,60,36,34,44,24,22,30),'k':(54,50,62,38,36,46,26,24,32,16,14,20),'T':(70,64,72,50,46,52,36,32,38,24,22,26)})
    out['Shadeblade_Adv0']=hd(rogue_hd('night'),{**STEEL,'m':(186,170,214,140,124,172,100,86,132,64,54,92),'n':(120,84,170,88,58,136,62,40,100,40,24,68),'k':(90,62,132,64,44,100,46,30,74,30,18,50),'r':(190,120,255,150,80,220,106,52,170,72,34,120),'T':(84,62,116,62,46,88,44,32,64,30,20,44)})
    out['Shadeblade_Adv0_3rd']=hd(rogue_hd('eclipse'),{**STEEL,'m':(160,146,190,118,104,150,82,70,112,52,44,76),'n':(82,60,118,58,42,88,40,28,64,26,16,44),'k':(60,44,90,44,32,68,30,20,50,18,10,32),'r':(176,110,240,136,72,204,96,46,156,64,30,110),'T':(60,46,84,44,32,62,30,22,44,20,14,30)},(170,100,255))
    out['Shadeblade_Adv1']=hd(rogue_hd('blood'),{**STEEL,'n':(150,52,62,118,34,44,86,22,30,56,14,20),'k':(90,30,38,70,22,28,52,14,20,34,8,14),'T':(96,40,48,72,28,34,52,18,24,34,10,16)})
    out['Shadeblade_Adv1_3rd']=hd(rogue_hd('bloodking'),{**STEEL,'n':(150,52,62,118,34,44,86,22,30,56,14,20),'k':(90,30,38,70,22,28,52,14,20,34,8,14),'F':(180,40,52,146,26,38,110,16,26,72,8,16),'T':(96,40,48,72,28,34,52,18,24,34,10,16)},(255,60,70))
    HAIR={'D':(84,62,52,60,42,36,42,28,24,28,18,16)}
    out['Warden']=hd(warden_hd('base'),{**STEEL,**HAIR,'v':(176,52,56,140,34,40,104,22,28,70,14,18)})
    GOLD={'m':(255,232,150,232,190,86,180,136,48,122,88,26),'J':(200,170,110,160,130,76,120,94,50,84,64,30),'i':(214,180,110,176,140,74,136,102,48,94,68,30),'v':(255,236,170,236,200,110,196,152,64,140,104,36)}
    out['Warden_Adv0']=hd(warden_hd('sun'),{**STEEL,**HAIR,**GOLD})
    PALE={'m':(255,252,236,244,232,196,216,196,150,176,154,108),'J':(226,212,176,200,184,142,166,148,104,124,108,72),'i':(244,232,196,216,196,150,176,154,108,130,110,74),'v':(255,250,226,240,226,176,212,190,130,170,146,90),'H':(255,248,226,240,226,186,210,190,140,170,148,100),'b':(236,220,180,206,186,140,170,148,100,124,104,66)}
    out['Warden_Adv0_3rd']=hd(warden_hd('dawn'),{**STEEL,**HAIR,**PALE},(255,210,90))
    BLUE={'m':(170,196,240,112,140,200,74,98,156,46,62,110),'J':(110,130,170,80,98,136,56,70,104,36,46,72),'i':(110,130,170,80,98,136,56,70,104,36,46,72),'v':(70,100,184,46,70,148,30,48,108,20,30,72)}
    out['Warden_Adv1']=hd(warden_hd('bastion'),{**STEEL,**HAIR,**BLUE})
    out['Warden_Adv1_3rd']=hd(warden_hd('aegis'),{**STEEL,**HAIR,**BLUE},(190,225,255))
    from flat import VIVID
    vp={k:tuple(x for t in v for x in t) for k,v in VIVID.items()}
    vp.update({'P':(246,180,226,226,140,200,190,104,170,140,70,128),'b':(250,196,232,232,160,212,200,120,180,150,84,136)})
    out['Vividbearer']=hd(vivid_hd(),vp,(255,90,215))
    return out
