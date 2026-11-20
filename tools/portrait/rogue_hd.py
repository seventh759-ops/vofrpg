from hd_body import *
def rogue_hd(kind):
    """影刃の系統（細かい版）。黒い頭巾・なびく首巻き・鋼の肩当てと胸当て・外套・右手に剣、左手に短剣。
    kind: base / night（紫・角の頭巾・紫に光る刃）/ eclipse（さらに暗く、目が光る）/ blood（白い髪・赤い刃）/ bloodking（冠・白い毛皮の赤い外套・赤い目）"""
    c=C(192,272)
    purple = kind in ('night','eclipse'); blood = kind in ('blood','bloodking')
    blade = 'p' if purple else ('R' if blood else 'm')
    # 外套（背中）
    cape='F' if kind=='bloodking' else 'k'
    c.poly([(58,90),(110,90),(126,238),(118,244),(50,244),(42,238)],cape)
    if kind=='bloodking':
        # 白い毛皮の縁（黒い点）
        for (x0,y0,x1,y1) in [(58,90,42,238),(110,90,126,238),(42,240,126,240)]:
            c.line(x0,y0,x1,y1,'x',7)
        for k in range(40):
            import random; random.seed(k)
            x=random.choice([random.randint(40,48),random.randint(120,128)]); y=random.randint(96,236)
            if c.get(x,y)=='x': c.set(x,y,'e')
        c.ell(84,90,28,9,'x')
    # なびく首巻きの端（2本）：曲線に沿って、波打ちながら細くなる帯。ねじれの所は濃い色（Z）
    def ribbon(p0,p1,p2,w0,w1,waves,amp):
        L=[];R=[];mid=[]
        n=40
        for i in range(n+1):
            t=i/n
            x=(1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0]; y=(1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1]
            dx=2*(1-t)*(p1[0]-p0[0])+2*t*(p2[0]-p1[0]); dy=2*(1-t)*(p1[1]-p0[1])+2*t*(p2[1]-p1[1])
            d=math.hypot(dx,dy) or 1; nx,ny=-dy/d,dx/d
            off=amp*math.sin(t*math.pi*2*waves)*t
            w=(w0+(w1-w0)*t)*(0.85+0.15*abs(math.cos(t*math.pi*2*waves)))   # 波の山で少し細く（ねじれ）
            cx,cy=x+nx*off,y+ny*off
            L.append((cx+nx*w/2,cy+ny*w/2)); R.append((cx-nx*w/2,cy-ny*w/2)); mid.append((cx,cy,nx,ny,w,t))
        c.poly(L+R[::-1],'r')
        for (cx,cy,nx,ny,w,t) in mid:
            ph=(t*waves*2)%1
            if 0.42<ph<0.58: c.line(cx+nx*w/2,cy+ny*w/2,cx-nx*w/2,cy-ny*w/2,'Z',1.2,only='r')   # ねじれの影
        ex,ey,nx,ny,w,_=mid[-1]
        c.poly([(ex+nx*w/2,ey+ny*w/2),(ex-nx*w/2,ey-ny*w/2),(ex-6,ey+w*0.6)],'r')   # 端は斜めに切れる
    # 首の後ろから、左肩の後ろを通って流れる（体と肩当てに隠れて、肩の外から見える）
    ribbon((80,90),(44,92),(16,124),10,8,0.5,3)          # 下の短い端
    ribbon((80,86),(46,80),(8,104),15,11,0.75,5)         # 上の長い端
    legs_hd(c)
    # 胴着と腰の垂れ
    c.poly([(60,94),(108,94),(102,136),(66,136)],'n')                     # 胴は広く、腰で細い
    c.poly([(64,134),(84,134),(82,172),(56,170)],'n'); c.poly([(84,134),(104,134),(112,170),(86,172)],'n')   # 腰の垂れは広がる
    # 胸当て（中央の稜線）
    c.poly([(64,96),(104,96),(100,124),(84,130),(68,124)],'m')
    c.line(84,100,84,128,'i',1)
    # たすき掛けの革帯（左肩から右の腰へ）と留め金、小さな投げ短剣
    c.line(70,98,100,136,'b',3.5); c.rect(86,114,90,118,'g')
    for k in range(3): c.poly([(92+k*3,124+k*4),(94+k*3,124+k*4),(93+k*3,118+k*4)],'m')
    # 帯
    c.rect(65,131,103,136,'b'); c.rect(80,130,88,137,'g'); c.rect(82,132,86,135,'b')
    # 腕：袖・前腕の籠手・手袋
    arm_down(c,-1,'N',hand=False); arm_down(c,1,'N',hand=False)            # 袖は胴と別の素材（境目に影）
    c.poly([(46,132),(61,132),(60,164),(48,164)],'A'); c.poly([(107,132),(122,132),(120,164),(108,164)],'A')
    c.ell(54,172,5.5,6.5,'b'); c.ell(114,172,5.5,6.5,'b')
    # 肩当て（二段の鋼の板）
    for sx in (58,110):
        o=-1 if sx<84 else 1                                                  # 外側の向き
        X=lambda dx: sx+o*dx
        # 上の板：首側は高く平ら、外へ向かって下がる。下にもう1枚、外へずれた板を重ねる
        c.poly([(X(-10),89),(X(4),88),(X(14),93),(X(17),101),(X(12),104),(X(-8),99)],'A')
        c.poly([(X(-4),100),(X(12),102),(X(18),106),(X(16),112),(X(0),108)],'A')
        c.line(X(-8),99,X(12),104,'i',1); c.line(X(0),108,X(16),112,'i',1)
        for dx in (-6,2,10): c.ell(X(dx),91+max(0,dx)*0.35,1,1,'g')        # 鋲                      # 肩当ての鋲
    # すねの革の巻き帯と、ひざ当て
    for y in (200,210,220): c.line(63,y,80,y+2,'b',1.2); c.line(89,y+2,106,y,'b',1.2)
    c.ell(72,184,6,5,'m'); c.ell(97,184,6,5,'m')
    # 外套のひだ
    for (x0,x1) in ((50,44),(118,124)): c.line(x0,120,x1,236,'n' if kind!='bloodking' else 'f',1,only='kF')
    # 剣（右手・下へ）と短剣（左手・逆手）
    c.poly([(111,176),(117,176),(139,242),(135,246)],blade)
    if blade=='m': c.line(115,178,136,240,'x',1)
    else: c.line(114,178,136,240,'o',1)
    c.rect(107,174,123,177,'g'); c.ell(115,170,2.5,2.5,'g')
    c.poly([(51,178),(56,178),(51,206),(48,204)],blade); c.rect(47,176,59,178,'g')
    # 首巻き（首に巻いて、左へ長くなびく）
    c.poly([(68,83),(100,83),(102,90),(66,90)],'r')   # 首に巻く所は低く（首が長く見えないように）
    neck_hd(c)
    c.poly([(68,83),(100,83),(102,90),(66,90)],'r')   # 首に巻く所は低く（首が長く見えないように）
    c.oy=7   # 頭を少し下げて首を短く
    # 頭
    face_hd(c)
    if blood:
        hair_spiky(c,'x')
        eyes_hd(c,iris='R' if kind=='bloodking' else 'I',brow='q')
        if kind=='bloodking':
            c.poly([(66,30),(102,30),(104,14),(96,22),(90,10),(84,20),(78,10),(72,22),(64,14)],'g')
            for (x,y) in ((90,12),(78,12),(84,22)): c.ell(x,y,1.6,1.6,'R')
    else:
        bangs_hd(c,'h')
        hood_hd(c,'k' if kind!='base' else 'k',horns=purple)
        eyes_hd(c,iris='p' if kind=='eclipse' else 'I',pupil=kind!='eclipse')
    c.oy=0
    # 口元を覆う布（首巻きを鼻の上まで引き上げる）。目元だけ見える
    c.oy=7   # 頭と同じ位置に
    c.poly([(69,62),(99,62),(100,68),(95,75),(84,79),(73,75),(68,68)],'r',only='sSKQh')
    c.line(70,62,98,62,'r',1.4,only='sSKQh')
    for (x0,y0,x1,y1) in [(76,64,80,73),(88,64,86,74),(82,66,84,76)]: c.line(x0,y0,x1,y1,'O',1,only='r')   # 布のしわ
    c.oy=0
    if kind=='eclipse':
        for (x,y) in [(30,140),(160,90),(24,200),(166,210)]: c.line(x-3,y,x+3,y,'2'); c.line(x,y-3,x,y+3,'2')
    if kind=='bloodking':
        for (x,y) in [(28,150),(160,100),(26,220)]: c.line(x-3,y,x+3,y,'6'); c.line(x,y-3,x,y+3,'6')
    return c
