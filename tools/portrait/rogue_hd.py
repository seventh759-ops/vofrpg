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
    c.poly([(49,132),(66,132),(65,164),(50,164)],'A'); c.poly([(102,132),(119,132),(118,164),(103,164)],'A')
    c.ell(57,172,6,7,'b'); c.ell(111,172,6,7,'b')
    # 肩当て（二段の鋼の板）
    for sx in (60,108):
        c.ell(sx,96,15,9,'A'); c.ell(sx,104,12,6,'A'); c.line(sx-13,100,sx+13,100,'i',1)
    for sx in (60,108):
        for dx in (-8,0,8): c.ell(sx+dx,93,1,1,'g')                      # 肩当ての鋲
    # すねの革の巻き帯と、ひざ当て
    for y in (200,210,220): c.line(68,y,83,y+2,'b',1.2); c.line(86,y+2,101,y,'b',1.2)
    c.ell(76,184,6,5,'m'); c.ell(93,184,6,5,'m')
    # 外套のひだ
    for (x0,x1) in ((50,44),(118,124)): c.line(x0,120,x1,236,'n' if kind!='bloodking' else 'f',1,only='kF')
    # 剣（右手・下へ）と短剣（左手・逆手）
    c.poly([(108,176),(114,176),(136,242),(132,246)],blade)
    if blade=='m': c.line(112,178,133,240,'x',1)
    else: c.line(111,178,133,240,'o',1)
    c.rect(104,174,120,177,'g'); c.ell(112,170,2.5,2.5,'g')
    c.poly([(54,178),(59,178),(54,206),(51,204)],blade); c.rect(50,176,62,178,'g')
    # 首巻き（首に巻いて、左へ長くなびく）
    c.poly([(70,78),(98,78),(100,92),(68,92)],'r')
    c.poly([(68,82),(72,92),(40,106),(10,108),(30,98)],'r')
    c.poly([(40,106),(10,108),(4,118),(28,114)],'r')
    neck_hd(c)
    c.poly([(70,78),(98,78),(100,92),(68,92)],'r')
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
    if kind=='eclipse':
        for (x,y) in [(30,140),(160,90),(24,200),(166,210)]: c.line(x-3,y,x+3,y,'2'); c.line(x,y-3,x,y+3,'2')
    if kind=='bloodking':
        for (x,y) in [(28,150),(160,100),(26,220)]: c.line(x-3,y,x+3,y,'6'); c.line(x,y-3,x,y+3,'6')
    return c
