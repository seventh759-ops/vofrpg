from hd_body import *
def warden_hd(kind):
    """灯番の系統（細かい版）。兜なしの騎士：板金の鎧・十字の陣羽織・左腕に十字の盾・右手にランタン。
    kind: base（茶の短い髪・赤）/ sun（金の三つ編み・金）/ dawn（白金・光の輪と光の翼）/ bastion（黒い巻き毛・青）/ aegis（角の飾り・大きな盾）"""
    c=C(192,272)
    if kind=='dawn':
        for side in (-1,1):
            for k in range(4):
                cx=84+side*(34+k*7); cy=96+k*16
                c.ell(cx,cy,8,15,'l'); c.ell(cx,cy+2,4,9,'o')
    # 脚：すね当てと鉄靴
    c.poly([(68,150),(83,150),(82,232),(70,232)],'J'); c.poly([(86,150),(101,150),(99,232),(87,232)],'J')
    c.poly([(66,186),(84,186),(83,232),(68,232)],'m'); c.poly([(85,186),(103,186),(101,232),(86,232)],'m')
    c.ell(75,190,8,6,'m'); c.ell(94,190,8,6,'m')
    c.poly([(66,232),(84,232),(84,262),(88,264),(88,269),(62,269),(64,258)],'b')
    c.poly([(85,232),(103,232),(104,258),(108,262),(108,269),(85,269),(86,258)],'b')
    # 胴：鎖かたびらと胸当て
    c.poly([(60,94),(108,94),(102,134),(110,170),(58,170),(66,134)],'J')   # 胴は広く、腰で細く、腰から下は広がる
    c.poly([(62,96),(106,96),(101,132),(67,132)],'m')
    c.line(84,100,84,130,'i',1)
    # 陣羽織（前の垂れ）と十字
    cross='g' if kind in ('bastion','aegis') else 'x'
    c.poly([(70,120),(98,120),(104,206),(64,206)],'v')
    c.rect(81,128,87,184,cross); c.rect(73,140,95,146,cross)
    c.rect(64,202,104,206,'g')
    c.line(70,122,64,204,'g',1.2); c.line(98,122,104,204,'g',1.2)       # 陣羽織のふちの金の線
    c.rect(66,131,102,136,'b'); c.rect(80,130,88,137,'g')
    # 腕：板金の腕・籠手
    arm_down(c,-1,'A',hand=False); arm_down(c,1,'A',hand=False)            # 腕の鎧は胴と別の素材（境目に影）
    c.ell(54,172,5.5,6.5,'A'); c.ell(114,172,5.5,6.5,'A')
    c.line(47,132,61,132,'i',1); c.line(107,132,122,132,'i',1)
    # 肩当て
    for sx in (58,110):
        c.ell(sx,96,16,10,'A'); c.ell(sx,106,13,6,'A'); c.line(sx-14,101,sx+14,101,'i',1)
    # 板の継ぎ目（腕・すね・胸）
    for y in (142,152): c.line(47,y,61,y,'i',1); c.line(107,y,121,y,'i',1)
    for y in (200,212,222): c.line(66,y,84,y,'i',1); c.line(85,y,103,y,'i',1)
    c.line(70,118,98,118,'i',1)
    for sx in (58,110):
        for dx in (-9,0,9): c.ell(sx+dx,92,1,1,'g')
    # ランタン（右手に下げる）
    c.line(114,176,117,186,'i',1.4); c.line(114,176,111,186,'i',1.4)
    c.poly([(105,186),(123,186),(119,180),(109,180)],'i')
    c.rect(105,186,123,212,'i'); c.rect(108,189,120,209,'l'); c.ell(114,200,3,6,'L'); c.ell(114,201,1.5,3,'o')
    c.rect(103,212,125,216,'i')
    # 盾（左腕）
    if kind!='dawn':
        big = kind=='aegis'
        x0,x1,y0,y1=(10,62,112,236) if big else (18,58,122,214)
        shape=[(x0,y0),(x1,y0),(x1,y1-30),((x0+x1)/2,y1),(x0,y1-30)]
        c.poly(shape,'m')
        inner=[(x0+3,y0+3),(x1-3,y0+3),(x1-3,y1-31),((x0+x1)/2,y1-4),(x0+3,y1-31)]
        c.poly(inner,'f' if kind=='base' else ('g' if kind=='sun' else 'u'))
        mx=(x0+x1)/2
        for k in range(8):
            c.ell(x0+4+k*(x1-x0-8)/7,y0+3,1,1,'g')                         # 盾の縁の鋲
        c.rect(mx-3,y0+12,mx+3,y1-24,'x'); c.rect(mx-14,y0+30,mx+14,y0+36,'x'); c.ell(mx,y0+33,4,4,'g')
    neck_hd(c)
    c.rect(76,84,92,94,'m')       # 首当て
    c.oy=7   # 頭を少し下げて首を短く
    face_hd(c)
    hc={'base':'h','sun':'H','dawn':'H','bastion':'D','aegis':'D'}[kind]
    {'base':hair_bob,'sun':hair_braid,'dawn':hair_braid,'bastion':hair_curly,'aegis':hair_curly}[kind](c,hc)
    eyes_hd(c,brow=hc if hc!='H' else 'h',iris='B' if kind=='aegis' else 'I')
    if kind=='dawn':
        for a in range(0,360,4):
            r=math.radians(a); c.ell(84+16*math.cos(r),16+4*math.sin(r),1.2,1.2,'l')
    if kind=='aegis':
        c.line(66,40,102,40,'g',2)
        c.poly([(68,38),(60,24),(58,8),(66,18),(72,34)],'w'); c.poly([(100,38),(108,24),(110,8),(102,18),(96,34)],'w')
        c.ell(58,8,2.5,2.5,'g'); c.ell(110,8,2.5,2.5,'g')
        for (x,y) in [(170,100),(176,200),(16,60)]: c.line(x-3,y,x+3,y,'1'); c.line(x,y-3,x,y+3,'1')
    c.oy=0
    return c
