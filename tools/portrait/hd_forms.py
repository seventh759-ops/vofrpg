"""細かい版（192×272）の15種類。色はすべて1ドットずつ書き出す（flat.py）。"""
from canvas import C
from flat import flatten, scale2x, hd_face
from lamp_hd import lamp_hd
from mages import mage
from rogues import rogue
from wardens import warden
from vivid import vivid

DEF={'I':(120,210,220,80,170,190,52,128,150,30,88,110),'S':(224,170,140,200,146,118,170,118,96,130,90,74),
     'K':(214,132,124,190,110,104,160,88,84,120,64,62),'y':(222,192,140,190,158,106,150,120,76,106,82,50),
     'i':(120,120,132,84,84,96,60,60,72,38,38,48),'n':(80,70,70,58,50,52,42,36,38,28,24,26)}
FLAME=(255,170,60,236,120,30,190,76,16,130,44,10)
def poly_tone(T,M,pts,tone,mats):
    W=len(M[0]);H=len(M); c=C(W,H); c.poly(pts,'#')
    for y in range(H):
        for x in range(W):
            if c.g[y][x]=='#' and M[y][x] in mats and not isinstance(T[y][x],tuple): T[y][x]=tone
def mage_extra(T,M):
    # 長衣の大きな影と照り（灯術師の細かい版と同じ置き方）
    poly_tone(T,M,[(98,142),(104,142),(122,238),(104,238)],2,'c')
    poly_tone(T,M,[(104,150),(108,150),(122,238),(116,238)],3,'c')
    poly_tone(T,M,[(70,150),(74,150),(62,224),(57,224)],0,'c')
    poly_tone(T,M,[(96,100),(106,98),(100,140),(94,140)],2,'c')
    poly_tone(T,M,[(58,140),(66,140),(64,180),(54,180)],2,'c')
    poly_tone(T,M,[(112,150),(136,150),(136,180),(116,180)],2,'c')
    poly_tone(T,M,[(60,96),(76,86),(80,88),(64,104)],0,'c')

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
    out['Lampwright_Adv0']=mage_hd('flare',{**pal('Lampwright_Adv0'),'F':FLAME})
    out['Lampwright_Adv0_3rd']=mage_hd('pyre',{**pal('Lampwright_Adv0_3rd'),'F':FLAME},(255,130,40))
    out['Lampwright_Adv1']=mage_hd('beacon',pal('Lampwright_Adv1'))
    out['Lampwright_Adv1_3rd']=mage_hd('star',pal('Lampwright_Adv1_3rd'),(150,200,255))
    out['Shadeblade']=scaled(rogue('base'),pal('Shadeblade'))
    out['Shadeblade_Adv0']=scaled(rogue('night'),pal('Shadeblade_Adv0'))
    out['Shadeblade_Adv0_3rd']=scaled(rogue('eclipse'),pal('Shadeblade_Adv0_3rd'),iris='p',aura=(170,100,255))
    out['Shadeblade_Adv1']=scaled(rogue('blood'),pal('Shadeblade_Adv1'),hair='q')
    out['Shadeblade_Adv1_3rd']=scaled(rogue('bloodking'),pal('Shadeblade_Adv1_3rd'),iris='R',hair='q',aura=(255,60,70))
    out['Warden']=scaled(warden('base'),pal('Warden'))
    out['Warden_Adv0']=scaled(warden('sun'),pal('Warden_Adv0'))
    out['Warden_Adv0_3rd']=scaled(warden('dawn'),pal('Warden_Adv0_3rd'),aura=(255,210,90))
    out['Warden_Adv1']=scaled(warden('bastion'),pal('Warden_Adv1'))
    out['Warden_Adv1_3rd']=scaled(warden('aegis'),pal('Warden_Adv1_3rd'),iris='B',aura=(190,225,255))
    from flat import VIVID
    vp={k:tuple(x for t in v for x in t) for k,v in VIVID.items()}
    out['Vividbearer']=scaled(vivid(),vp,hair='q',aura=(255,90,215),fix=vivid_fix)
    return out
